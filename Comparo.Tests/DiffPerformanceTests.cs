using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using FluentAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace Comparo.Tests;

public class DiffPerformanceTests
{
    [Fact]
    public void MyersDiff_10KBFile_ShouldCompleteUnderTarget()
    {
        var algorithm = new MyersDiff();
        var oldLines = Enumerable.Range(1, 10000).Select(i => $"line{i} content here").ToArray();
        var newLines = oldLines.ToArray();
        newLines[5000] = "modified line 5001";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = algorithm.ComputeDiff(oldLines, newLines);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        result.Should().NotBeNull();
    }

    [Fact]
    public void PatienceDiff_10KBFile_ShouldCompleteUnderTarget()
    {
        var algorithm = new PatienceDiff();
        var oldLines = Enumerable.Range(1, 10000).Select(i => $"line{i} content here").ToArray();
        var newLines = oldLines.ToArray();
        newLines[5000] = "modified line 5001";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = algorithm.ComputeDiff(oldLines, newLines);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        result.Should().NotBeNull();
    }

    [Fact]
    public void HistogramDiff_10KBFile_ShouldCompleteUnderTarget()
    {
        var algorithm = new HistogramDiff();
        var oldLines = Enumerable.Range(1, 10000).Select(i => $"line{i} content here").ToArray();
        var newLines = oldLines.ToArray();
        newLines[5000] = "modified line 5001";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = algorithm.ComputeDiff(oldLines, newLines);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        result.Should().NotBeNull();
    }

    [Fact]
    public void AllAlgorithms_10MBFile_ShouldCompleteUnderTarget()
    {
        var algorithms = new List<IDiffAlgorithm> { new MyersDiff(), new PatienceDiff(), new HistogramDiff() };
        var oldLines = Enumerable.Range(1, 1000000).Select(i => $"line{i} content here").ToArray();
        var newLines = oldLines.ToArray();
        newLines[500000] = "modified line 500001";

        foreach (var algorithm in algorithms)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = algorithm.ComputeDiff(oldLines, newLines);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, $"{algorithm.GetType().Name} should complete in under 2000ms for 10MB file");
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void AllAlgorithms_ManyChanges_ShouldCompleteUnderTarget()
    {
        var algorithms = new List<IDiffAlgorithm> { new MyersDiff(), new PatienceDiff(), new HistogramDiff() };
        var oldLines = Enumerable.Range(1, 10000).Select(i => $"old{i}").ToArray();
        var newLines = Enumerable.Range(1, 10000).Select(i => $"new{i}").ToArray();

        foreach (var algorithm in algorithms)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = algorithm.ComputeDiff(oldLines, newLines);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, $"{algorithm.GetType().Name} should complete in under 1000ms");
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void AllAlgorithms_ComplexReordering_ShouldCompleteUnderTarget()
    {
        var algorithms = new List<IDiffAlgorithm> { new MyersDiff(), new PatienceDiff(), new HistogramDiff() };
        var oldLines = Enumerable.Range(1, 5000).Select(i => $"line{i}").ToArray();
        var newLines = oldLines.OrderByDescending(i => i).ToArray();

        foreach (var algorithm in algorithms)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = algorithm.ComputeDiff(oldLines, newLines);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, $"{algorithm.GetType().Name} should complete in under 500ms for reordering");
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void AllAlgorithms_LargeDuplicateContent_ShouldCompleteUnderTarget()
    {
        var algorithms = new List<IDiffAlgorithm> { new MyersDiff(), new PatienceDiff(), new HistogramDiff() };
        var oldLines = Enumerable.Repeat("same line", 10000).ToArray();
        var newLines = oldLines.ToArray();
        newLines[5000] = "different line";

        foreach (var algorithm in algorithms)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = algorithm.ComputeDiff(oldLines, newLines);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, $"{algorithm.GetType().Name} should handle duplicates efficiently");
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void AllAlgorithms_100KLinesWithSomeChanges_ShouldCompleteUnderTarget()
    {
        var algorithms = new List<IDiffAlgorithm> { new MyersDiff(), new PatienceDiff(), new HistogramDiff() };
        var oldLines = Enumerable.Range(1, 100000).Select(i => $"line{i}").ToArray();
        var newLines = oldLines.ToArray();

        for (int i = 0; i < 100; i++)
        {
            newLines[i * 1000] = $"modified line {i * 1000 + 1}";
        }

        foreach (var algorithm in algorithms)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = algorithm.ComputeDiff(oldLines, newLines);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, $"{algorithm.GetType().Name} should complete in under 500ms");
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void SideBySideModel_Performance_LargeFile_ShouldCompleteUnderTarget()
    {
        var algorithm = new MyersDiff();
        var oldLines = Enumerable.Range(1, 50000).Select(i => $"line{i}").ToArray();
        var newLines = oldLines.ToArray();
        newLines[25000] = "modified line 25001";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = algorithm.ComputeSideBySideDiff(oldLines, newLines);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(50000);
    }

    [Fact]
    public void MemoryUsage_10MBFile_ShouldBeUnderLimit()
    {
        var algorithm = new MyersDiff();
        var oldLines = Enumerable.Range(1, 1000000).Select(i => $"line{i} content here").ToArray();
        var newLines = oldLines.ToArray();
        newLines[500000] = "modified line 500001";

        var initialMemory = GC.GetTotalMemory(true);

        var result = algorithm.ComputeDiff(oldLines, newLines);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        var estimatedFileSize = oldLines.Sum(l => l.Length) + newLines.Sum(l => l.Length);
        var fiveXFileSize = estimatedFileSize * 5;

        memoryUsed.Should().BeLessThan(fiveXFileSize, "Memory usage should be less than 5x file size");
        result.Should().NotBeNull();
    }

    [Fact]
    public void ScrollLatency_Simulation_ShouldBeUnderTarget()
    {
        var algorithm = new MyersDiff();
        var oldLines = Enumerable.Range(1, 100000).Select(i => $"line{i}").ToArray();
        var newLines = oldLines.ToArray();
        newLines[50000] = "modified line 50001";

        var result = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var linesToRender = 50;
        var startLine = 50000;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var visibleLines = result.Lines.Skip(startLine).Take(linesToRender).ToList();

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(16, "Scrolling latency should be under 16ms (60 FPS)");
        visibleLines.Should().HaveCount(linesToRender);
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class DiffBenchmark
{
    private readonly MyersDiff _myersDiff = new();
    private readonly PatienceDiff _patienceDiff = new();
    private readonly HistogramDiff _histogramDiff = new();
    private string[]? _smallFile;
    private string[]? _mediumFile;
    private string[]? _largeFile;

    [GlobalSetup]
    public void Setup()
    {
        _smallFile = Enumerable.Range(1, 1000).Select(i => $"line{i}").ToArray();
        _mediumFile = Enumerable.Range(1, 10000).Select(i => $"line{i}").ToArray();
        _largeFile = Enumerable.Range(1, 100000).Select(i => $"line{i}").ToArray();
    }

    [Benchmark]
    public DiffHunk MyersDiff_SmallFile()
    {
        var copy = _smallFile!.ToArray();
        copy[500] = "modified";
        return _myersDiff.ComputeDiff(_smallFile!, copy);
    }

    [Benchmark]
    public DiffHunk PatienceDiff_SmallFile()
    {
        var copy = _smallFile!.ToArray();
        copy[500] = "modified";
        return _patienceDiff.ComputeDiff(_smallFile!, copy);
    }

    [Benchmark]
    public DiffHunk HistogramDiff_SmallFile()
    {
        var copy = _smallFile!.ToArray();
        copy[500] = "modified";
        return _histogramDiff.ComputeDiff(_smallFile!, copy);
    }

    [Benchmark]
    public SideBySideModel MyersDiff_MediumFile()
    {
        var copy = _mediumFile!.ToArray();
        copy[5000] = "modified";
        return _myersDiff.ComputeSideBySideDiff(_mediumFile!, copy);
    }

    [Benchmark]
    public SideBySideModel PatienceDiff_MediumFile()
    {
        var copy = _mediumFile!.ToArray();
        copy[5000] = "modified";
        return _patienceDiff.ComputeSideBySideDiff(_mediumFile!, copy);
    }

    [Benchmark]
    public SideBySideModel HistogramDiff_MediumFile()
    {
        var copy = _mediumFile!.ToArray();
        copy[5000] = "modified";
        return _histogramDiff.ComputeSideBySideDiff(_mediumFile!, copy);
    }

    [Benchmark]
    public SideBySideModel MyersDiff_LargeFile()
    {
        var copy = _largeFile!.ToArray();
        copy[50000] = "modified";
        return _myersDiff.ComputeSideBySideDiff(_largeFile!, copy);
    }

    [Benchmark]
    public SideBySideModel PatienceDiff_LargeFile()
    {
        var copy = _largeFile!.ToArray();
        copy[50000] = "modified";
        return _patienceDiff.ComputeSideBySideDiff(_largeFile!, copy);
    }

    [Benchmark]
    public SideBySideModel HistogramDiff_LargeFile()
    {
        var copy = _largeFile!.ToArray();
        copy[50000] = "modified";
        return _histogramDiff.ComputeSideBySideDiff(_largeFile!, copy);
    }
}
