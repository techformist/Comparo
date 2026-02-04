namespace Comparo.Core.DiffModels;

public enum JsonPathOperation
{
    Add,
    Remove,
    Replace,
    Move,
    Copy,
    Test
}

public class JsonPathChange
{
    public JsonPathOperation Operation { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? FromPath { get; set; }
    public object? Value { get; set; }
    public object? OldValue { get; set; }

    public JsonPathChange(JsonPathOperation operation, string path, object? value = null, string? fromPath = null)
    {
        Operation = operation;
        Path = path;
        Value = value;
        FromPath = fromPath;
    }

    public bool IsAdd => Operation == JsonPathOperation.Add;
    public bool IsRemove => Operation == JsonPathOperation.Remove;
    public bool IsReplace => Operation == JsonPathOperation.Replace;
    public bool IsMove => Operation == JsonPathOperation.Move;
}
