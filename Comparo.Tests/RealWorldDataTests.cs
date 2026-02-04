using Comparo.Core.DiffAlgorithms;
using Comparo.Core.DiffModels;
using Comparo.Core.StructuredComparators;
using FluentAssertions;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Comparo.Tests;

/// <summary>
/// Integration tests using real-world test data files.
/// These tests are explicit/manual run for performance benchmarking against actual files.
/// Test data is managed in the Comparo.TestData submodule.
/// Run explicitly with: dotnet test --filter "FullyQualifiedName~RealWorldDataTests"
/// To enable: Remove the Skip parameter from [Fact] attributes
/// </summary>
public class RealWorldDataTests
{
  private readonly ITestOutputHelper _output;
  private readonly string _testDataPath;

  public RealWorldDataTests(ITestOutputHelper output)
  {
    _output = output;
    _testDataPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "..", "..", "TestData"
    );

    if (!Directory.Exists(_testDataPath))
    {
      _testDataPath = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "..", "..", "..", "TestData"
      );
    }
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void IdenticalFiles_AllAlgorithms_ShouldDetectNoChanges()
  {
    var folder = Path.Combine(_testDataPath, "01_identical_files");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
    _output.WriteLine($"Testing {files.Length} files from: {folder}");

    var algorithms = new List<IDiffAlgorithm>
        {
            new MyersDiff(),
            new PatienceDiff(),
            new HistogramDiff()
        };

    foreach (var file in files.Take(10))
    {
      var lines = File.ReadAllLines(file);
      _output.WriteLine($"  File: {Path.GetFileName(file)} ({lines.Length} lines)");

      foreach (var algorithm in algorithms)
      {
        var sw = Stopwatch.StartNew();
        var result = algorithm.ComputeDiff(lines, lines);
        sw.Stop();

        result.Should().NotBeNull();
        result.Lines.Should().OnlyContain(l => l.ChangeType == ChangeType.Unchanged,
            $"identical files should have no changes - {algorithm.GetType().Name}");
        _output.WriteLine($"    {algorithm.GetType().Name}: {sw.ElapsedMilliseconds}ms");
      }
    }
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void MinorChanges_5Percent_AllAlgorithms_PerformanceTest()
  {
    var folder = Path.Combine(_testDataPath, "02_minor_changes_5percent");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    RunDiffPerformanceTest(folder, "5% changes");
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void ModerateChanges_20Percent_AllAlgorithms_PerformanceTest()
  {
    var folder = Path.Combine(_testDataPath, "03_moderate_changes_20percent");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    RunDiffPerformanceTest(folder, "20% changes");
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void SignificantChanges_50Percent_AllAlgorithms_PerformanceTest()
  {
    var folder = Path.Combine(_testDataPath, "06_significant_changes_50percent");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    RunDiffPerformanceTest(folder, "50% changes");
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void MajorChanges_80Percent_AllAlgorithms_PerformanceTest()
  {
    var folder = Path.Combine(_testDataPath, "07_major_changes_80percent");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    RunDiffPerformanceTest(folder, "80% changes");
  }

  [Fact(Skip = "Explicit run only - requires TestData submodule")]
  public void SalesforceApexRecipes_VersionComparison_ShouldDetectChanges()
  {
    var folder = Path.Combine(_testDataPath, "21_salesforce_apex_recipes");
    if (!Directory.Exists(folder))
    {
      _output.WriteLine($"Skipping test - TestData folder not found: {folder}");
      return;
    }

    var v1Path = Path.Combine(folder, "v1");
    var v2Path = Path.Combine(folder, "v2");

    if (!Directory.Exists(v1Path) || !Directory.Exists(v2Path))
    {
      _output.WriteLine("Skipping test - v1 or v2 folders not found");
      return;
    }

    var v1Files = Directory.GetFiles(v1Path, "*.cls", SearchOption.AllDirectories);
    var v2Files = Directory.GetFiles(v2Path, "*.cls", SearchOption.AllDirectories);

    _output.WriteLine($"Comparing Salesforce Apex code: {v1Files.Length} v1 files vs {v2Files.Length} v2 files");

    var algorithm = new MyersDiff();
    var totalChanges = 0;
    var sw = Stopwatch.StartNew();

    foreach (var v1File in v1Files.Take(10))
    {
      var fileName = Path.GetFileName(v1File);
      var v2File = v2Files.FirstOrDefault(f => Path.GetFileName(f) == fileName);

      if (v2File == null) continue;

      var v1Lines = File.ReadAllLines(v1File);
      var v2Lines = File.ReadAllLines(v2File);

      var diff = algorithm.ComputeDiff(v1Lines, v2Lines);
      var changeCount = diff.Lines.Count(l => l.ChangeType != ChangeType.Unchanged);
      totalChanges += changeCount;

      _output.WriteLine($"  {fileName}: {changeCount} changes");
    }

    sw.Stop();
    _output.WriteLine($"Total: {totalChanges} changes detected in {sw.ElapsedMilliseconds}ms");
  }

  private void RunDiffPerformanceTest(string folder, string description)
  {
    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
        .Where(f => !f.EndsWith(".md") && !f.EndsWith(".json"))
        .ToArray();

    _output.WriteLine($"Testing {description} with {files.Length} files from: {folder}");

    var algorithms = new List<IDiffAlgorithm>
        {
            new MyersDiff(),
            new PatienceDiff(),
            new HistogramDiff()
        };

    // Find original/modified pairs
    var originalFiles = files.Where(f => f.Contains("original") || f.Contains("_v1")).ToArray();
    var modifiedFiles = files.Where(f => f.Contains("modified") || f.Contains("_v2")).ToArray();

    var testCount = Math.Min(originalFiles.Length, modifiedFiles.Length);
    if (testCount == 0)
    {
      _output.WriteLine("No original/modified pairs found");
      return;
    }

    foreach (var algorithm in algorithms)
    {
      var totalTime = 0L;
      var totalChanges = 0;

      for (int i = 0; i < Math.Min(testCount, 5); i++)
      {
        var oldLines = File.ReadAllLines(originalFiles[i]);
        var newLines = File.ReadAllLines(modifiedFiles[i]);

        var sw = Stopwatch.StartNew();
        var result = algorithm.ComputeDiff(oldLines, newLines);
        sw.Stop();

        totalTime += sw.ElapsedMilliseconds;
        totalChanges += result.Lines.Count(l => l.ChangeType != ChangeType.Unchanged);
      }

      _output.WriteLine($"  {algorithm.GetType().Name}: {totalTime}ms total, {totalChanges} changes detected");
    }
  }
}
