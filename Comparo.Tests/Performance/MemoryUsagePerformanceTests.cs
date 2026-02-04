using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using Comparo.Core.DiffAlgorithms;
using System.Diagnostics;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(MemoryUsagePerformanceConfig))]
public class MemoryUsagePerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly PatienceDiff _patienceDiff = new();
    private readonly HistogramDiff _histogramDiff = new();

    private string[]? _file1MB;
    private string[]? _file10MB;
    private string[]? _file100MB;

    private string[]? _modified1MB;
    private string[]? _modified10MB;
    private string[]? _modified100MB;

    [GlobalSetup]
    public void Setup()
    {
        _file1MB = TestDataGenerator.GenerateTextLines(20000);
        _file10MB = TestDataGenerator.GenerateTextLines(200000);
        _file100MB = TestDataGenerator.GenerateTextLines(2000000);

        _modified1MB = TestDataGenerator.ModifyLines(_file1MB, 0.05);
        _modified10MB = TestDataGenerator.ModifyLines(_file10MB, 0.05);
        _modified100MB = TestDataGenerator.ModifyLines(_file100MB, 0.05);
    }

    #region Memory Usage for Different File Sizes

    [Benchmark]
    public void Memory_Usage_1MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _myersDiff.ComputeDiff(_file1MB!, _modified1MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Memory_Usage_10MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _myersDiff.ComputeDiff(_file10MB!, _modified10MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Memory_Usage_100MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _myersDiff.ComputeDiff(_file100MB!, _modified100MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    #endregion

    #region Memory Comparison Between Algorithms

    [Benchmark]
    public void Memory_MyersDiff_10MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _myersDiff.ComputeDiff(_file10MB!, _modified10MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Memory_PatienceDiff_10MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _patienceDiff.ComputeDiff(_file10MB!, _modified10MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Memory_HistogramDiff_10MB()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        _histogramDiff.ComputeDiff(_file10MB!, _modified10MB!);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    #endregion

    #region Memory Leak Detection - Repeated Operations

    [Benchmark]
    public void Memory_Leak_Detection_RepeatedDiffs()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
        {
            var file = TestDataGenerator.GenerateTextLines(10000);
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeDiff(file, modified);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Memory_Leak_Detection_SideBySideDiffs()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
        {
            var file = TestDataGenerator.GenerateTextLines(10000);
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeSideBySideDiff(file, modified);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;
    }

    #endregion

    #region Memory Efficiency Target (< 5x File Size)

    [Benchmark]
    public void Memory_Efficiency_1MB_TargetCheck()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        var result = _myersDiff.ComputeDiff(_file1MB!, _modified1MB!);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        var memoryUsed = finalMemory - initialMemory;
        var estimatedFileSize = _file1MB!.Sum(l => l.Length) + _modified1MB!.Sum(l => l.Length);
        var fiveXFileSize = estimatedFileSize * 5;

        var isWithinTarget = memoryUsed < fiveXFileSize;
    }

    [Benchmark]
    public void Memory_Efficiency_10MB_TargetCheck()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        var result = _myersDiff.ComputeDiff(_file10MB!, _modified10MB!);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        var memoryUsed = finalMemory - initialMemory;
        var estimatedFileSize = _file10MB!.Sum(l => l.Length) + _modified10MB!.Sum(l => l.Length);
        var fiveXFileSize = estimatedFileSize * 5;

        var isWithinTarget = memoryUsed < fiveXFileSize;
    }

    [Benchmark]
    public void Memory_Efficiency_100MB_TargetCheck()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        var result = _myersDiff.ComputeDiff(_file100MB!, _modified100MB!);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        var memoryUsed = finalMemory - initialMemory;
        var estimatedFileSize = _file100MB!.Sum(l => l.Length) + _modified100MB!.Sum(l => l.Length);
        var fiveXFileSize = estimatedFileSize * 5;

        var isWithinTarget = memoryUsed < fiveXFileSize;
    }

    #endregion

    #region Process Memory Monitoring

    [Benchmark]
    public void Process_Memory_10MB_File()
    {
        var process = Process.GetCurrentProcess();
        var initialMemory = process.PrivateMemorySize64;

        _myersDiff.ComputeDiff(_file10MB!, _modified10MB!);

        var finalMemory = process.PrivateMemorySize64;
        var memoryDelta = finalMemory - initialMemory;
    }

    [Benchmark]
    public void Process_Memory_Peak_Usage()
    {
        var process = Process.GetCurrentProcess();
        var peakMemory = process.PeakWorkingSet64;

        _myersDiff.ComputeSideBySideDiff(_file10MB!, _modified10MB!);

        var finalPeakMemory = process.PeakWorkingSet64;
        var peakDelta = finalPeakMemory - peakMemory;
    }

    #endregion

    #region Memory Usage with Different Change Percentages

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    [Arguments(0.50)]
    public void Memory_Usage_VariousChangePercentages(double changePercentage)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(true);

        var file = TestDataGenerator.GenerateTextLines(10000);
        var modified = TestDataGenerator.ModifyLines(file, changePercentage);
        _myersDiff.ComputeDiff(file, modified);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
    }

    #endregion

    #region Large File Memory Stress Test

    [Benchmark]
    public void Memory_Stress_LargeFiles()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < 10; i++)
        {
            var file = TestDataGenerator.GenerateTextLines(50000);
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeDiff(file, modified);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    #endregion
}

public class MemoryUsagePerformanceConfig : ManualConfig
{
}
