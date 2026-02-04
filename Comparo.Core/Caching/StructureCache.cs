using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Comparo.Core.Caching;

public class StructureCache
{
    private readonly ConcurrentDictionary<string, CacheEntry<ParsedStructure>> _cache;
    private readonly ConcurrentDictionary<string, string> _fileHashCache;
    private readonly TimeSpan _defaultExpiration;
    private readonly Timer _cleanupTimer;
    private readonly int _maxCacheSize;

    public int CacheSize => _cache.Count;

    public StructureCache(TimeSpan? expiration = null, int maxCacheSize = 200, TimeSpan? cleanupInterval = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry<ParsedStructure>>();
        _fileHashCache = new ConcurrentDictionary<string, string>();
        _defaultExpiration = expiration ?? TimeSpan.FromMinutes(15);
        _maxCacheSize = maxCacheSize;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, cleanupInterval ?? TimeSpan.FromMinutes(3), cleanupInterval ?? TimeSpan.FromMinutes(3));
    }

    public bool TryGet(string filePath, out object? structure)
    {
        if (_cache.TryGetValue(filePath, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpirationTime)
            {
                if (File.Exists(filePath))
                {
                    var currentFileHash = LineHashCache.ComputeFileHash(filePath);
                    if (_fileHashCache.TryGetValue(filePath, out var cachedFileHash) && currentFileHash == cachedFileHash)
                    {
                        structure = entry.Value.Structure;
                        return true;
                    }

                    Invalidate(filePath);
                }
            }
        }

        structure = null;
        return false;
    }

    public bool TryGet<T>(string filePath, out T? structure) where T : class
    {
        if (TryGet(filePath, out var obj) && obj is T typedStructure)
        {
            structure = typedStructure;
            return true;
        }

        structure = null;
        return false;
    }

    public void Set(string filePath, object structure, string structureType)
    {
        if (_cache.Count >= _maxCacheSize)
        {
            EvictOldestEntry();
        }

        var parsedStructure = new ParsedStructure(structure, structureType);
        var entry = new CacheEntry<ParsedStructure>(parsedStructure, DateTime.UtcNow.Add(_defaultExpiration));
        _cache.AddOrUpdate(filePath, entry, (_, _) => entry);

        if (File.Exists(filePath))
        {
            var fileHash = LineHashCache.ComputeFileHash(filePath);
            _fileHashCache.AddOrUpdate(filePath, fileHash, (_, _) => fileHash);
        }
    }

    public async Task<T?> GetOrComputeAsync<T>(
        string filePath,
        Func<Task<T>> computeFunc,
        string structureType,
        CancellationToken cancellationToken = default) where T : class
    {
        if (TryGet(filePath, out T? cachedStructure))
        {
            return cachedStructure;
        }

        var structure = await computeFunc();
        Set(filePath, structure, structureType);
        return structure;
    }

    public void Invalidate(string filePath)
    {
        _cache.TryRemove(filePath, out _);
        _fileHashCache.TryRemove(filePath, out _);
    }

    public void InvalidateByType(string structureType)
    {
        var keysToRemove = _cache
            .Where(kvp => kvp.Value.Value.StructureType == structureType)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
            _fileHashCache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        _fileHashCache.Clear();
    }

    public ImmutableArray<string> GetAllCachedFilePaths()
    {
        return _cache.Keys.ToImmutableArray();
    }

    public ImmutableDictionary<string, string> GetStructureTypeMap()
    {
        return _cache
            .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.Value.StructureType);
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

    private readonly struct ParsedStructure
    {
        public object Structure { get; }
        public string StructureType { get; }

        public ParsedStructure(object structure, string structureType)
        {
            Structure = structure;
            StructureType = structureType;
        }
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
