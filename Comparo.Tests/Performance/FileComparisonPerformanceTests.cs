using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using Comparo.Core.DiffAlgorithms;
using Comparo.Core.StructuredComparators;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(FileComparisonPerformanceConfig))]
public class FileComparisonPerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly PatienceDiff _patienceDiff = new();
    private readonly HistogramDiff _histogramDiff = new();
    private readonly JsonSemanticComparator _jsonComparator = new();
    private readonly XmlSemanticComparator _xmlComparator = new();

    private string[]? _txt10KB;
    private string[]? _txt100KB;
    private string[]? _txt1MB;
    private string[]? _txt10MB;

    private string[]? _md10KB;
    private string[]? _md100KB;
    private string[]? _md1MB;

    private string? _json10KB;
    private string? _json100KB;
    private string? _json1MB;

    private string? _xml10KB;
    private string? _xml100KB;
    private string? _xml1MB;

    [GlobalSetup]
    public void Setup()
    {
        int avgLineLength = 50;

        _txt10KB = TestDataGenerator.GenerateTextLines(200, avgLineLength);
        _txt100KB = TestDataGenerator.GenerateTextLines(2000, avgLineLength);
        _txt1MB = TestDataGenerator.GenerateTextLines(20000, avgLineLength);
        _txt10MB = TestDataGenerator.GenerateTextLines(200000, avgLineLength);

        _md10KB = TestDataGenerator.GenerateMarkdownLines(200);
        _md100KB = TestDataGenerator.GenerateMarkdownLines(2000);
        _md1MB = TestDataGenerator.GenerateMarkdownLines(20000);

        _json10KB = TestDataGenerator.GenerateJson(10 * 1024);
        _json100KB = TestDataGenerator.GenerateJson(100 * 1024);
        _json1MB = TestDataGenerator.GenerateJson(1024 * 1024);

        _xml10KB = TestDataGenerator.GenerateXml(10 * 1024);
        _xml100KB = TestDataGenerator.GenerateXml(100 * 1024);
        _xml1MB = TestDataGenerator.GenerateXml(1024 * 1024);
    }

    #region TXT Files

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    [Arguments(10 * 1024 * 1024)]
    public void MyersDiff_Txt(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _txt10KB!,
            100 * 1024 => _txt100KB!,
            1024 * 1024 => _txt1MB!,
            _ => _txt10MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _myersDiff.ComputeDiff(lines, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    [Arguments(10 * 1024 * 1024)]
    public void PatienceDiff_Txt(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _txt10KB!,
            100 * 1024 => _txt100KB!,
            1024 * 1024 => _txt1MB!,
            _ => _txt10MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _patienceDiff.ComputeDiff(lines, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    [Arguments(10 * 1024 * 1024)]
    public void HistogramDiff_Txt(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _txt10KB!,
            100 * 1024 => _txt100KB!,
            1024 * 1024 => _txt1MB!,
            _ => _txt10MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _histogramDiff.ComputeDiff(lines, modified);
    }

    #endregion

    #region Markdown Files

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void MyersDiff_Markdown(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _md10KB!,
            100 * 1024 => _md100KB!,
            _ => _md1MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _myersDiff.ComputeDiff(lines, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void PatienceDiff_Markdown(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _md10KB!,
            100 * 1024 => _md100KB!,
            _ => _md1MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _patienceDiff.ComputeDiff(lines, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void HistogramDiff_Markdown(int fileSizeBytes)
    {
        var lines = fileSizeBytes switch
        {
            10 * 1024 => _md10KB!,
            100 * 1024 => _md100KB!,
            _ => _md1MB!
        };

        var modified = TestDataGenerator.ModifyLines(lines, 0.05);
        _histogramDiff.ComputeDiff(lines, modified);
    }

    #endregion

    #region JSON Files

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void JsonSemanticCompare(int fileSizeBytes)
    {
        var json = fileSizeBytes switch
        {
            10 * 1024 => _json10KB!,
            100 * 1024 => _json100KB!,
            _ => _json1MB!
        };

        var modified = TestDataGenerator.ModifyJson(json, 0.05);
        _jsonComparator.Compare(json, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void Json_LineBased_Compare(int fileSizeBytes)
    {
        var json = fileSizeBytes switch
        {
            10 * 1024 => _json10KB!,
            100 * 1024 => _json100KB!,
            _ => _json1MB!
        };

        var lines = json.Split('\n');
        var modifiedJson = TestDataGenerator.ModifyJson(json, 0.05);
        var modifiedLines = modifiedJson.Split('\n');
        _myersDiff.ComputeDiff(lines, modifiedLines);
    }

    #endregion

    #region XML Files

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void XmlSemanticCompare(int fileSizeBytes)
    {
        var xml = fileSizeBytes switch
        {
            10 * 1024 => _xml10KB!,
            100 * 1024 => _xml100KB!,
            _ => _xml1MB!
        };

        var modified = TestDataGenerator.ModifyXml(xml, 0.05);
        _xmlComparator.Compare(xml, modified);
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(1024 * 1024)]
    public void Xml_LineBased_Compare(int fileSizeBytes)
    {
        var xml = fileSizeBytes switch
        {
            10 * 1024 => _xml10KB!,
            100 * 1024 => _xml100KB!,
            _ => _xml1MB!
        };

        var lines = xml.Split('\n');
        var modifiedXml = TestDataGenerator.ModifyXml(xml, 0.05);
        var modifiedLines = modifiedXml.Split('\n');
        _myersDiff.ComputeDiff(lines, modifiedLines);
    }

    #endregion
}

public class FileComparisonPerformanceConfig : ManualConfig
{
}
