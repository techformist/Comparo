using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Comparo.Core.Normalizers;

public class XmlNormalizer
{
    private readonly bool _sortAttributes;
    private readonly bool _handleNamespaces;
    private readonly bool _preserveComments;
    private readonly bool _preserveWhitespace;

    public XmlNormalizer(bool sortAttributes = true, bool handleNamespaces = true, bool preserveComments = false, bool preserveWhitespace = false)
    {
        _sortAttributes = sortAttributes;
        _handleNamespaces = handleNamespaces;
        _preserveComments = preserveComments;
        _preserveWhitespace = preserveWhitespace;
    }

    public async Task<string> NormalizeAsync(string xmlContent, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Normalize(xmlContent), cancellationToken);
    }

    public string Normalize(string xmlContent)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var parser = context.GetService<IHtmlParser>();
        if (parser == null)
        {
            throw new InvalidOperationException("Unable to create HTML parser");
        }
        var document = parser.ParseDocument(xmlContent);

        return NormalizeDocument(document);
    }

    public async Task<string> NormalizeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await NormalizeAsync(content, cancellationToken);
    }

    private string NormalizeDocument(IDocument document)
    {
        var normalizedElement = NormalizeNode(document.DocumentElement);
        return normalizedElement?.ToHtml() ?? string.Empty;
    }

    private INode? NormalizeNode(INode node)
    {
        return node.NodeType switch
        {
            NodeType.Element => NormalizeElement((IElement)node),
            NodeType.Text => NormalizeText(node),
            NodeType.Comment => _preserveComments ? NormalizeComment(node) : null,
            NodeType.Document => node.Clone(true),
            _ => node.Clone(true)
        };
    }

    private IElement NormalizeElement(IElement element)
    {
        var normalized = (IElement)element.Clone(false);

        var attributes = GetSortedAttributes(element);

        foreach (var attribute in attributes)
        {
            var normalizedValue = NormalizeAttributeValue(attribute.Value);
            normalized.SetAttribute(attribute.Name, normalizedValue);
        }

        var namespaceUri = element.NamespaceUri;
        if (_handleNamespaces && !string.IsNullOrEmpty(namespaceUri))
        {
            normalized.SetAttribute("xmlns", namespaceUri);
        }

        foreach (var child in element.ChildNodes)
        {
            var normalizedChild = NormalizeNode(child);
            if (normalizedChild != null)
            {
                normalized.AppendChild(normalizedChild);
            }
        }

        return normalized;
    }

    private IEnumerable<IAttr> GetSortedAttributes(IElement element)
    {
        var attributes = element.Attributes;

        if (_sortAttributes)
        {
            return attributes
                .Where(a => !_handleNamespaces || !a.Name.StartsWith("xmlns", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        return attributes;
    }

    private INode NormalizeText(INode textNode)
    {
        if (_preserveWhitespace)
        {
            return textNode.Clone(true);
        }

        var text = textNode.TextContent?.Trim() ?? string.Empty;
        return textNode.Owner?.CreateTextNode(text) ?? textNode.Clone(true);
    }

    private INode NormalizeComment(INode commentNode)
    {
        return commentNode.Clone(true);
    }

    private string NormalizeAttributeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (_preserveWhitespace)
        {
            return value;
        }

        return value.Trim();
    }

    public static async Task<string> CanonicalizeAsync(string xmlContent, CancellationToken cancellationToken = default)
    {
        var normalizer = new XmlNormalizer(
            sortAttributes: true,
            handleNamespaces: true,
            preserveComments: false,
            preserveWhitespace: false);

        return await normalizer.NormalizeAsync(xmlContent, cancellationToken);
    }
}
