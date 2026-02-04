using System.Text;

namespace Comparo.Core.FileParsers;

public class TextParser : IFileParser
{
    public FileType FileType => FileType.Text;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".csv", ".tsv", ".ini", ".cfg", ".conf", ".properties",
        ".yml", ".yaml", ".toml", ".env"
    };

    public bool CanParse(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return TextExtensions.Contains(extension) || string.IsNullOrEmpty(extension);
    }

    public async Task<string[]> ParseLinesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        
        await foreach (var line in ReadLinesAsync(filePath, cancellationToken))
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    public async Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private async IAsyncEnumerable<string> ReadLinesAsync(string filePath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, bufferSize: 4096);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            yield return line;
        }
    }
}
