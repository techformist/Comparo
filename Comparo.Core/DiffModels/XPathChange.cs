namespace Comparo.Core.DiffModels;

public enum XPathChangeType
{
    AddElement,
    RemoveElement,
    ModifyElement,
    ModifyAttribute,
    AddAttribute,
    RemoveAttribute,
    ReorderElement
}

public class XPathChange
{
    public XPathChangeType ChangeType { get; set; }
    public string XPath { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? AttributeName { get; set; }
    public int? OldPosition { get; set; }
    public int? NewPosition { get; set; }

    public XPathChange(XPathChangeType changeType, string xPath, string? oldValue = null, string? newValue = null)
    {
        ChangeType = changeType;
        XPath = xPath;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public bool IsElementChange => ChangeType switch
    {
        XPathChangeType.AddElement => true,
        XPathChangeType.RemoveElement => true,
        XPathChangeType.ModifyElement => true,
        XPathChangeType.ReorderElement => true,
        _ => false
    };

    public bool IsAttributeChange => ChangeType switch
    {
        XPathChangeType.AddAttribute => true,
        XPathChangeType.RemoveAttribute => true,
        XPathChangeType.ModifyAttribute => true,
        _ => false
    };
}
