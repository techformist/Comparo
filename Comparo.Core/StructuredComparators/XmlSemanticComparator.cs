using System.Xml.Linq;
using Comparo.Core.DiffModels;

namespace Comparo.Core.StructuredComparators;

public class XmlSemanticComparator : IStructuredComparator
{
    private readonly bool _ignoreAttributeOrder;
    private readonly bool _ignoreComments;

    public XmlSemanticComparator(bool ignoreAttributeOrder = true, bool ignoreComments = false)
    {
        _ignoreAttributeOrder = ignoreAttributeOrder;
        _ignoreComments = ignoreComments;
    }

    public string GetFileExtension() => ".xml";

    public XPathChange[] Compare(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            throw new ArgumentException("Left XML input cannot be null or empty", nameof(left));
        if (string.IsNullOrWhiteSpace(right))
            throw new ArgumentException("Right XML input cannot be null or empty", nameof(right));

        try
        {
            var changes = new List<XPathChange>();
            var leftDoc = XDocument.Parse(left, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var rightDoc = XDocument.Parse(right, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            if (leftDoc.Root == null || rightDoc.Root == null)
                throw new InvalidOperationException("XML document must have a root element");

            CompareElements(leftDoc.Root, rightDoc.Root, leftDoc.Root.Name.LocalName, changes);
            return changes.ToArray();
        }
        catch (System.Xml.XmlException ex)
        {
            throw new InvalidOperationException("Failed to parse XML: " + ex.Message, ex);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to compare XML", ex);
        }
    }

    private void CompareElements(XElement left, XElement right, string path, List<XPathChange> changes)
    {
        if (left.Name != right.Name)
        {
            changes.Add(new XPathChange(XPathChangeType.ModifyElement, path, left.Name.LocalName, right.Name.LocalName));
            return;
        }

        var leftAttrs = left.Attributes().Where(a => a.IsNamespaceDeclaration == false)
                                         .ToDictionary(a => a.Name.ToString(), a => a.Value);
        var rightAttrs = right.Attributes().Where(a => a.IsNamespaceDeclaration == false)
                                           .ToDictionary(a => a.Name.ToString(), a => a.Value);

        // Process attributes in deterministic (sorted) order
        var attrNames = new HashSet<string>(leftAttrs.Keys);
        attrNames.UnionWith(rightAttrs.Keys);
        foreach (var name in attrNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            leftAttrs.TryGetValue(name, out var lval);
            rightAttrs.TryGetValue(name, out var rval);
            if (lval == null && rval != null)
                changes.Add(new XPathChange(XPathChangeType.AddAttribute, path) { AttributeName = name, NewValue = rval });
            else if (lval != null && rval == null)
                changes.Add(new XPathChange(XPathChangeType.RemoveAttribute, path) { AttributeName = name, OldValue = lval });
            else if (lval != null && rval != null && lval != rval)
                changes.Add(new XPathChange(XPathChangeType.ModifyAttribute, path) { AttributeName = name, OldValue = lval, NewValue = rval });
        }

        var ltext = string.Concat(left.Nodes().OfType<XText>().Select(t => t.Value)).Trim();
        var rtext = string.Concat(right.Nodes().OfType<XText>().Select(t => t.Value)).Trim();
        if (!string.Equals(ltext, rtext, StringComparison.Ordinal))
        {
            changes.Add(new XPathChange(XPathChangeType.ModifyElement, path, ltext, rtext));
        }

        var lChildren = left.Elements().ToList();
        var rChildren = right.Elements().ToList();

        if (lChildren.Count == 0 && rChildren.Count == 0) return;

        var lNames = lChildren.Select(c => c.Name.LocalName).ToList();
        var rNames = rChildren.Select(c => c.Name.LocalName).ToList();

        // Check if elements are reordered (same set, different order)
        bool isReordered = lNames.Count == rNames.Count &&
                          lNames.OrderBy(x => x).SequenceEqual(rNames.OrderBy(x => x)) &&
                          !lNames.SequenceEqual(rNames);

        if (isReordered)
        {
            // When reordered, match elements by name rather than position
            // This avoids false positives from positional comparison
            var matchedRight = new HashSet<int>();

            for (int i = 0; i < lChildren.Count; i++)
            {
                var lChild = lChildren[i];
                var childPath = $"{path}/{lChild.Name.LocalName}";

                // Find corresponding element in right by name
                int rIndex = -1;
                for (int j = 0; j < rChildren.Count; j++)
                {
                    if (!matchedRight.Contains(j) && rChildren[j].Name == lChild.Name)
                    {
                        rIndex = j;
                        break;
                    }
                }

                if (rIndex >= 0)
                {
                    matchedRight.Add(rIndex);
                    CompareElements(lChild, rChildren[rIndex], childPath, changes);
                }
                else
                {
                    // Element exists in left but not in right (shouldn't happen if truly reordered)
                    changes.Add(new XPathChange(XPathChangeType.RemoveElement, childPath, lChild.Value, null));
                }
            }
        }
        else
        {
            // Not reordered, compare positionally
            var max = Math.Max(lChildren.Count, rChildren.Count);
            for (int i = 0; i < max; i++)
            {
                if (i >= lChildren.Count)
                {
                    var childPath = $"{path}/{rChildren[i].Name.LocalName}";
                    changes.Add(new XPathChange(XPathChangeType.AddElement, childPath, null, rChildren[i].Value));
                }
                else if (i >= rChildren.Count)
                {
                    var childPath = $"{path}/{lChildren[i].Name.LocalName}";
                    changes.Add(new XPathChange(XPathChangeType.RemoveElement, childPath, lChildren[i].Value, null));
                }
                else
                {
                    var childPath = $"{path}/{lChildren[i].Name.LocalName}";
                    CompareElements(lChildren[i], rChildren[i], childPath, changes);
                }
            }
        }
    }
}
