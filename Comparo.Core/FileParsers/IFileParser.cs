namespace Comparo.Core.FileParsers;

public enum FileType
{
    Text,
    Markdown,
    Json,
    Xml
}

public interface IFileParser
{
    FileType FileType { get; }

    bool CanParse(string filePath);

    Task<string[]> ParseLinesAsync(string filePath, CancellationToken cancellationToken = default);

    Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IStructuredParser : IFileParser
{
    Task<object> ParseStructuredAsync(string filePath, CancellationToken cancellationToken = default);

    Task<string> NormalizeAsync(string filePath, CancellationToken cancellationToken = default);
}
