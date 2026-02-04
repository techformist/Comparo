using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using FluentAssertions;
using Xunit;

namespace Comparo.Tests;

public class MyersDiffTests
{
    private readonly MyersDiff _algorithm;

    public MyersDiffTests()
    {
        _algorithm = new MyersDiff();
    }

    [Fact]
    public void ComputeDiff_IdenticalFiles_ShouldHaveNoChanges()
    {
        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = ["line1", "line2", "line3"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(3);
        result.Lines.Should().OnlyContain(l => l.ChangeType == ChangeType.Unchanged);
    }

    [Fact]
    public void ComputeDiff_AllLinesAdded_ShouldDetectAllAdds()
    {
        string[] oldLines = [];
        string[] newLines = ["line1", "line2", "line3"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(3);
        result.Lines.Should().OnlyContain(l => l.ChangeType == ChangeType.Added);
    }

    [Fact]
    public void ComputeDiff_AllLinesDeleted_ShouldDetectAllDeletes()
    {
        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = [];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(3);
        result.Lines.Should().OnlyContain(l => l.ChangeType == ChangeType.Deleted);
    }

    [Fact]
    public void ComputeDiff_SingleChangeInMiddle_ShouldDetectChange()
    {
        string[] oldLines = ["line1", "line2", "line3", "line4"];
        string[] newLines = ["line1", "modified", "line3", "line4"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        // DiffPlex reports single line changes as Delete + Add
        result.Lines.Should().HaveCount(5);
        result.Lines[0].ChangeType.Should().Be(ChangeType.Unchanged);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Deleted);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Added);
        result.Lines[3].ChangeType.Should().Be(ChangeType.Unchanged);
        result.Lines[4].ChangeType.Should().Be(ChangeType.Unchanged);
    }

    [Fact]
    public void ComputeDiff_MultipleChanges_ShouldDetectAllChanges()
    {
        string[] oldLines = ["a", "b", "c", "d", "e"];
        string[] newLines = ["a", "b2", "c", "d2", "e"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        // DiffPlex reports changes as Delete+Add pairs, not Modified
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Deleted);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Added);
    }

    [Fact]
    public void ComputeDiff_InsertAndDelete_ShouldDetectBoth()
    {
        string[] oldLines = ["a", "b", "c", "d"];
        string[] newLines = ["a", "new", "b", "d"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Added);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Deleted);
    }

    [Fact]
    public void ComputeDiff_EmptyFiles_ShouldHandleGracefully()
    {
        string[] oldLines = [];
        string[] newLines = [];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public void ComputeSideBySideDiff_IdenticalFiles_ShouldHaveNoChanges()
    {
        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = ["line1", "line2", "line3"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(3);
        result.Lines.Should().OnlyContain(l => l.ChangeType == ChangeType.Unchanged);
        result.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_SingleChange_ShouldHaveCorrectCounts()
    {
        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = ["line1", "changed", "line3"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.ModifiedCount.Should().Be(1);
        result.TotalChanges.Should().Be(1);
        result.Lines[1].ChangeType.Should().Be(ChangeType.Modified);
        result.Lines[1].LeftContent.Should().Be("line2");
        result.Lines[1].RightContent.Should().Be("changed");
    }

    [Fact]
    public void ComputeSideBySideDiff_AddedLines_ShouldCountCorrectly()
    {
        string[] oldLines = ["a", "b"];
        string[] newLines = ["a", "new", "b"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.AddedCount.Should().Be(1);
        result.TotalChanges.Should().Be(1);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Added && l.RightContent == "new");
    }

    [Fact]
    public void ComputeSideBySideDiff_DeletedLines_ShouldCountCorrectly()
    {
        string[] oldLines = ["a", "delete", "b"];
        string[] newLines = ["a", "b"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.DeletedCount.Should().Be(1);
        result.TotalChanges.Should().Be(1);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Deleted && l.LeftContent == "delete");
    }

    [Fact]
    public void ComputeSideBySideDiff_MultipleChangeTypes_ShouldCountAll()
    {
        string[] oldLines = ["a", "b", "c", "d"];
        string[] newLines = ["a", "b2", "new", "d"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        // DiffPlex treats "c" delete + "b2" and "new" as adds = 2 total changes
        result.TotalChanges.Should().Be(2);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Deleted);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Added);
    }

    [Fact]
    public void ComputeSideBySideDiff_LineNumbers_ShouldBeCorrect()
    {
        string[] oldLines = ["line1", "line2", "line3"];
        string[] newLines = ["line1", "modified", "line3"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Lines[0].LeftLineNumber.Should().Be(1);
        result.Lines[0].RightLineNumber.Should().Be(1);
        result.Lines[1].LeftLineNumber.Should().Be(2);
        result.Lines[1].RightLineNumber.Should().Be(2);
        result.Lines[2].LeftLineNumber.Should().Be(3);
        result.Lines[2].RightLineNumber.Should().Be(3);
    }

    [Fact]
    public void ComputeSideBySideDiff_AddedLine_ShouldHaveNullLeftNumber()
    {
        string[] oldLines = ["a", "b"];
        string[] newLines = ["a", "new", "b"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var addedLine = result.Lines.First(l => l.ChangeType == ChangeType.Added);
        addedLine.LeftLineNumber.Should().BeNull();
        addedLine.RightLineNumber.Should().NotBeNull();
    }

    [Fact]
    public void ComputeSideBySideDiff_DeletedLine_ShouldHaveNullRightNumber()
    {
        string[] oldLines = ["a", "delete", "b"];
        string[] newLines = ["a", "b"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        var deletedLine = result.Lines.First(l => l.ChangeType == ChangeType.Deleted);
        deletedLine.LeftLineNumber.Should().NotBeNull();
        deletedLine.RightLineNumber.Should().BeNull();
    }
}
