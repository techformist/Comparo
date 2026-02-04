namespace Comparo.Core.DiffModels;

public class SideBySideDiffLine
{
    public int? LeftLineNumber { get; set; }
    public int? RightLineNumber { get; set; }
    public string LeftContent { get; set; } = string.Empty;
    public string RightContent { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public string? SemanticPath { get; set; }

    public SideBySideDiffLine(int? leftLineNumber, int? rightLineNumber, string leftContent, string rightContent, ChangeType changeType, string? semanticPath = null)
    {
        LeftLineNumber = leftLineNumber;
        RightLineNumber = rightLineNumber;
        LeftContent = leftContent;
        RightContent = rightContent;
        ChangeType = changeType;
        SemanticPath = semanticPath;
    }
}

public class SideBySideModel
{
    public List<SideBySideDiffLine> Lines { get; set; } = new();
    public int TotalChanges { get; set; }
    public int AddedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ModifiedCount { get; set; }
    public int ReorderedCount { get; set; }

    public void AddLine(SideBySideDiffLine line)
    {
        Lines.Add(line);

        switch (line.ChangeType)
        {
            case ChangeType.Added:
                AddedCount++;
                TotalChanges++;
                break;
            case ChangeType.Deleted:
                DeletedCount++;
                TotalChanges++;
                break;
            case ChangeType.Modified:
                ModifiedCount++;
                TotalChanges++;
                break;
            case ChangeType.Reordered:
                ReorderedCount++;
                TotalChanges++;
                break;
        }
    }

    public void Clear()
    {
        Lines.Clear();
        TotalChanges = 0;
        AddedCount = 0;
        DeletedCount = 0;
        ModifiedCount = 0;
        ReorderedCount = 0;
    }
}
