using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Comparo.Core.DiffModels;
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;
using SystemChangeType = Comparo.Core.DiffModels.ChangeType;

namespace Comparo.Core.DiffAlgorithms;

public class MyersDiff : IDiffAlgorithm
{
    public DiffHunk ComputeDiff(string[] oldLines, string[] newLines)
    {
        var diff = InlineDiffBuilder.Diff(string.Join("\n", oldLines), string.Join("\n", newLines));

        var hunk = new DiffHunk(1, oldLines.Length, 1, newLines.Length, SystemChangeType.Unchanged);

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

            hunk.Lines.Add(new DiffLine(changeType, line.Text, line.Position ?? 0, line.Position ?? 0));
        }

        return hunk;
    }

    public SideBySideModel ComputeSideBySideDiff(string[] oldLines, string[] newLines)
    {
        var model = new SideBySideModel();
        var diff = SideBySideDiffBuilder.Diff(string.Join("\n", oldLines), string.Join("\n", newLines));

        var leftLines = diff.OldText.Lines.Where(l => l.Type != DiffPlexChangeType.Imaginary).ToList();
        var rightLines = diff.NewText.Lines.Where(l => l.Type != DiffPlexChangeType.Imaginary).ToList();

        int leftIdx = 0;
        int rightIdx = 0;

        while (leftIdx < leftLines.Count || rightIdx < rightLines.Count)
        {
            var leftLine = leftIdx < leftLines.Count ? leftLines[leftIdx] : null;
            var rightLine = rightIdx < rightLines.Count ? rightLines[rightIdx] : null;

            if (leftLine == null && rightLine != null)
            {
                // Only right line exists (Added)
                model.AddLine(new SideBySideDiffLine(null, rightIdx + 1, string.Empty, rightLine.Text ?? string.Empty, SystemChangeType.Added));
                rightIdx++;
            }
            else if (leftLine != null && rightLine == null)
            {
                // Only left line exists (Deleted)
                model.AddLine(new SideBySideDiffLine(leftIdx + 1, null, leftLine.Text ?? string.Empty, string.Empty, SystemChangeType.Deleted));
                leftIdx++;
            }
            else if (leftLine != null && rightLine != null)
            {
                if (leftLine.Type == DiffPlexChangeType.Unchanged && rightLine.Type == DiffPlexChangeType.Unchanged)
                {
                    // Both unchanged
                    model.AddLine(new SideBySideDiffLine(leftIdx + 1, rightIdx + 1, leftLine.Text ?? string.Empty, rightLine.Text ?? string.Empty, SystemChangeType.Unchanged));
                    leftIdx++;
                    rightIdx++;
                }
                else if (leftLine.Type == DiffPlexChangeType.Modified && rightLine.Type == DiffPlexChangeType.Modified)
                {
                    // Both modified
                    model.AddLine(new SideBySideDiffLine(leftIdx + 1, rightIdx + 1, leftLine.Text ?? string.Empty, rightLine.Text ?? string.Empty, SystemChangeType.Modified));
                    leftIdx++;
                    rightIdx++;
                }
                else if (leftLine.Type == DiffPlexChangeType.Deleted)
                {
                    // Left is deleted
                    model.AddLine(new SideBySideDiffLine(leftIdx + 1, null, leftLine.Text ?? string.Empty, string.Empty, SystemChangeType.Deleted));
                    leftIdx++;
                }
                else if (rightLine.Type == DiffPlexChangeType.Inserted)
                {
                    // Right is inserted
                    model.AddLine(new SideBySideDiffLine(null, rightIdx + 1, string.Empty, rightLine.Text ?? string.Empty, SystemChangeType.Added));
                    rightIdx++;
                }
                else
                {
                    // Fallback: treat as unchanged
                    model.AddLine(new SideBySideDiffLine(leftIdx + 1, rightIdx + 1, leftLine.Text ?? string.Empty, rightLine.Text ?? string.Empty, SystemChangeType.Unchanged));
                    leftIdx++;
                    rightIdx++;
                }
            }
        }

        return model;
    }
}
