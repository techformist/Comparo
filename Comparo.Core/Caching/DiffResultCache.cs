using System.Collections.Concurrent;
using Comparo.Core.DiffModels;

namespace Comparo.Core.Caching;

public class DiffResultCache
{
    private readonly ConcurrentDictionary<string, CacheEntry<SideBySideModel>> _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly Timer _cleanupTimer;
    private readonly int _maxCacheSize;
    private long _cacheHits;
    private long _cacheMisses;

    public int CacheSize => _cache.Count;
    public long CacheHits => _cacheHits;
    public long CacheMisses => _cacheMisses;
    public double HitRate => _cacheHits + _cacheMisses > 0
        ? (double)_cacheHits / (_cacheHits + _cacheMisses)
        : 0;

    public DiffResultCache(TimeSpan? expiration = null, int maxCacheSize = 1000, TimeSpan? cleanupInterval = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry<SideBySideModel>>();
        _defaultExpiration = expiration ?? TimeSpan.FromMinutes(5);
        _maxCacheSize = maxCacheSize;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, cleanupInterval ?? TimeSpan.FromMinutes(1), cleanupInterval ?? TimeSpan.FromMinutes(1));
    }

    public bool TryGet(string leftFilePath, string rightFilePath, out SideBySideModel? result)
    {
        var key = GenerateCacheKey(leftFilePath, rightFilePath);

        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpirationTime)
            {
                result = entry.Value;
                Interlocked.Increment(ref _cacheHits);
                return true;
            }

            _cache.TryRemove(key, out _);
            Interlocked.Increment(ref _cacheMisses);
            result = null;
            return false;
        }

        Interlocked.Increment(ref _cacheMisses);
        result = null;
        return false;
    }

    public void Set(string leftFilePath, string rightFilePath, SideBySideModel diffResult, TimeSpan? customExpiration = null)
    {
        var key = GenerateCacheKey(leftFilePath, rightFilePath);

        if (_cache.Count >= _maxCacheSize)
        {
            EvictOldestEntry();
        }

        var expiration = customExpiration ?? _defaultExpiration;
        var entry = new CacheEntry<SideBySideModel>(diffResult, DateTime.UtcNow.Add(expiration));
        _cache.AddOrUpdate(key, entry, (_, _) => entry);
    }

    public void Invalidate(string leftFilePath, string rightFilePath)
    {
        var key = GenerateCacheKey(leftFilePath, rightFilePath);
        _cache.TryRemove(key, out _);
    }

    public void InvalidateFile(string filePath)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.Contains(filePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
    }

    public async Task<SideBySideModel?> GetOrComputeAsync(
        string leftFilePath,
        string rightFilePath,
        Func<Task<SideBySideModel>> computeFunc,
        TimeSpan? customExpiration = null)
    {
        if (TryGet(leftFilePath, rightFilePath, out var cachedResult))
        {
            return cachedResult;
        }

        var result = await computeFunc();
        Set(leftFilePath, rightFilePath, result, customExpiration);
        return result;
    }

    private string GenerateCacheKey(string leftFilePath, string rightFilePath)
    {
        // Avoid touching filesystem; just use names + stable hashes of paths
        var leftName = Path.GetFileName(leftFilePath);
        var rightName = Path.GetFileName(rightFilePath);
        var leftHash = leftFilePath.GetHashCode(StringComparison.OrdinalIgnoreCase);
        var rightHash = rightFilePath.GetHashCode(StringComparison.OrdinalIgnoreCase);
        return $"{leftName}_{leftHash}|{rightName}_{rightHash}";
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
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache.Clear();
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
