using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using Comparo.Core.StructuredComparators;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Comparo.Tests;

public class EndToEndWorkflowTests
{
    [Fact]
    public void Workflow_FileSelection_Diff_Navigation_Complete()
    {
        var algorithm = new MyersDiff();

        string leftContent = @"line1
line2
line3
line4
line5";
        string rightContent = @"line1
modified
line3
line4
new line
line5";

        var oldLines = leftContent.Split('\n');
        var newLines = rightContent.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        diff.Should().NotBeNull();
        diff.TotalChanges.Should().Be(2);

        var changedLine = diff.Lines.FirstOrDefault(l => l.ChangeType == ChangeType.Modified);
        changedLine.Should().NotBeNull();
        changedLine!.LeftContent.Should().Be("line2");
        changedLine!.RightContent.Should().Be("modified");

        var addedLine = diff.Lines.FirstOrDefault(l => l.ChangeType == ChangeType.Added);
        addedLine.Should().NotBeNull();
        addedLine!.RightContent.Should().Be("new line");
    }

    [Fact]
    public void Workflow_DragAndDrop_Simulation_Complete()
    {
        var algorithm = new PatienceDiff();

        var droppedFile1Content = @"file1 line1
file1 line2
file1 line3";

        var droppedFile2Content = @"file2 line1
file2 line2
file2 line3";

        var file1Lines = droppedFile1Content.Split('\n');
        var file2Lines = droppedFile2Content.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(file1Lines, file2Lines);

        diff.Should().NotBeNull();
        diff.TotalChanges.Should().Be(3);
        diff.Lines.All(l => l.ChangeType == ChangeType.Modified).Should().BeTrue();
    }

    [Fact]
    public void Workflow_ExportFunctionality_Simulation_Complete()
    {
        var algorithm = new HistogramDiff();

        string leftContent = @"original line1
original line2
original line3";

        string rightContent = @"modified line1
original line2
modified line3";

        var oldLines = leftContent.Split('\n');
        var newLines = rightContent.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var exportedDiff = new StringBuilder();
        exportedDiff.AppendLine("=== Side by Side Diff ===");
        exportedDiff.AppendLine("Left | Right | Change Type");
        exportedDiff.AppendLine("---------------------------");

        foreach (var line in diff.Lines)
        {
            var left = line.LeftContent ?? "(empty)";
            var right = line.RightContent ?? "(empty)";
            exportedDiff.AppendLine($"{left} | {right} | {line.ChangeType}");
        }

        var exportContent = exportedDiff.ToString();

        exportContent.Should().Contain("modified line1");
        exportContent.Should().Contain("Modified");
        exportContent.Should().Contain("=== Side by Side Diff ===");
    }

    [Fact]
    public void Workflow_SettingsPersistence_Simulation_Complete()
    {
        var settings = new DiffSettings
        {
            Algorithm = "Myers",
            IgnoreWhitespace = false,
            CaseSensitive = true,
            ShowLineNumbers = true
        };

        var serialized = System.Text.Json.JsonSerializer.Serialize(settings);

        serialized.Should().NotBeNullOrEmpty();
        serialized.Should().Contain("Myers");
        serialized.Should().Contain("CaseSensitive");

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<DiffSettings>(serialized);

        deserialized.Should().NotBeNull();
        deserialized!.Algorithm.Should().Be("Myers");
        deserialized!.IgnoreWhitespace.Should().BeFalse();
        deserialized!.CaseSensitive.Should().BeTrue();
        deserialized!.ShowLineNumbers.Should().BeTrue();
    }

    [Fact]
    public void Workflow_CompleteFileComparison_AllSteps()
    {
        var algorithm = new MyersDiff();

        var leftFile = @"Header
Section 1
Content 1
Content 2
Footer";

        var rightFile = @"Header
Section 1 Modified
Content 1
Content 2 Added
Footer";

        var oldLines = leftFile.Split('\n');
        var newLines = rightFile.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        diff.Should().NotBeNull();
        diff.TotalChanges.Should().Be(2);

        var unchangedLine = diff.Lines.First(l => l.LeftContent == "Content 1");
        unchangedLine.ChangeType.Should().Be(ChangeType.Unchanged);
        unchangedLine.LeftLineNumber.Should().Be(3);
        unchangedLine.RightLineNumber.Should().Be(3);

        var modifiedLine = diff.Lines.First(l => l.LeftContent == "Section 1");
        modifiedLine.ChangeType.Should().Be(ChangeType.Modified);
    }

    [Fact]
    public void Workflow_NextPreviousChange_Complete()
    {
        var algorithm = new PatienceDiff();

        var leftContent = @"line1
line2
line3
line4
line5
line6
line7
line8
line9
line10";

        var rightContent = @"line1
modified1
line3
line4
modified2
line6
line7
modified3
line9
line10";

        var oldLines = leftContent.Split('\n');
        var newLines = rightContent.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var changes = diff.Lines.Where(l => l.ChangeType != ChangeType.Unchanged).ToList();

        changes.Should().HaveCount(3);

        var firstChange = changes[0];
        firstChange.ChangeType.Should().Be(ChangeType.Modified);

        var secondChange = changes[1];
        secondChange.ChangeType.Should().Be(ChangeType.Modified);

        var thirdChange = changes[2];
        thirdChange.ChangeType.Should().Be(ChangeType.Modified);
    }

    [Fact]
    public void Workflow_SemanticAwareComparison_Complete()
    {
        var jsonComparator = new JsonSemanticComparator();
        var myersDiff = new MyersDiff();

        var leftJson = @"{
    ""name"": ""Alice"",
    ""age"": 30,
    ""city"": ""NYC""
}";

        var rightJson = @"{
    ""age"": 30,
    ""name"": ""Alice"",
    ""city"": ""SF""
}";

        var semanticChanges = jsonComparator.Compare(leftJson, rightJson);

        semanticChanges.Should().NotBeEmpty();
        semanticChanges.Should().Contain(c => c.Path.Contains("city"));

        var oldLines = leftJson.Split('\n');
        var newLines = rightJson.Split('\n');

        var lineDiff = myersDiff.ComputeSideBySideDiff(oldLines, newLines);

        lineDiff.Should().NotBeNull();
        lineDiff.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Workflow_FileComparisonWithFiltering_Complete()
    {
        var algorithm = new HistogramDiff();

        var leftContent = @"TODO: implement feature X
TODO: fix bug Y
Actual code line 1
Actual code line 2
TODO: add test Z";

        var rightContent = @"TODO: implement feature X
Actual code line 1
Actual code line 2 modified
TODO: add test Z";

        var oldLines = leftContent.Split('\n');
        var newLines = rightContent.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var filteredDiff = new SideBySideModel();
        foreach (var line in diff.Lines)
        {
            if (!line.LeftContent?.StartsWith("TODO") == true &&
                !line.RightContent?.StartsWith("TODO") == true)
            {
                filteredDiff.AddLine(line);
            }
        }

        filteredDiff.TotalChanges.Should().Be(1);
        filteredDiff.Lines.Should().Contain(l => l.RightContent == "Actual code line 2 modified");
    }

    [Fact]
    public void Workflow_MultiFileComparison_Complete()
    {
        var algorithm = new MyersDiff();

        var files = new Dictionary<string, (string[] oldLines, string[] newLines)>
        {
            ["file1.txt"] = (
                ["line1", "line2", "line3"],
                ["line1", "modified", "line3"]
            ),
            ["file2.txt"] = (
                ["a", "b", "c"],
                ["a", "b", "changed"]
            ),
            ["file3.txt"] = (
                ["x", "y", "z"],
                ["x", "y", "z"]
            )
        };

        var results = new Dictionary<string, SideBySideModel>();

        foreach (var file in files)
        {
            var diff = algorithm.ComputeSideBySideDiff(file.Value.oldLines, file.Value.newLines);
            results[file.Key] = diff;
        }

        results.Should().HaveCount(3);
        results["file1.txt"].TotalChanges.Should().Be(1);
        results["file2.txt"].TotalChanges.Should().Be(1);
        results["file3.txt"].TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Workflow_ExportUnifiedFormat_Complete()
    {
        var algorithm = new MyersDiff();

        var leftContent = @"line1
line2
line3
line4";

        var rightContent = @"line1
modified
line3
new line";

        var oldLines = leftContent.Split('\n');
        var newLines = rightContent.Split('\n');

        var diff = algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var export = new StringBuilder();
        export.AppendLine("--- Original");
        export.AppendLine("+++ Modified");
        export.AppendLine("@@ -1,4 +1,4 @@");

        foreach (var line in diff.Lines)
        {
            switch (line.ChangeType)
            {
                case ChangeType.Deleted:
                    export.AppendLine($"-{line.LeftContent}");
                    break;
                case ChangeType.Added:
                    export.AppendLine($"+{line.RightContent}");
                    break;
                case ChangeType.Modified:
                    export.AppendLine($"-{line.LeftContent}");
                    export.AppendLine($"+{line.RightContent}");
                    break;
                default:
                    export.AppendLine($" {line.LeftContent}");
                    break;
            }
        }

        var exportContent = export.ToString();

        exportContent.Should().Contain("--- Original");
        exportContent.Should().Contain("+++ Modified");
        exportContent.Should().Contain("-line2");
        exportContent.Should().Contain("+modified");
        exportContent.Should().Contain("+new line");
    }
}

public class DiffSettings
{
    public string Algorithm { get; set; } = "Myers";
    public bool IgnoreWhitespace { get; set; }
    public bool CaseSensitive { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = true;
}
