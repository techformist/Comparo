namespace Comparo.Core.DiffModels;

public class DiffHunk
{
    public int OldLineNumber { get; set; }
    public int OldLineCount { get; set; }
    public int NewLineNumber { get; set; }
    public int NewLineCount { get; set; }
    public ChangeType ChangeType { get; set; }
    public List<DiffLine> Lines { get; set; } = new();

    public DiffHunk(int oldLineNumber, int oldLineCount, int newLineNumber, int newLineCount, ChangeType changeType)
    {
        OldLineNumber = oldLineNumber;
        OldLineCount = oldLineCount;
        NewLineNumber = newLineNumber;
        NewLineCount = newLineCount;
        ChangeType = changeType;
    }
}

public class DiffLine
{
    public ChangeType ChangeType { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OldLineNumber { get; set; }
    public int NewLineNumber { get; set; }

    public DiffLine(ChangeType changeType, string content, int oldLineNumber, int newLineNumber)
    {
        ChangeType = changeType;
        Content = content;
        OldLineNumber = oldLineNumber;
        NewLineNumber = newLineNumber;
    }
}
