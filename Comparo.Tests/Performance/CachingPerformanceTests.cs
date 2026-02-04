using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Comparo.Core.DiffAlgorithms;
using Comparo.Core.Caching;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(CachingPerformanceConfig))]
public class CachingPerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly DiffResultCache _cache = new();

    private string[]? _fileSmall;
    private string[]? _fileMedium;
    private string[]? _fileLarge;

    private string[]? _modifiedSmall;
    private string[]? _modifiedMedium;
    private string[]? _modifiedLarge;

    [GlobalSetup]
    public void Setup()
    {
        _fileSmall = TestDataGenerator.GenerateTextLines(1000);
        _fileMedium = TestDataGenerator.GenerateTextLines(10000);
        _fileLarge = TestDataGenerator.GenerateTextLines(100000);

        _modifiedSmall = TestDataGenerator.ModifyLines(_fileSmall, 0.05);
        _modifiedMedium = TestDataGenerator.ModifyLines(_fileMedium, 0.05);
        _modifiedLarge = TestDataGenerator.ModifyLines(_fileLarge, 0.05);
    }

    #region Cache Hit vs Cache Miss

    [Benchmark]
    public void CacheHit_SmallFile()
    {
        string leftPath = "small_file.txt";
        string rightPath = "modified_small_file.txt";

        var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
        _cache.Set(leftPath, rightPath, result);

        if (_cache.TryGet(leftPath, rightPath, out var cached))
        {
            var _ = cached;
        }
    }

    [Benchmark]
    public void CacheMiss_SmallFile()
    {
        string leftPath = $"small_file_{Guid.NewGuid()}.txt";
        string rightPath = $"modified_small_file_{Guid.NewGuid()}.txt";

        if (_cache.TryGet(leftPath, rightPath, out var cached))
        {
            var _ = cached;
        }
    }

    [Benchmark]
    public void CacheHit_MediumFile()
    {
        string leftPath = "medium_file.txt";
        string rightPath = "modified_medium_file.txt";

        var result = _myersDiff.ComputeSideBySideDiff(_fileMedium!, _modifiedMedium!);
        _cache.Set(leftPath, rightPath, result);

        if (_cache.TryGet(leftPath, rightPath, out var cached))
        {
            var _ = cached;
        }
    }

    [Benchmark]
    public void CacheMiss_MediumFile()
    {
        string leftPath = $"medium_file_{Guid.NewGuid()}.txt";
        string rightPath = $"modified_medium_file_{Guid.NewGuid()}.txt";

        if (_cache.TryGet(leftPath, rightPath, out var cached))
        {
            var _ = cached;
        }
    }

    #endregion

    #region Repeated Comparisons

    [Benchmark]
    public void RepeatedComparisons_WithoutCache()
    {
        for (int i = 0; i < 10; i++)
        {
            _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
        }
    }

    [Benchmark]
    public void RepeatedComparisons_WithCache()
    {
        string leftPath = "repeated_file.txt";
        string rightPath = "modified_repeated_file.txt";

        for (int i = 0; i < 10; i++)
        {
            if (!_cache.TryGet(leftPath, rightPath, out var cached))
            {
                var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
                _cache.Set(leftPath, rightPath, result);
                cached = result;
            }
        }
    }

    #endregion

    #region LRU Eviction

    [Benchmark]
    public void LRU_Eviction_Performance()
    {
        var cache = new DiffResultCache(maxCacheSize: 100);

        for (int i = 0; i < 200; i++)
        {
            string leftPath = $"file_{i}.txt";
            string rightPath = $"modified_file_{i}.txt";

            var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
            cache.Set(leftPath, rightPath, result);
        }
    }

    #endregion

    #region Cache Performance with Different File Sizes

    [Benchmark]
    public void Cache_HitRate_MultipleAccess()
    {
        var cache = new DiffResultCache();

        string leftPath1 = "file1.txt";
        string rightPath1 = "modified_file1.txt";
        string leftPath2 = "file2.txt";
        string rightPath2 = "modified_file2.txt";

        var result1 = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
        var result2 = _myersDiff.ComputeSideBySideDiff(_fileMedium!, _modifiedMedium!);

        cache.Set(leftPath1, rightPath1, result1);
        cache.Set(leftPath2, rightPath2, result2);

        for (int i = 0; i < 20; i++)
        {
            var path = i % 2 == 0 ? leftPath1 : leftPath2;
            var rightPath = i % 2 == 0 ? rightPath1 : rightPath2;

            if (cache.TryGet(path, rightPath, out var cached))
            {
                var _ = cached;
            }
        }
    }

    #endregion

    #region Cache Invalidation

    [Benchmark]
    public void Cache_Invalidation_Performance()
    {
        var cache = new DiffResultCache();

        for (int i = 0; i < 100; i++)
        {
            string leftPath = $"file_{i}.txt";
            string rightPath = $"modified_file_{i}.txt";

            var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
            cache.Set(leftPath, rightPath, result);

            if (i % 10 == 0)
            {
                cache.InvalidateFile(leftPath);
            }
        }
    }

    #endregion

    #region Cache Statistics

    [Benchmark]
    public void Cache_HitRateMeasurement()
    {
        var cache = new DiffResultCache();

        string leftPath = "hit_rate_file.txt";
        string rightPath = "modified_hit_rate_file.txt";

        var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
        cache.Set(leftPath, rightPath, result);

        for (int i = 0; i < 100; i++)
        {
            if (cache.TryGet(leftPath, rightPath, out var cached))
            {
                var _ = cached;
            }
        }

        var hitRate = cache.HitRate;
    }

    #endregion

    #region Async Cache Operations

    [Benchmark]
    public async Task Cache_GetOrComputeAsync()
    {
        var cache = new DiffResultCache();
        string leftPath = "async_file.txt";
        string rightPath = "modified_async_file.txt";

        for (int i = 0; i < 10; i++)
        {
            var result = await cache.GetOrComputeAsync(
                leftPath,
                rightPath,
                () => Task.FromResult(_myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!))
            );
        }
    }

    #endregion

    #region Concurrent Access

    [Benchmark]
    public void Cache_ConcurrentAccess()
    {
        var cache = new DiffResultCache();

        Parallel.For(0, 50, i =>
        {
            string leftPath = $"concurrent_file_{i % 10}.txt";
            string rightPath = $"modified_concurrent_file_{i % 10}.txt";

            if (!cache.TryGet(leftPath, rightPath, out var cached))
            {
                var result = _myersDiff.ComputeSideBySideDiff(_fileSmall!, _modifiedSmall!);
                cache.Set(leftPath, rightPath, result);
            }
        });
    }

    #endregion
}

public class CachingPerformanceConfig : ManualConfig
{
}
