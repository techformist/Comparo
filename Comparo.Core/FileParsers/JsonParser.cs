using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Comparo.Core.FileParsers;

public class JsonParser : IStructuredParser
{
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB
    private const int MaxLineCount = 10_000_000; // 10 million lines
    private const int MaxDepth = 100;

    public FileType FileType => FileType.Json;

    private static readonly HashSet<string> JsonExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json", ".jsonc"
    };

    public bool CanParse(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return JsonExtensions.Contains(extension);
    }

    public async Task<string[]> ParseLinesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await ParseContentAsync(filePath, cancellationToken);
        return SplitIntoLines(content);
    }

    public async Task<string> ParseContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidateFileSize(filePath);

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, bufferSize: 65536);
        var content = await reader.ReadToEndAsync(cancellationToken);

        ValidateContent(content);
        return content;
    }

    public async Task<object> ParseStructuredAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await ParseContentAsync(filePath, cancellationToken);

        var settings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            LineInfoHandling = LineInfoHandling.Load
        };

        JToken token;
        using (var stringReader = new StringReader(content))
        using (var jsonReader = new JsonTextReader(stringReader))
        {
            jsonReader.MaxDepth = MaxDepth;
            jsonReader.DateParseHandling = DateParseHandling.None;

            token = JToken.ReadFrom(jsonReader, settings);
        }

        return token;
    }

    public async Task<string> NormalizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var jToken = (JToken)await ParseStructuredAsync(filePath, cancellationToken);
        var normalized = NormalizeToken(jToken);
        return normalized.ToString(Formatting.Indented);
    }

    private void ValidateFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size ({fileInfo.Length:N0} bytes) exceeds maximum allowed size ({MaxFileSizeBytes:N0} bytes)");
        }
    }

    private void ValidateContent(string content)
    {
        var lineCount = content.Count(c => c == '\n') + 1;
        if (lineCount > MaxLineCount)
        {
            throw new InvalidOperationException($"Line count ({lineCount:N0}) exceeds maximum allowed count ({MaxLineCount:N0})");
        }
    }

    private JToken NormalizeToken(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => NormalizeObject((JObject)token),
            JTokenType.Array => NormalizeArray((JArray)token),
            _ => token.DeepClone()
        };
    }

    private JObject NormalizeObject(JObject obj)
    {
        var normalized = new JObject();

        foreach (var property in obj.Properties().OrderBy(p => p.Name))
        {
            normalized.Add(property.Name, NormalizeToken(property.Value));
        }

        return normalized;
    }

    private JArray NormalizeArray(JArray array)
    {
        var normalized = new JArray();

        foreach (var item in array)
        {
            normalized.Add(NormalizeToken(item));
        }

        return normalized;
    }

    private static string[] SplitIntoLines(string content)
    {
        return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }
}
