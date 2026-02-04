namespace Comparo.Core.DiffModels;

public class DiffBlock
{
    public ChangeType ChangeType { get; set; }
    public int OldLineNumber { get; set; }
    public int NewLineNumber { get; set; }
    public string OldContent { get; set; } = string.Empty;
    public string NewContent { get; set; } = string.Empty;
    public int Length { get; set; }
    public string? SemanticPath { get; set; }

    public DiffBlock(ChangeType changeType, int oldLineNumber, int newLineNumber, string oldContent, string newContent, int length = 1)
    {
        ChangeType = changeType;
        OldLineNumber = oldLineNumber;
        NewLineNumber = newLineNumber;
        OldContent = oldContent;
        NewContent = newContent;
        Length = length;
    }

    public DiffBlock(ChangeType changeType, int oldLineNumber, int newLineNumber, string content)
        : this(changeType, oldLineNumber, newLineNumber, content, content)
    {
    }

    public bool IsUnchanged => ChangeType == ChangeType.Unchanged;
    public bool IsAdded => ChangeType == ChangeType.Added;
    public bool IsDeleted => ChangeType == ChangeType.Deleted;
    public bool IsModified => ChangeType == ChangeType.Modified;
    public bool IsReordered => ChangeType == ChangeType.Reordered;
}
