using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using FluentAssertions;
using Xunit;

namespace Comparo.Tests;

public class PatienceDiffTests
{
    private readonly PatienceDiff _algorithm;

    public PatienceDiffTests()
    {
        _algorithm = new PatienceDiff();
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
        result.Lines.Should().HaveCountGreaterOrEqualTo(3);
        result.Lines.Should().Contain(l => l.ChangeType == ChangeType.Modified || l.ChangeType == ChangeType.Added || l.ChangeType == ChangeType.Deleted);
    }

    [Fact]
    public void ComputeDiff_BlockMovement_ShouldDetectReordering()
    {
        string[] oldLines = ["a", "b", "c", "d", "e"];
        string[] newLines = ["a", "d", "e", "b", "c"];

        var result = _algorithm.ComputeDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Count(l => l.ChangeType != ChangeType.Unchanged).Should().BeGreaterThan(0);
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
    public void ComputeSideBySideDiff_MultipleChanges_ShouldCountAll()
    {
        string[] oldLines = ["a", "b", "c", "d"];
        string[] newLines = ["a", "b2", "new", "d"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_LargeFile_ShouldHandleEfficiently()
    {
        var oldLines = Enumerable.Range(1, 1000).Select(i => $"line{i}").ToArray();
        var newLines = Enumerable.Range(1, 1000).Select(i => i == 500 ? "changed" : $"line{i}").ToArray();

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(1000);
    }

    [Fact]
    public void ComputeSideBySideDiff_ReorderedBlocks_ShouldDetectChanges()
    {
        string[] oldLines = ["header", "section1", "section2", "section3", "footer"];
        string[] newLines = ["header", "section3", "section1", "section2", "footer"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_DuplicatedLines_ShouldHandleCorrectly()
    {
        string[] oldLines = ["same", "same", "same"];
        string[] newLines = ["same", "same", "same"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_DuplicatedLinesWithChanges_ShouldDetectChanges()
    {
        string[] oldLines = ["same", "same", "same"];
        string[] newLines = ["same", "changed", "same"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().BeGreaterThan(0);
    }
}
