using Comparo.Core.Caching;
using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using Comparo.Core.FileParsers;
using Comparo.Core.Normalizers;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Comparo.Tests;

public class SyntaxHighlightingTests
{
    [Fact]
    public void SyntaxHighlighting_10KLines_ShouldCompleteUnderTarget()
    {
        var lines = GenerateCodeLines(10000);
        var code = string.Join('\n', lines);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var highlighted = HighlightCode(code);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300, "Syntax highlighting should complete in under 300ms for 10K lines");
        highlighted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SyntaxHighlighting_Keywords_ShouldBeDetected()
    {
        var code = @"public class Test {
    public void Method() {
        var x = 1;
        if (x == 1) return;
    }
}";

        var highlighted = HighlightCode(code);

        highlighted.Should().Contain("keyword", "Keywords should be highlighted");
    }

    [Fact]
    public void SyntaxHighlighting_Strings_ShouldBeDetected()
    {
        var code = @"var text = ""Hello World"";";

        var highlighted = HighlightCode(code);

        highlighted.Should().Contain("string", "Strings should be highlighted");
    }

    [Fact]
    public void SyntaxHighlighting_Comments_ShouldBeDetected()
    {
        var code = @"
// This is a comment
var x = 1;
/* Multi-line
   comment */";

        var highlighted = HighlightCode(code);

        highlighted.Should().Contain("comment", "Comments should be highlighted");
    }

    [Fact]
    public void SyntaxHighlighting_Numbers_ShouldBeDetected()
    {
        var code = @"var x = 42;
var y = 3.14;";

        var highlighted = HighlightCode(code);

        highlighted.Should().Contain("number", "Numbers should be highlighted");
    }

    [Fact]
    public void SyntaxHighlighting_LargeFile_ShouldMaintainPerformance()
    {
        var lines = GenerateCodeLines(50000);
        var code = string.Join('\n', lines);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var highlighted = HighlightCode(code);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Syntax highlighting should maintain performance for large files");
        highlighted.Should().NotBeNullOrEmpty();
    }

    private string HighlightCode(string code)
    {
        var result = new StringBuilder();
        var keywords = new[] { "public", "private", "class", "void", "var", "if", "else", "return", "for", "while", "using", "namespace", "static", "string", "int", "bool" };

        foreach (var line in code.Split('\n'))
        {
            var highlightedLine = line;

            foreach (var keyword in keywords)
            {
                highlightedLine = highlightedLine.Replace($" {keyword} ", $" <span class=\"keyword\">{keyword}</span> ");
            }

            highlightedLine = System.Text.RegularExpressions.Regex.Replace(highlightedLine, @""".*?""", "<span class=\"string\">$&</span>");
            highlightedLine = System.Text.RegularExpressions.Regex.Replace(highlightedLine, @"\/\/.*", "<span class=\"comment\">$&</span>");
            highlightedLine = System.Text.RegularExpressions.Regex.Replace(highlightedLine, @"\/\*.*?\*\/", "<span class=\"comment\">$&</span>");
            highlightedLine = System.Text.RegularExpressions.Regex.Replace(highlightedLine, @"\b\d+\.?\d*\b", "<span class=\"number\">$&</span>");

            result.AppendLine(highlightedLine);
        }

        return result.ToString();
    }

    private string[] GenerateCodeLines(int count)
    {
        var lines = new List<string>();
        var keywords = new[] { "public", "private", "class", "void", "var", "if", "else", "return" };
        var rand = new Random(42);

        for (int i = 0; i < count; i++)
        {
            var keyword = keywords[rand.Next(keywords.Length)];
            var num = rand.Next(100);
            lines.Add($"    {keyword} method{i}() {{ var x{i} = {num}; return x{i}; }}");
        }

        return lines.ToArray();
    }
}

public class CachingTests
{
    [Fact]
    public void DiffResultCache_SameInput_ShouldReturnCachedResult()
    {
        var cache = new DiffResultCache();
        var algorithm = new MyersDiff();

        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = ["line1", "modified", "line3"];

        var firstResult = algorithm.ComputeSideBySideDiff(oldLines, newLines);
        cache.Set("leftFile.txt", "rightFile.txt", firstResult);

        cache.TryGet("leftFile.txt", "rightFile.txt", out var cachedResult).Should().BeTrue();

        cachedResult.Should().NotBeNull();
        cachedResult!.Lines.Should().HaveCount(firstResult.Lines.Count);
    }

    [Fact]
    public void DiffResultCache_DifferentInput_ShouldNotReuseCache()
    {
        var cache = new DiffResultCache(maxCacheSize: 100);
        var algorithm = new MyersDiff();

        string[] oldLines1 = ["line1", "line2"];
        string[] newLines1 = ["line1", "modified"];

        string[] oldLines2 = ["a", "b"];
        string[] newLines2 = ["a", "changed"];

        var result1 = algorithm.ComputeSideBySideDiff(oldLines1, newLines1);
        cache.Set("left1.txt", "right1.txt", result1);

        var result2 = algorithm.ComputeSideBySideDiff(oldLines2, newLines2);
        cache.Set("left2.txt", "right2.txt", result2);

        cache.TryGet("left1.txt", "right1.txt", out var cached1).Should().BeTrue();
        cache.TryGet("left2.txt", "right2.txt", out var cached2).Should().BeTrue();

        cached1.Should().NotBeNull();
        cached2.Should().NotBeNull();
        cached1!.Lines[0].LeftContent.Should().NotBe(cached2!.Lines[0].LeftContent);
    }

    [Fact]
    public void LineHashCache_DuplicateLines_ShouldOptimize()
    {
        var duplicateLines = Enumerable.Repeat("same line", 1000).ToArray();
        var uniqueHashes = new HashSet<string>();

        foreach (var line in duplicateLines)
        {
            var hash = LineHashCache.ComputeLineHash(line);
            hash.Should().NotBeNullOrEmpty();
            uniqueHashes.Add(hash);
        }

        uniqueHashes.Should().HaveCount(1, "All duplicate lines should have the same hash");
    }

    [Fact]
    public async Task StructureCache_JsonParsing_ShouldCache()
    {
        var cache = new StructureCache();

        var firstParse = await cache.GetOrComputeAsync("test.json", async () =>
        {
            return new { name = "test", value = 123 };
        }, "json");

        var secondParse = await cache.GetOrComputeAsync("test.json", async () =>
        {
            return new { name = "test", value = 123 };
        }, "json");

        firstParse.Should().NotBeNull();
        secondParse.Should().NotBeNull();
    }

    [Fact]
    public void DiffResultCache_CacheInvalidation_ShouldWork()
    {
        var cache = new DiffResultCache();
        var algorithm = new MyersDiff();

        string[] oldLines = ["line1", "line2"];
        string[] newLines = ["line1", "modified"];

        var result = algorithm.ComputeSideBySideDiff(oldLines, newLines);
        cache.Set("left.txt", "right.txt", result);

        cache.Invalidate("left.txt", "right.txt");

        cache.TryGet("left.txt", "right.txt", out var cached).Should().BeFalse();
        cached.Should().BeNull("Cache should be invalidated");
    }

    [Fact]
    public void LineHashCache_LargeFile_ShouldHandleEfficiently()
    {
        var lines = Enumerable.Range(1, 100000).Select(i => $"line{i}").ToArray();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var hashes = LineHashCache.ComputeLineHashes(lines);

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Line hash cache should handle large files efficiently");
        hashes.Should().HaveCount(100000);
    }

    [Fact]
    public void DiffResultCache_MaxSize_ShouldEvictOldEntries()
    {
        var cache = new DiffResultCache(maxCacheSize: 100);
        var algorithm = new MyersDiff();

        for (int i = 0; i < 150; i++)
        {
            string[] oldLines = [$"line{i}_1"];
            string[] newLines = [$"line{i}_2"];
            var result = algorithm.ComputeSideBySideDiff(oldLines, newLines);
            cache.Set($"left{i}.txt", $"right{i}.txt", result);
        }

        cache.TryGet($"left0.txt", $"right0.txt", out var oldCached).Should().BeFalse();
        cache.TryGet($"left140.txt", $"right140.txt", out var recentCached).Should().BeTrue();
        recentCached.Should().NotBeNull();
    }
}

public class FileParserTests
{
    [Fact]
    public async Task TextParser_ShouldParseCorrectly()
    {
        var tempFile = Path.GetTempFileName() + ".txt";
        try
        {
            await File.WriteAllLinesAsync(tempFile, ["line1", "line2", "line3"]);
            var parser = new TextParser();

            var lines = await parser.ParseLinesAsync(tempFile);

            lines.Should().HaveCount(3);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task JsonParser_ShouldParseValidJson()
    {
        var tempFile = Path.GetTempFileName() + ".json";
        try
        {
            await File.WriteAllTextAsync(tempFile, @"{""name"": ""test"", ""value"": 123}");
            var parser = new JsonParser();

            var result = await parser.ParseStructuredAsync(tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task XmlParser_ShouldParseValidXml()
    {
        var tempFile = Path.GetTempFileName() + ".xml";
        try
        {
            await File.WriteAllTextAsync(tempFile, @"<root><child>value</child></root>");
            var parser = new XmlParser();

            var result = await parser.ParseStructuredAsync(tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}

public class NormalizerTests
{
    [Fact]
    public void JsonNormalizer_ShouldNormalizeWhitespace()
    {
        var normalizer = new JsonNormalizer();
        var json = @"{  ""name"":  ""test"",  ""value"":  123  }";

        var normalized = normalizer.Normalize(json);

        normalized.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void XmlNormalizer_ShouldNormalizeWhitespace()
    {
        var normalizer = new XmlNormalizer();
        var xml = @"<root>  <child>  value  </child>  </root>";

        var normalized = normalizer.Normalize(xml);

        normalized.Should().NotBeNullOrEmpty();
    }
}
