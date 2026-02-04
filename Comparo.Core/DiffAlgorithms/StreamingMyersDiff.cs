using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Comparo.Core.DiffModels;
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;
using SystemChangeType = Comparo.Core.DiffModels.ChangeType;

namespace Comparo.Core.DiffAlgorithms;

/// <summary>
/// Streaming implementation of Myers diff algorithm that works with chunked loaders
/// to minimize memory footprint for large files.
/// </summary>
public class StreamingMyersDiff : IStreamingDiffAlgorithm
{
  private readonly int _chunkSize;

  /// <summary>
  /// Initializes a new instance of StreamingMyersDiff.
  /// </summary>
  /// <param name="chunkSize">Size of chunks to process at a time (default: 5000 lines)</param>
  public StreamingMyersDiff(int chunkSize = 5000)
  {
    _chunkSize = chunkSize > 0 ? chunkSize : 5000;
  }

  public async Task<DiffHunk> ComputeDiffAsync(
      ChunkedTextLoader oldLoader,
      ChunkedTextLoader newLoader,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(oldLoader);
    ArgumentNullException.ThrowIfNull(newLoader);

    var oldLineCount = oldLoader.TotalLineCount;
    var newLineCount = newLoader.TotalLineCount;

    var hunk = new DiffHunk(1, oldLineCount, 1, newLineCount, SystemChangeType.Unchanged);

    // Process files in chunks to avoid loading entire file into memory
    var oldPos = 0;
    var newPos = 0;

    while (oldPos < oldLineCount || newPos < newLineCount)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var oldChunk = await Task.Run(() =>
          oldLoader.GetLineRange(oldPos, Math.Min(_chunkSize, oldLineCount - oldPos), cancellationToken),
          cancellationToken);

      var newChunk = await Task.Run(() =>
          newLoader.GetLineRange(newPos, Math.Min(_chunkSize, newLineCount - newPos), cancellationToken),
          cancellationToken);

      // Use DiffPlex on this chunk
      var diff = InlineDiffBuilder.Diff(
          string.Join("\n", oldChunk),
          string.Join("\n", newChunk));

      foreach (var line in diff.Lines)
      {
        SystemChangeType changeType = line.Type switch
        {
          DiffPlexChangeType.Inserted => SystemChangeType.Added,
          DiffPlexChangeType.Deleted => SystemChangeType.Deleted,
          DiffPlexChangeType.Imaginary => SystemChangeType.Unchanged,
          DiffPlexChangeType.Unchanged => SystemChangeType.Unchanged,
          DiffPlexChangeType.Modified => SystemChangeType.Modified,
          _ => SystemChangeType.Unchanged
        };

        var adjustedPosition = (line.Position ?? 0) + (changeType == SystemChangeType.Deleted ? oldPos : newPos);
        hunk.Lines.Add(new DiffLine(changeType, line.Text, adjustedPosition, adjustedPosition));
      }

      oldPos += oldChunk.Length;
      newPos += newChunk.Length;
    }

    return hunk;
  }

  public async Task<SideBySideModel> ComputeSideBySideDiffAsync(
      ChunkedTextLoader oldLoader,
      ChunkedTextLoader newLoader,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(oldLoader);
    ArgumentNullException.ThrowIfNull(newLoader);

    var model = new SideBySideModel();
    var oldLineCount = oldLoader.TotalLineCount;
    var newLineCount = newLoader.TotalLineCount;

    var oldPos = 0;
    var newPos = 0;

    while (oldPos < oldLineCount || newPos < newLineCount)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var oldChunk = await Task.Run(() =>
          oldLoader.GetLineRange(oldPos, Math.Min(_chunkSize, oldLineCount - oldPos), cancellationToken),
          cancellationToken);

      var newChunk = await Task.Run(() =>
          newLoader.GetLineRange(newPos, Math.Min(_chunkSize, newLineCount - newPos), cancellationToken),
          cancellationToken);

      var diff = SideBySideDiffBuilder.Diff(
          string.Join("\n", oldChunk),
          string.Join("\n", newChunk));

      var leftLines = diff.OldText.Lines.Where(l => l.Type != DiffPlexChangeType.Imaginary).ToList();
      var rightLines = diff.NewText.Lines.Where(l => l.Type != DiffPlexChangeType.Imaginary).ToList();

      int leftIdx = 0;
      int rightIdx = 0;

      while (leftIdx < leftLines.Count || rightIdx < rightLines.Count)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var leftLine = leftIdx < leftLines.Count ? leftLines[leftIdx] : null;
        var rightLine = rightIdx < rightLines.Count ? rightLines[rightIdx] : null;

        if (leftLine == null && rightLine != null)
        {
          model.AddLine(new SideBySideDiffLine(
              null,
              newPos + rightIdx + 1,
              string.Empty,
              rightLine.Text ?? string.Empty,
              SystemChangeType.Added));
          rightIdx++;
        }
        else if (leftLine != null && rightLine == null)
        {
          model.AddLine(new SideBySideDiffLine(
              oldPos + leftIdx + 1,
              null,
              leftLine.Text ?? string.Empty,
              string.Empty,
              SystemChangeType.Deleted));
          leftIdx++;
        }
        else if (leftLine != null && rightLine != null)
        {
          SystemChangeType changeType = DetermineChangeType(leftLine.Type, rightLine.Type);
          model.AddLine(new SideBySideDiffLine(
              oldPos + leftIdx + 1,
              newPos + rightIdx + 1,
              leftLine.Text ?? string.Empty,
              rightLine.Text ?? string.Empty,
              changeType));

          if (changeType != SystemChangeType.Deleted)
            rightIdx++;
          if (changeType != SystemChangeType.Added)
            leftIdx++;
        }
      }

      oldPos += oldChunk.Length;
      newPos += newChunk.Length;
    }

    return model;
  }

  private static SystemChangeType DetermineChangeType(DiffPlexChangeType leftType, DiffPlexChangeType rightType)
  {
    if (leftType == DiffPlexChangeType.Unchanged && rightType == DiffPlexChangeType.Unchanged)
      return SystemChangeType.Unchanged;
    if (leftType == DiffPlexChangeType.Modified && rightType == DiffPlexChangeType.Modified)
      return SystemChangeType.Modified;
    if (leftType == DiffPlexChangeType.Deleted)
      return SystemChangeType.Deleted;
    if (rightType == DiffPlexChangeType.Inserted)
      return SystemChangeType.Added;

    return SystemChangeType.Unchanged;
  }
}
