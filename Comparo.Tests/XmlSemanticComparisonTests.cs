using Comparo.Core.StructuredComparators;
using Comparo.Core.DiffModels;
using FluentAssertions;
using Xunit;

namespace Comparo.Tests;

public class XmlSemanticComparisonTests
{
    private readonly XmlSemanticComparator _comparator;

    public XmlSemanticComparisonTests()
    {
        _comparator = new XmlSemanticComparator();
    }

    [Fact]
    public void TagAwareness_TagsNeverBroken_ShouldPreserveTagStructure()
    {
        string left = @"<root><parent><child>content</child></parent></root>";

        string right = @"<root><parent><child>changed</child></parent></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("content change should be detected");
        changes.Should().OnlyContain(c => c.XPath.Contains("/") && !c.XPath.Contains("<"), "XPath should not contain broken tags");
    }

    [Fact]
    public void TagAwareness_NestedTagsPreserved_ShouldMaintainHierarchy()
    {
        string left = @"<root><level1><level2><level3>deep</level3></level2></level1></root>";

        string right = @"<root><level1><level2><level3>modified</level3></level2></level1></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.XPath.Contains("root/level1/level2/level3"), "should maintain full path hierarchy");
    }

    [Fact(Skip = "XML element reordering detection not fully implemented")]
    public void ElementOrderPreservation_DifferentOrder_ShouldDetectReordering()
    {
        string left = @"<root><a/><b/><c/></root>";

        string right = @"<root><b/><a/><c/></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("element order changes should be detected");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.ReorderElement, "reordering should be marked");
    }

    [Fact]
    public void ElementOrderPreservation_SameOrder_ShouldHaveNoChanges()
    {
        string left = @"<root><a/><b/><c/></root>";

        string right = @"<root><a/><b/><c/></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("same element order should have no changes");
    }

    [Fact]
    public void AttributeOrderIndependence_DifferentOrder_ShouldHaveNoChanges()
    {
        string left = @"<root a=""1"" b=""2"" c=""3""/>";

        string right = @"<root c=""3"" a=""1"" b=""2""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("attribute order should not matter");
    }

    [Fact]
    public void AttributeOrderIndependence_MultipleAttributes_ShouldIgnoreOrder()
    {
        string left = @"<element id=""123"" name=""test"" type=""text""/>";

        string right = @"<element type=""text"" id=""123"" name=""test""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("multiple attributes in different order should match");
    }

    [Fact]
    public void AttributeValueComparison_ValueChanged_ShouldDetectChange()
    {
        string left = @"<root port=""8080""/>";

        string right = @"<root port=""8081""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("attribute value change should be detected");
        changes.Should().Contain(c =>
            c.ChangeType == XPathChangeType.ModifyAttribute &&
            c.AttributeName == "port", "should detect port attribute change");
    }

    [Fact]
    public void AttributeValueComparison_MultipleChanged_ShouldDetectAll()
    {
        string left = @"<root a=""1"" b=""2"" c=""3""/>";

        string right = @"<root a=""1"" b=""20"" c=""30""/>";

        var changes = _comparator.Compare(left, right);

        changes.Where(c => c.ChangeType == XPathChangeType.ModifyAttribute)
            .Should().HaveCount(2, "should detect 2 attribute value changes");
    }

    [Fact]
    public void NamespaceAwareness_WithNamespaces_ShouldRespectNamespaces()
    {
        string left = @"<root xmlns:ns=""http://example.com""><ns:child>value</ns:child></root>";

        string right = @"<root xmlns:ns=""http://example.com""><ns:child>changed</ns:child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("namespace-aware comparison should detect changes");
    }

    [Fact]
    public void NamespaceAwareness_DifferentNamespaces_ShouldDetectDifference()
    {
        string left = @"<root xmlns:ns=""http://old.com""><ns:child>value</ns:child></root>";

        string right = @"<root xmlns:ns=""http://new.com""><ns:child>value</ns:child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("namespace URI changes should be detected");
    }

    [Fact]
    public void TextContentComparison_ContentChanged_ShouldDetectChange()
    {
        string left = @"<root>Hello World</root>";

        string right = @"<root>Hello Universe</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("text content changes should be detected");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.ModifyElement, "text change should be element modification");
    }

    [Fact]
    public void TextContentComparison_EmptyVsContent_ShouldDetectDifference()
    {
        string left = @"<root></root>";

        string right = @"<root>content</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("empty vs non-empty content should be different");
    }

    [Fact]
    public void MixedContentSupport_TextAndElements_ShouldPreserveStructure()
    {
        string left = @"<root>Text1<child/>Text2</root>";

        string right = @"<root>Text1<child/>Text3</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("changes in mixed content should be detected");
    }

    [Fact(Skip = "XML mixed content reordering detection not fully implemented")]
    public void MixedContentSupport_ReorderMixedContent_ShouldDetectChange()
    {
        string left = @"<root>A<child1/>B<child2/></root>";

        string right = @"<root>A<child2/>B<child1/></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("mixed content reordering should be detected");
    }

    [Fact(Skip = "XML XPath generation for attributes not fully implemented")]
    public void XPathChangeTracking_ShouldGenerateValidXPath()
    {
        string left = @"<config><server port=""8080""><timeout>30</timeout></server></config>";

        string right = @"<config><server port=""8081""><timeout>30</timeout></server></config>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.XPath.StartsWith("/"), "XPath should start with /");
        changes.Should().Contain(c => c.XPath.Contains("config/server/@port"), "should have correct attribute path");
    }

    [Fact]
    public void XPathChangeTracking_NestedElement_ShouldTrackFullPath()
    {
        string left = @"<root><a><b><c>value</c></b></a></root>";

        string right = @"<root><a><b><c>changed</c></b></a></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.XPath.Contains("root/a/b/c"), "should track full nested path");
    }

    [Fact]
    public void CommentHandling_IgnoreComments_ShouldNotReportCommentChanges()
    {
        string left = @"<root><!-- old comment --><child>value</child></root>";

        string right = @"<root><!-- new comment --><child>value</child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("comment changes should be ignored when configured");
    }

    [Fact]
    public void CommentHandling_CommentVsNoComment_ShouldNotReportChange()
    {
        string left = @"<root><child>value</child></root>";

        string right = @"<root><!-- comment --><child>value</child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("adding/removing comments should not affect comparison");
    }

    [Fact]
    public void CDataSectionSupport_ContentInCData_ShouldTreatAsOpaque()
    {
        string left = @"<root><![CDATA[some <data> here]]></root>";

        string right = @"<root><![CDATA[some <other> here]]></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("CDATA content changes should be detected");
    }

    [Fact]
    public void CDataSectionSupport_SameCData_ShouldHaveNoChanges()
    {
        string left = @"<root><![CDATA[unchanged content]]></root>";

        string right = @"<root><![CDATA[unchanged content]]></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("identical CDATA should have no changes");
    }

    [Fact]
    public void EmptyElements_EmptyVsMissing_ShouldDetectDifference()
    {
        string left = @"<root><empty/></root>";

        string right = @"<root></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("empty element vs missing element should be different");
    }

    [Fact]
    public void EmptyElements_EmptyVsContent_ShouldDetectDifference()
    {
        string left = @"<root><empty/></root>";

        string right = @"<root><empty>content</empty></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("empty element vs element with content should be different");
    }

    [Fact]
    public void SelfClosingTags_SelfClosingVsOpenClose_ShouldBeEquivalent()
    {
        string left = @"<root><tag/></root>";

        string right = @"<root><tag></tag></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("self-closing tag and open-close pair should be equivalent");
    }

    [Fact]
    public void SelfClosingTags_MultipleSelfClosing_ShouldBeEquivalent()
    {
        string left = @"<root><a/><b/><c/></root>";

        string right = @"<root><a></a><b></b><c></c></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("multiple self-closing tags should be equivalent to open-close pairs");
    }

    [Fact]
    public void NestedElements_MultipleLevels_ShouldDetectDeepChanges()
    {
        string left = @"<root><l1><l2><l3><l4>deep</l4></l3></l2></l1></root>";

        string right = @"<root><l1><l2><l3><l4>changed</l4></l3></l2></l1></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("changes deep in nesting should be detected");
        changes.Should().Contain(c => c.XPath.Contains("l4"), "should identify the deep element");
    }

    [Fact]
    public void NestedElements_TenLevelsDeep_ShouldDetectChange()
    {
        string left = @"<root><l1><l2><l3><l4><l5><l6><l7><l8><l9><l10>value</l10></l9></l8></l7></l6></l5></l4></l3></l2></l1></root>";

        string right = @"<root><l1><l2><l3><l4><l5><l6><l7><l8><l9><l10>modified</l10></l9></l8></l7></l6></l5></l4></l3></l2></l1></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("changes at 10 levels deep should be detected");
    }

    [Fact]
    public void AttributeChanges_AttributeAdded_ShouldDetectAdd()
    {
        string left = @"<root name=""test""/>";

        string right = @"<root name=""test"" id=""123""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("added attribute should be detected");
        changes.Should().Contain(c =>
            c.ChangeType == XPathChangeType.AddAttribute &&
            c.AttributeName == "id", "should detect id attribute added");
    }

    [Fact]
    public void AttributeChanges_AttributeRemoved_ShouldDetectRemove()
    {
        string left = @"<root name=""test"" id=""123""/>";

        string right = @"<root name=""test""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("removed attribute should be detected");
        changes.Should().Contain(c =>
            c.ChangeType == XPathChangeType.RemoveAttribute &&
            c.AttributeName == "id", "should detect id attribute removed");
    }

    [Fact]
    public void AttributeChanges_MultipleAttributeChanges_ShouldDetectAll()
    {
        string left = @"<root a=""1"" b=""2"" c=""3"" d=""4""/>";

        string right = @"<root a=""1"" b=""20"" e=""5"" d=""4""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().HaveCountGreaterThan(0, "should detect attribute changes");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.ModifyAttribute, "should detect modified attribute");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.RemoveAttribute, "should detect removed attribute");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.AddAttribute, "should detect added attribute");
    }

    [Fact]
    public void NamespacePrefixChanges_PrefixChangeOnly_ShouldNotReportChange()
    {
        string left = @"<root xmlns:ns=""http://example.com""><ns:child>value</ns:child></root>";

        string right = @"<root xmlns:alt=""http://example.com""><alt:child>value</alt:child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("prefix change with same namespace URI should not matter");
    }

    [Fact]
    public void NamespacePrefixChanges_NamespaceUriChange_ShouldDetectDifference()
    {
        string left = @"<root xmlns:ns=""http://old.com""><ns:child>value</ns:child></root>";

        string right = @"<root xmlns:ns=""http://new.com""><ns:child>value</ns:child></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("namespace URI changes should be detected");
    }

    [Fact]
    public void ElementVsAttribute_ChildElementVsAttribute_ShouldBeDifferent()
    {
        string left = @"<root><child>value</child></root>";

        string right = @"<root child=""value""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("child element vs attribute should be different");
    }

    [Fact]
    public void ElementVsAttribute_ElementAddedVsAttributeAdded_ShouldDetectCorrectly()
    {
        string left = @"<root><value>text</value></root>";

        string right = @"<root><value>text</value><extra>more</extra></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("element addition should be detected");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.AddElement, "should detect element added");
    }

    [Fact]
    public void MultipleRootElements_DifferentRoots_ShouldCompareDocuments()
    {
        string left = @"<root1><a/></root1>";

        string right = @"<root2><b/></root2>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("different root elements should be detected");
    }

    [Fact]
    public void MultipleRootElements_SameRootDifferentContent_ShouldDetectChanges()
    {
        string left = @"<root><a/><b/></root>";

        string right = @"<root><a/><c/></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("different children of same root should be detected");
    }

    [Fact]
    public void WhitespaceInContent_WhitespaceChanges_ShouldDetectDifference()
    {
        string left = @"<root>text content</root>";

        string right = @"<root>text  content</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("whitespace changes in text content should be detected");
    }

    [Fact]
    public void WhitespaceInContent_NewlinesInContent_ShouldDetectDifference()
    {
        string left = @"<root>line1line2</root>";

        string right = @"<root>line1
line2</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("newline changes in content should be detected");
    }

    [Fact]
    public void SpecialCharacters_EntityEncoded_ShouldDecodeCorrectly()
    {
        string left = @"<root>&amp;&lt;&gt;</root>";

        string right = @"<root>&amp;&lt;&gt;</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("same encoded entities should be equal");
    }

    [Fact]
    public void SpecialCharacters_EntityChanges_ShouldDetectChanges()
    {
        string left = @"<root>&amp;</root>";

        string right = @"<root>&lt;</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("different entities should be detected");
    }

    [Fact]
    public void SpecialCharacters_AmpersandInText_ShouldHandleCorrectly()
    {
        string left = @"<root>AT&amp;T</root>";

        string right = @"<root>AT&amp;T</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("same text with ampersand should match");
    }

    [Fact]
    public void LargeDocuments_ManyElements_ShouldHandleEfficiently()
    {
        var leftElements = new System.Text.StringBuilder();
        var rightElements = new System.Text.StringBuilder();

        leftElements.Append("<root>");
        rightElements.Append("<root>");

        for (int i = 0; i < 100; i++)
        {
            leftElements.Append($"<item id=\"{i}\">value{i}</item>");
            rightElements.Append($"<item id=\"{i}\">value{i}</item>");
        }

        rightElements.Append("<item id=\"100\">extra</item>");

        leftElements.Append("</root>");
        rightElements.Append("</root>");

        var changes = _comparator.Compare(leftElements.ToString(), rightElements.ToString());

        changes.Should().NotBeEmpty("should detect change in large document");
        changes.Should().HaveCount(1, "should only detect the one extra element");
    }

    [Fact]
    public void LargeDocuments_DeeplyNestedManyElements_ShouldTrackChanges()
    {
        var leftXml = "<root>" + new string(' ', 1000) + "<deep>value</deep></root>";
        var rightXml = "<root>" + new string(' ', 1000) + "<deep>changed</deep></root>";

        var changes = _comparator.Compare(leftXml, rightXml);

        changes.Should().NotBeEmpty("should detect changes in documents with deep structure");
    }

    [Fact]
    public void ComplexRealWorldExample_MultipleChangeTypes_ShouldDetectAllChanges()
    {
        string left = @"
<config>
    <server host=""localhost"" port=""8080"">
        <timeout>30</timeout>
    </server>
    <logging level=""info""/>
</config>";

        string right = @"
<config>
    <server host=""localhost"" port=""8081"">
        <timeout>30</timeout>
        <debug>true</debug>
    </server>
    <logging level=""debug""/>
    <cache enabled=""true""/>
</config>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("multiple changes should be detected");
        changes.Should().HaveCountGreaterOrEqualTo(4, "should detect 4+ changes");
        changes.Should().Contain(c => c.AttributeName == "port", "should detect port attribute change");
        changes.Should().Contain(c => c.ChangeType == XPathChangeType.AddElement, "should detect debug element added");
    }

    [Fact]
    public void IdenticalXml_DifferentFormatting_ShouldHaveNoChanges()
    {
        string left = @"<root><child a=""1"" b=""2"">text</child></root>";

        string right = @"
<root>
    <child
        b=""2""
        a=""1""
    >
        text
    </child>
</root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("identical XML with different formatting should have no changes");
    }

    [Fact(Skip = "XML namespace attribute tracking not fully implemented")]
    public void Attributes_WithNamespace_ShouldTrackCorrectly()
    {
        string left = @"<root xmlns:ns=""http://example.com"" ns:attr=""value1""/>";

        string right = @"<root xmlns:ns=""http://example.com"" ns:attr=""value2""/>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("namespaced attribute changes should be detected");
        changes.Should().Contain(c => c.AttributeName == "ns:attr", "should track namespaced attribute name");
    }

    [Fact]
    public void Elements_SameTagDifferentAttributes_ShouldBeDifferent()
    {
        string left = @"<root><item id=""1""/></root>";

        string right = @"<root><item id=""2""/></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("same tag with different attributes should be detected");
    }

    [Fact]
    public void Elements_SameContentDifferentParents_ShouldTrackLocation()
    {
        string left = @"<root><parent1><child>value</child></parent1></root>";

        string right = @"<root><parent2><child>value</child></parent2></root>";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("same content in different parents should be detected");
    }
}
