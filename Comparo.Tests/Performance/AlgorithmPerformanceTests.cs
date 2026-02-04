using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using Comparo.Core.DiffAlgorithms;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(AlgorithmPerformanceConfig))]
public class AlgorithmPerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly PatienceDiff _patienceDiff = new();
    private readonly HistogramDiff _histogramDiff = new();

    private string[]? _identicalFile;
    private string[]? _smallChangesFile;
    private string[]? _largeChangesFile;
    private string[]? _reorderedFile;

    [GlobalSetup]
    public void Setup()
    {
        int lineCount = 10000;
        _identicalFile = TestDataGenerator.GenerateTextLines(lineCount);

        _smallChangesFile = TestDataGenerator.ModifyLines(_identicalFile, 0.05);
        _largeChangesFile = TestDataGenerator.ModifyLines(_identicalFile, 0.30);

        var original = TestDataGenerator.GenerateTextLines(lineCount);
        _reorderedFile = TestDataGenerator.ModifyLines(original, 0.20, true);
    }

    #region Identical Files

    [Benchmark]
    public void MyersDiff_Identical()
    {
        _myersDiff.ComputeDiff(_identicalFile!, _identicalFile!);
    }

    [Benchmark]
    public void PatienceDiff_Identical()
    {
        _patienceDiff.ComputeDiff(_identicalFile!, _identicalFile!);
    }

    [Benchmark]
    public void HistogramDiff_Identical()
    {
        _histogramDiff.ComputeDiff(_identicalFile!, _identicalFile!);
    }

    #endregion

    #region Small Changes (5%)

    [Benchmark]
    public void MyersDiff_SmallChanges()
    {
        _myersDiff.ComputeDiff(_identicalFile!, _smallChangesFile!);
    }

    [Benchmark]
    public void PatienceDiff_SmallChanges()
    {
        _patienceDiff.ComputeDiff(_identicalFile!, _smallChangesFile!);
    }

    [Benchmark]
    public void HistogramDiff_SmallChanges()
    {
        _histogramDiff.ComputeDiff(_identicalFile!, _smallChangesFile!);
    }

    #endregion

    #region Large Changes (30%)

    [Benchmark]
    public void MyersDiff_LargeChanges()
    {
        _myersDiff.ComputeDiff(_identicalFile!, _largeChangesFile!);
    }

    [Benchmark]
    public void PatienceDiff_LargeChanges()
    {
        _patienceDiff.ComputeDiff(_identicalFile!, _largeChangesFile!);
    }

    [Benchmark]
    public void HistogramDiff_LargeChanges()
    {
        _histogramDiff.ComputeDiff(_identicalFile!, _largeChangesFile!);
    }

    #endregion

    #region Reordered Content

    [Benchmark]
    public void MyersDiff_Reordered()
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        _myersDiff.ComputeDiff(original, _reorderedFile!);
    }

    [Benchmark]
    public void PatienceDiff_Reordered()
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        _patienceDiff.ComputeDiff(original, _reorderedFile!);
    }

    [Benchmark]
    public void HistogramDiff_Reordered()
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        _histogramDiff.ComputeDiff(original, _reorderedFile!);
    }

    #endregion

    #region SideBySide Diff Comparison

    [Benchmark]
    public void MyersDiff_SideBySide_SmallChanges()
    {
        _myersDiff.ComputeSideBySideDiff(_identicalFile!, _smallChangesFile!);
    }

    [Benchmark]
    public void PatienceDiff_SideBySide_SmallChanges()
    {
        _patienceDiff.ComputeSideBySideDiff(_identicalFile!, _smallChangesFile!);
    }

    [Benchmark]
    public void HistogramDiff_SideBySide_SmallChanges()
    {
        _histogramDiff.ComputeSideBySideDiff(_identicalFile!, _smallChangesFile!);
    }

    #endregion

    #region Different Change Percentages

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    [Arguments(0.50)]
    public void MyersDiff_VariousChangePercentages(double changePercentage)
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        var modified = TestDataGenerator.ModifyLines(original, changePercentage);
        _myersDiff.ComputeDiff(original, modified);
    }

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    [Arguments(0.50)]
    public void PatienceDiff_VariousChangePercentages(double changePercentage)
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        var modified = TestDataGenerator.ModifyLines(original, changePercentage);
        _patienceDiff.ComputeDiff(original, modified);
    }

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    [Arguments(0.50)]
    public void HistogramDiff_VariousChangePercentages(double changePercentage)
    {
        var original = TestDataGenerator.GenerateTextLines(10000);
        var modified = TestDataGenerator.ModifyLines(original, changePercentage);
        _histogramDiff.ComputeDiff(original, modified);
    }

    #endregion
}

public class AlgorithmPerformanceConfig : ManualConfig
{
}
