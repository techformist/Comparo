using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Comparo.Core.DiffAlgorithms;
using Comparo.Core.StructuredComparators;
using Comparo.Core.Normalizers;
using System.Text.Json;

namespace Comparo.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio")]
[Config(typeof(SemanticComparisonPerformanceConfig))]
public class SemanticComparisonPerformanceTests
{
    private readonly MyersDiff _myersDiff = new();
    private readonly JsonSemanticComparator _jsonComparator = new();
    private readonly XmlSemanticComparator _xmlComparator = new();
    private readonly JsonNormalizer _jsonNormalizer = new();
    private readonly XmlNormalizer _xmlNormalizer = new();

    private string? _jsonSimple;
    private string? _jsonNested2;
    private string? _jsonNested5;
    private string? _jsonNested10;
    private string? _jsonDeepArray;

    private string? _xmlSimple;
    private string? _xmlNested3;
    private string? _xmlNested5;
    private string? _xmlNested10;

    private string[]? _jsonLines;
    private string[]? _xmlLines;

    [GlobalSetup]
    public void Setup()
    {
        _jsonSimple = GenerateSimpleJson(100);
        _jsonNested2 = GenerateNestedJson(100, 2);
        _jsonNested5 = GenerateNestedJson(100, 5);
        _jsonNested10 = GenerateNestedJson(100, 10);
        _jsonDeepArray = GenerateDeepArrayJson(1000);

        _xmlSimple = GenerateSimpleXml(100);
        _xmlNested3 = GenerateNestedXml(100, 3);
        _xmlNested5 = GenerateNestedXml(100, 5);
        _xmlNested10 = GenerateNestedXml(100, 10);

        _jsonLines = _jsonNested5.Split('\n');
        _xmlLines = _xmlNested5.Split('\n');
    }

    #region JSON Comparison

    [Benchmark]
    public void Json_Semantic_Simple()
    {
        var modified = TestDataGenerator.ModifyJson(_jsonSimple!, 0.05);
        _jsonComparator.Compare(_jsonSimple!, modified);
    }

    [Benchmark]
    public void Json_Semantic_Nested2()
    {
        var modified = TestDataGenerator.ModifyJson(_jsonNested2!, 0.05);
        _jsonComparator.Compare(_jsonNested2!, modified);
    }

    [Benchmark]
    public void Json_Semantic_Nested5()
    {
        var modified = TestDataGenerator.ModifyJson(_jsonNested5!, 0.05);
        _jsonComparator.Compare(_jsonNested5!, modified);
    }

    [Benchmark]
    public void Json_Semantic_Nested10()
    {
        var modified = TestDataGenerator.ModifyJson(_jsonNested10!, 0.05);
        _jsonComparator.Compare(_jsonNested10!, modified);
    }

    [Benchmark]
    public void Json_Semantic_DeepArray()
    {
        var modified = TestDataGenerator.ModifyJson(_jsonDeepArray!, 0.05);
        _jsonComparator.Compare(_jsonDeepArray!, modified);
    }

    [Benchmark]
    public void Json_LineBased_Nested5()
    {
        var modifiedJson = TestDataGenerator.ModifyJson(_jsonNested5!, 0.05);
        var modifiedLines = modifiedJson.Split('\n');
        _myersDiff.ComputeDiff(_jsonLines!, modifiedLines);
    }

    [Benchmark]
    public void Json_Normalization_Nested5()
    {
        var normalized = _jsonNormalizer.Normalize(_jsonNested5!);
    }

    #endregion

    #region XML Comparison

    [Benchmark]
    public void Xml_Semantic_Simple()
    {
        var modified = TestDataGenerator.ModifyXml(_xmlSimple!, 0.05);
        _xmlComparator.Compare(_xmlSimple!, modified);
    }

    [Benchmark]
    public void Xml_Semantic_Nested3()
    {
        var modified = TestDataGenerator.ModifyXml(_xmlNested3!, 0.05);
        _xmlComparator.Compare(_xmlNested3!, modified);
    }

    [Benchmark]
    public void Xml_Semantic_Nested5()
    {
        var modified = TestDataGenerator.ModifyXml(_xmlNested5!, 0.05);
        _xmlComparator.Compare(_xmlNested5!, modified);
    }

    [Benchmark]
    public void Xml_Semantic_Nested10()
    {
        var modified = TestDataGenerator.ModifyXml(_xmlNested10!, 0.05);
        _xmlComparator.Compare(_xmlNested10!, modified);
    }

    [Benchmark]
    public void Xml_LineBased_Nested5()
    {
        var modifiedXml = TestDataGenerator.ModifyXml(_xmlNested5!, 0.05);
        var modifiedLines = modifiedXml.Split('\n');
        _myersDiff.ComputeDiff(_xmlLines!, modifiedLines);
    }

    [Benchmark]
    public void Xml_Normalization_Nested5()
    {
        var normalized = _xmlNormalizer.Normalize(_xmlNested5!);
    }

    #endregion

    #region Semantic vs Line-Based Comparison

    [Benchmark]
    public void Json_SemanticVsLineBased_Comparison()
    {
        var modifiedJson = TestDataGenerator.ModifyJson(_jsonNested5!, 0.10);
        var modifiedLines = modifiedJson.Split('\n');

        _jsonComparator.Compare(_jsonNested5!, modifiedJson);
        _myersDiff.ComputeDiff(_jsonLines!, modifiedLines);
    }

    [Benchmark]
    public void Xml_SemanticVsLineBased_Comparison()
    {
        var modifiedXml = TestDataGenerator.ModifyXml(_xmlNested5!, 0.10);
        var modifiedLines = modifiedXml.Split('\n');

        _xmlComparator.Compare(_xmlNested5!, modifiedXml);
        _myersDiff.ComputeDiff(_xmlLines!, modifiedLines);
    }

    #endregion

    #region JSON with Various Change Percentages

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    public void Json_Semantic_VariousChangePercentages(double changePercentage)
    {
        var modified = TestDataGenerator.ModifyJson(_jsonNested5!, changePercentage);
        _jsonComparator.Compare(_jsonNested5!, modified);
    }

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    public void Json_LineBased_VariousChangePercentages(double changePercentage)
    {
        var modifiedJson = TestDataGenerator.ModifyJson(_jsonNested5!, changePercentage);
        var modifiedLines = modifiedJson.Split('\n');
        _myersDiff.ComputeDiff(_jsonLines!, modifiedLines);
    }

    #endregion

    #region XML with Various Change Percentages

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    public void Xml_Semantic_VariousChangePercentages(double changePercentage)
    {
        var modified = TestDataGenerator.ModifyXml(_xmlNested5!, changePercentage);
        _xmlComparator.Compare(_xmlNested5!, modified);
    }

    [Benchmark]
    [Arguments(0.01)]
    [Arguments(0.05)]
    [Arguments(0.10)]
    [Arguments(0.25)]
    public void Xml_LineBased_VariousChangePercentages(double changePercentage)
    {
        var modifiedXml = TestDataGenerator.ModifyXml(_xmlNested5!, changePercentage);
        var modifiedLines = modifiedXml.Split('\n');
        _myersDiff.ComputeDiff(_xmlLines!, modifiedLines);
    }

    #endregion

    private string GenerateSimpleJson(int propertyCount)
    {
        var json = new Dictionary<string, object>();
        for (int i = 0; i < propertyCount; i++)
        {
            json[$"property{i}"] = $"value{i}";
        }
        return JsonSerializer.Serialize(json);
    }

    private string GenerateNestedJson(int propertyCount, int nestingLevel)
    {
        var json = new Dictionary<string, object>();

        for (int i = 0; i < propertyCount; i++)
        {
            if (nestingLevel > 0 && i % 3 == 0)
            {
                json[$"nested{i}"] = GenerateNestedJson(propertyCount / 3, nestingLevel - 1);
            }
            else
            {
                json[$"property{i}"] = $"value{i}";
            }
        }

        return JsonSerializer.Serialize(json);
    }

    private string GenerateDeepArrayJson(int arrayLength)
    {
        var array = new object[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            array[i] = new { index = i, value = $"item{i}" };
        }
        return JsonSerializer.Serialize(array);
    }

    private string GenerateSimpleXml(int elementCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<root>");

        for (int i = 0; i < elementCount; i++)
        {
            sb.AppendLine($"  <element{i}>value{i}</element{i}>");
        }

        sb.AppendLine("</root>");
        return sb.ToString();
    }

    private string GenerateNestedXml(int elementCount, int nestingLevel)
    {
        var sb = new System.Text.StringBuilder();
        GenerateNestedXmlElement(sb, "root", elementCount, nestingLevel);
        return sb.ToString();
    }

    private void GenerateNestedXmlElement(System.Text.StringBuilder sb, string elementName, int elementCount, int nestingLevel)
    {
        sb.Append($"<{elementName}>");

        for (int i = 0; i < elementCount; i++)
        {
            if (nestingLevel > 0 && i % 3 == 0)
            {
                GenerateNestedXmlElement(sb, $"child{i}", elementCount / 3, nestingLevel - 1);
            }
            else
            {
                sb.Append($"<item{i}>value{i}</item{i}>");
            }
        }

        sb.Append($"</{elementName}>");
    }
}

public class SemanticComparisonPerformanceConfig : ManualConfig
{
}
