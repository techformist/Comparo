using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Comparo.Core.DiffModels;
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;
using SystemChangeType = Comparo.Core.DiffModels.ChangeType;

namespace Comparo.Core.DiffAlgorithms;

public class HuntSzymanskiDiff : IDiffAlgorithm
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
        var diff = InlineDiffBuilder.Diff(string.Join("\n", oldLines), string.Join("\n", newLines));

        int oldIndex = 0;
        int newIndex = 0;

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

            int? leftLineNumber = null;
            int? rightLineNumber = null;
            string leftContent = "";
            string rightContent = "";

            switch (changeType)
            {
                case SystemChangeType.Unchanged:
                    leftLineNumber = ++oldIndex;
                    rightLineNumber = ++newIndex;
                    leftContent = oldLines[leftLineNumber.Value - 1];
                    rightContent = newLines[rightLineNumber.Value - 1];
                    break;

                case SystemChangeType.Added:
                    rightLineNumber = ++newIndex;
                    rightContent = newLines[rightLineNumber.Value - 1];
                    break;

                case SystemChangeType.Deleted:
                    leftLineNumber = ++oldIndex;
                    leftContent = oldLines[leftLineNumber.Value - 1];
                    break;

                case SystemChangeType.Modified:
                    leftLineNumber = ++oldIndex;
                    rightLineNumber = ++newIndex;
                    leftContent = oldLines[leftLineNumber.Value - 1];
                    rightContent = newLines[rightLineNumber.Value - 1];
                    break;
            }

            model.AddLine(new SideBySideDiffLine(leftLineNumber, rightLineNumber, leftContent, rightContent, changeType));
        }

        return model;
    }
}
