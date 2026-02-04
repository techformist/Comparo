using System.Xml;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Comparo.Core.FileParsers;

public class XmlParser : IStructuredParser
{
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB
    private const int MaxLineCount = 10_000_000; // 10 million lines

    public FileType FileType => FileType.Xml;

    private static readonly HashSet<string> XmlExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xml", ".xaml", ".axaml", ".config", ".csproj", ".fsproj", ".vbproj",
        ".resx", ".svg", ".rss", ".atom", ".wsdl", ".xsd", ".xsl", ".xslt"
    };

    private readonly IBrowsingContext _browsingContext;

    public XmlParser()
    {
        // Do NOT use WithDefaultLoader() - it can fetch external resources
        var config = Configuration.Default;
        _browsingContext = BrowsingContext.New(config);
    }

    public bool CanParse(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return XmlExtensions.Contains(extension);
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
        ValidateXmlSecure(content);
        return content;
    }

    public async Task<object> ParseStructuredAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await ParseContentAsync(filePath, cancellationToken);
        var parser = new HtmlParser();
        return await parser.ParseDocumentAsync(content, cancellationToken);
    }

    public async Task<string> NormalizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var document = (IDocument)await ParseStructuredAsync(filePath, cancellationToken);
        return NormalizeDocument(document);
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

    private void ValidateXmlSecure(string content)
    {
        // Use XmlReader with secure settings to detect XXE and other attacks
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = MaxFileSizeBytes
        };

        try
        {
            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader, settings);

            // Just validate, don't process
            while (xmlReader.Read())
            {
                // Read through document to trigger validation
            }
        }
        catch (XmlException ex)
        {
            // Check if it's a security issue
            if (ex.Message.Contains("DTD") || ex.Message.Contains("entity"))
            {
                throw new InvalidOperationException("XML document contains prohibited DTD or entity references", ex);
            }
            // Re-throw other XML errors
            throw;
        }
    }

    private string NormalizeDocument(IDocument document)
    {
        var normalized = NormalizeNode(document.DocumentElement);
        return normalized.ToHtml();
    }

    private INode NormalizeNode(INode node)
    {
        if (node is IElement element)
        {
            return NormalizeElement(element);
        }

        return node.Clone(true);
    }

    private IElement NormalizeElement(IElement element)
    {
        var normalized = (IElement)element.Clone(false);

        var attributes = element.Attributes
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var attribute in attributes)
        {
            normalized.SetAttribute(attribute.Name, attribute.Value);
        }

        foreach (var child in element.ChildNodes)
        {
            var normalizedChild = NormalizeNode(child);
            normalized.AppendChild(normalizedChild);
        }

        return normalized;
    }

    private static string[] SplitIntoLines(string content)
    {
        return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }
}
