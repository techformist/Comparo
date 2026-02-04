using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Comparo.Core.Caching;

public class LineHashCache
{
    private readonly ConcurrentDictionary<string, CacheEntry<ImmutableArray<string>>> _cache;
    private readonly ConcurrentDictionary<string, string> _fileHashCache;
    private readonly TimeSpan _defaultExpiration;
    private readonly Timer _cleanupTimer;
    private readonly int _maxCacheSize;

    public int CacheSize => _cache.Count;

    public LineHashCache(TimeSpan? expiration = null, int maxCacheSize = 500, TimeSpan? cleanupInterval = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry<ImmutableArray<string>>>();
        _fileHashCache = new ConcurrentDictionary<string, string>();
        _defaultExpiration = expiration ?? TimeSpan.FromMinutes(10);
        _maxCacheSize = maxCacheSize;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, cleanupInterval ?? TimeSpan.FromMinutes(2), cleanupInterval ?? TimeSpan.FromMinutes(2));
    }

    public bool TryGet(string filePath, out ImmutableArray<string> lineHashes)
    {
        if (_cache.TryGetValue(filePath, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpirationTime)
            {
                if (File.Exists(filePath))
                {
                    var currentFileHash = ComputeFileHash(filePath);
                    if (_fileHashCache.TryGetValue(filePath, out var cachedFileHash) && currentFileHash == cachedFileHash)
                    {
                        lineHashes = entry.Value;
                        return true;
                    }

                    Invalidate(filePath);
                }
            }
        }

        lineHashes = ImmutableArray<string>.Empty;
        return false;
    }

    public ImmutableArray<string> GetOrCompute(string filePath, string[] lines)
    {
        if (TryGet(filePath, out var cachedHashes))
        {
            return cachedHashes;
        }

        var hashes = ComputeLineHashes(lines);
        Set(filePath, hashes);
        return hashes;
    }

    public async Task<ImmutableArray<string>> GetOrComputeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (TryGet(filePath, out var cachedHashes))
        {
            return cachedHashes;
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        var hashes = ComputeLineHashes(lines);
        Set(filePath, hashes);
        return hashes;
    }

    public void Set(string filePath, ImmutableArray<string> lineHashes)
    {
        if (_cache.Count >= _maxCacheSize)
        {
            EvictOldestEntry();
        }

        var entry = new CacheEntry<ImmutableArray<string>>(lineHashes, DateTime.UtcNow.Add(_defaultExpiration));
        _cache.AddOrUpdate(filePath, entry, (_, _) => entry);
        if (File.Exists(filePath))
        {
            var fileHash = ComputeFileHash(filePath);
            _fileHashCache.AddOrUpdate(filePath, fileHash, (_, _) => fileHash);
        }
        else
        {
            _fileHashCache.TryRemove(filePath, out _);
        }
    }

    public void Invalidate(string filePath)
    {
        _cache.TryRemove(filePath, out _);
        _fileHashCache.TryRemove(filePath, out _);
    }

    public void Clear()
    {
        _cache.Clear();
        _fileHashCache.Clear();
    }

    public static ImmutableArray<string> ComputeLineHashes(string[] lines)
    {
        var builder = ImmutableArray.CreateBuilder<string>(lines.Length);
        using var sha256 = SHA256.Create();

        foreach (var line in lines)
        {
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(line));
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
            builder.Add(hashString);
        }

        return builder.ToImmutableArray();
    }

    public static string ComputeLineHash(string line)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(line));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void EvictOldestEntry()
    {
        var oldestKey = _cache
            .OrderBy(kvp => kvp.Value.CreationTime)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (oldestKey != null)
        {
            _cache.TryRemove(oldestKey, out _);
            _fileHashCache.TryRemove(oldestKey, out _);
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpirationTime < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
            _fileHashCache.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache.Clear();
        _fileHashCache.Clear();
    }

    private class CacheEntry<T>
    {
        public T Value { get; }
        public DateTime CreationTime { get; }
        public DateTime ExpirationTime { get; }

        public CacheEntry(T value, DateTime expirationTime)
        {
            Value = value;
            CreationTime = DateTime.UtcNow;
            ExpirationTime = expirationTime;
        }
    }
}
