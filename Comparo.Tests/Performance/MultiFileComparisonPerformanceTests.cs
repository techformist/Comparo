using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using Comparo.Core.DiffAlgorithms;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(MultiFileComparisonPerformanceConfig))]
public class MultiFileComparisonPerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly PatienceDiff _patienceDiff = new();
    private readonly HistogramDiff _histogramDiff = new();

    private List<string[]>? _txtFiles10;
    private List<string[]>? _txtFiles50;
    private List<string[]>? _txtFiles100;
    private List<string[]>? _txtFiles1000;

    private List<string[]>? _mdFiles50;
    private List<string[]>? _mdFiles100;

    private List<string>? _jsonFiles50;
    private List<string>? _jsonFiles100;

    private List<string>? _xmlFiles50;
    private List<string>? _xmlFiles100;

    [GlobalSetup]
    public void Setup()
    {
        _txtFiles10 = Enumerable.Range(0, 10)
            .Select(_ => TestDataGenerator.GenerateTextLines(100, 50))
            .ToList();

        _txtFiles50 = Enumerable.Range(0, 50)
            .Select(_ => TestDataGenerator.GenerateTextLines(100, 50))
            .ToList();

        _txtFiles100 = Enumerable.Range(0, 100)
            .Select(_ => TestDataGenerator.GenerateTextLines(100, 50))
            .ToList();

        _txtFiles1000 = Enumerable.Range(0, 1000)
            .Select(_ => TestDataGenerator.GenerateTextLines(100, 50))
            .ToList();

        _mdFiles50 = Enumerable.Range(0, 50)
            .Select(_ => TestDataGenerator.GenerateMarkdownLines(100))
            .ToList();

        _mdFiles100 = Enumerable.Range(0, 100)
            .Select(_ => TestDataGenerator.GenerateMarkdownLines(100))
            .ToList();

        _jsonFiles50 = Enumerable.Range(0, 50)
            .Select(_ => TestDataGenerator.GenerateJson(10 * 1024))
            .ToList();

        _jsonFiles100 = Enumerable.Range(0, 100)
            .Select(_ => TestDataGenerator.GenerateJson(10 * 1024))
            .ToList();

        _xmlFiles50 = Enumerable.Range(0, 50)
            .Select(_ => TestDataGenerator.GenerateXml(10 * 1024))
            .ToList();

        _xmlFiles100 = Enumerable.Range(0, 100)
            .Select(_ => TestDataGenerator.GenerateXml(10 * 1024))
            .ToList();
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void MyersDiff_MultiFile_Txt(int fileCount)
    {
        var files = fileCount switch
        {
            10 => _txtFiles10!,
            50 => _txtFiles50!,
            _ => _txtFiles100!
        };

        foreach (var file in files)
        {
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeDiff(file, modified);
        }
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void PatienceDiff_MultiFile_Txt(int fileCount)
    {
        var files = fileCount switch
        {
            10 => _txtFiles10!,
            50 => _txtFiles50!,
            _ => _txtFiles100!
        };

        foreach (var file in files)
        {
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _patienceDiff.ComputeDiff(file, modified);
        }
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void HistogramDiff_MultiFile_Txt(int fileCount)
    {
        var files = fileCount switch
        {
            10 => _txtFiles10!,
            50 => _txtFiles50!,
            _ => _txtFiles100!
        };

        foreach (var file in files)
        {
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _histogramDiff.ComputeDiff(file, modified);
        }
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public void MyersDiff_MultiFile_Markdown(int fileCount)
    {
        var files = fileCount == 50 ? _mdFiles50! : _mdFiles100!;

        foreach (var file in files)
        {
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeDiff(file, modified);
        }
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public void MyersDiff_MultiFile_Json(int fileCount)
    {
        var files = fileCount == 50 ? _jsonFiles50! : _jsonFiles100!;

        foreach (var file in files)
        {
            var lines = file.Split('\n');
            var modifiedJson = TestDataGenerator.ModifyJson(file, 0.05);
            var modifiedLines = modifiedJson.Split('\n');
            _myersDiff.ComputeDiff(lines, modifiedLines);
        }
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public void MyersDiff_MultiFile_Xml(int fileCount)
    {
        var files = fileCount == 50 ? _xmlFiles50! : _xmlFiles100!;

        foreach (var file in files)
        {
            var lines = file.Split('\n');
            var modifiedXml = TestDataGenerator.ModifyXml(file, 0.05);
            var modifiedLines = modifiedXml.Split('\n');
            _myersDiff.ComputeDiff(lines, modifiedLines);
        }
    }

    [Benchmark]
    public void LargeScale_1000Files_Txt()
    {
        foreach (var file in _txtFiles1000!)
        {
            var modified = TestDataGenerator.ModifyLines(file, 0.05);
            _myersDiff.ComputeDiff(file, modified);
        }
    }
}

public class MultiFileComparisonPerformanceConfig : ManualConfig
{
}
