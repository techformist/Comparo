using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using FluentAssertions;
using Xunit;

namespace Comparo.Tests;

public class HistogramDiffTests
{
    private readonly HistogramDiff _algorithm;

    public HistogramDiffTests()
    {
        _algorithm = new HistogramDiff();
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
        result.Lines.Should().Contain(l => l.ChangeType != ChangeType.Unchanged);
    }

    [Fact]
    public void ComputeDiff_LargeFileWithReordering_ShouldDetectChanges()
    {
        var oldLines = Enumerable.Range(1, 1000).Select(i => $"line{i}").ToArray();
        var newLines = Enumerable.Range(1, 1000).OrderByDescending(i => i).Select(i => $"line{i}").ToArray();

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
    public void ComputeSideBySideDiff_ManyDuplicates_ShouldHandleCorrectly()
    {
        string[] oldLines = ["same", "same", "same", "same", "same"];
        string[] newLines = ["same", "same", "changed", "same", "same"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_DuplicateBlocksMoved_ShouldDetectReordering()
    {
        string[] oldLines = ["block", "block", "different", "block", "block"];
        string[] newLines = ["block", "block", "block", "block", "different"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeSideBySideDiff_PerformanceWithManyChanges_ShouldComplete()
    {
        var oldLines = Enumerable.Range(1, 5000).Select(i => $"old{i}").ToArray();
        var newLines = Enumerable.Range(1, 5000).Select(i => $"new{i}").ToArray();

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        // When equal numbers of completely different lines exist, they are treated as modifications
        result.Lines.Should().HaveCount(5000);
        result.ModifiedCount.Should().Be(5000);
    }

    [Fact]
    public void ComputeSideBySideDiff_SimilarLines_ShouldDetectDifferences()
    {
        string[] oldLines = ["function test()", "  return 1;", "}", "function other()", "  return 2;", "}"];
        string[] newLines = ["function test()", "  return 10;", "}", "function other()", "  return 20;", "}"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.ModifiedCount.Should().Be(2);
    }

    [Fact]
    public void ComputeSideBySideDiff_ComplexReordering_ShouldDetectChanges()
    {
        string[] oldLines = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
        string[] newLines = ["10", "1", "9", "2", "8", "3", "7", "4", "6", "5"];

        var result = _algorithm.ComputeSideBySideDiff(oldLines, newLines);

        result.Should().NotBeNull();
        result.TotalChanges.Should().BeGreaterThan(0);
    }
}
