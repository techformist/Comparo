using Comparo.Core.StructuredComparators;
using FluentAssertions;
using Xunit;

namespace Comparo.Tests;

public class JsonSemanticComparisonTests
{
    private readonly JsonSemanticComparator _comparator;

    public JsonSemanticComparisonTests()
    {
        _comparator = new JsonSemanticComparator();
    }

    [Fact]
    public void PropertyOrderIndependence_SameContentDifferentOrder_ShouldHaveNoChanges()
    {
        string left = @"{
            ""name"": ""John"",
            ""age"": 30
        }";

        string right = @"{
            ""age"": 30,
            ""name"": ""John""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("property order should not affect comparison");
    }

    [Fact]
    public void PropertyOrderIndependence_MultiplePropertiesDifferentOrder_ShouldHaveNoChanges()
    {
        string left = @"{
            ""firstName"": ""John"",
            ""lastName"": ""Doe"",
            ""age"": 30,
            ""active"": true
        }";

        string right = @"{
            ""active"": true,
            ""age"": 30,
            ""firstName"": ""John"",
            ""lastName"": ""Doe""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("multiple properties in different order should still match");
    }

    [Fact]
    public void ArrayOrderPreservation_SameElementsDifferentOrder_ShouldDetectReordering()
    {
        string left = @"{
            ""items"": [""a"", ""b"", ""c""]
        }";

        string right = @"{
            ""items"": [""b"", ""a"", ""c""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("array order changes should be detected");
        changes.Should().Contain(c => c.IsMove, "array reordering should be marked as Move operation");
    }

    [Fact]
    public void ArrayOrderPreservation_ArraysAreNotEqual_WhenElementsAreReordered()
    {
        string left = @"[""a"", ""b"", ""c""]";

        string right = @"[""b"", ""a"", ""c""]";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("reordered arrays should not be considered equal");
    }

    [Fact]
    public void WhitespaceIndependence_DifferentFormatting_ShouldHaveNoChanges()
    {
        string left = @"{ ""name"": ""John"", ""age"": 30 }";

        string right = @"{
  ""name"": ""John"",
  ""age"": 30
}";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("whitespace formatting should not affect comparison");
    }

    [Fact]
    public void WhitespaceIndependence_VariousIndentation_ShouldHaveNoChanges()
    {
        string left = @"{""name"":""John"",""age"":30}";

        string right = @"{
    ""name"": ""John"",
    ""age"": 30
}";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("different indentation levels should not affect comparison");
    }

    [Fact]
    public void TypeChangeDetection_StringToNumber_ShouldDetectChange()
    {
        string left = @"{
            ""value"": ""123""
        }";

        string right = @"{
            ""value"": 123
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("type changes should be detected");
        changes.Should().Contain(c => c.IsReplace, "value type change should be marked as Replace");
    }

    [Fact]
    public void TypeChangeDetection_NumberToString_ShouldDetectChange()
    {
        string left = @"{
            ""count"": 42
        }";

        string right = @"{
            ""count"": ""42""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("number to string type change should be detected");
        changes.Should().Contain(c => c.Path == "count", "change should be at 'count' path");
    }

    [Fact]
    public void DeepNestedComparison_MultipleLevels_ShouldDetectChange()
    {
        string left = @"{
            ""level1"": {
                ""level2"": {
                    ""level3"": {
                        ""value"": ""old""
                    }
                }
            }
        }";

        string right = @"{
            ""level1"": {
                ""level2"": {
                    ""level3"": {
                        ""value"": ""new""
                    }
                }
            }
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("deep nested changes should be detected");
        changes.Should().Contain(c => c.Path.Contains("level1/level2/level3/value"), "should track path to nested value");
    }

    [Fact]
    public void DeepNestedComparison_TenLevelsDeep_ShouldDetectChange()
    {
        string left = @"{
            ""l1"": {""l2"": {""l3"": {""l4"": {""l5"": {""l6"": {""l7"": {""l8"": {""l9"": {""l10"": ""deep""}}}}}}}}}
        }";

        string right = @"{
            ""l1"": {""l2"": {""l3"": {""l4"": {""l5"": {""l6"": {""l7"": {""l8"": {""l9"": {""l10"": ""changed""}}}}}}}}}
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("changes at 10 levels deep should be detected");
    }

    [Fact]
    public void MissingVsNullDetection_EmptyObjectVsObjectWithNull_ShouldDetectDifference()
    {
        string left = @"{}";

        string right = @"{
            ""value"": null
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("missing property vs null property should be different");
    }

    [Fact]
    public void MissingVsNullDetection_NullPropertyVsMissingProperty_ShouldDetectDifference()
    {
        string left = @"{
            ""value"": null
        }";

        string right = @"{}";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("null property vs missing property should be different");
        changes.Should().Contain(c => c.IsRemove, "should detect property removal");
    }

    [Fact]
    public void ArrayElementChanges_AddElement_ShouldDetectAdd()
    {
        string left = @"{
            ""items"": [""a"", ""b""]
        }";

        string right = @"{
            ""items"": [""a"", ""b"", ""c""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("array element addition should be detected");
        changes.Should().Contain(c => c.IsAdd, "added element should be marked as Add");
    }

    [Fact]
    public void ArrayElementChanges_RemoveElement_ShouldDetectRemove()
    {
        string left = @"{
            ""items"": [""a"", ""b"", ""c""]
        }";

        string right = @"{
            ""items"": [""a"", ""c""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("array element removal should be detected");
        changes.Should().Contain(c => c.IsRemove, "removed element should be marked as Remove");
    }

    [Fact]
    public void ArrayElementChanges_ReplaceElement_ShouldDetectReplace()
    {
        string left = @"{
            ""items"": [""a"", ""b"", ""c""]
        }";

        string right = @"{
            ""items"": [""a"", ""x"", ""c""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("array element replacement should be detected");
        changes.Should().Contain(c => c.IsReplace, "replaced element should be marked as Replace");
    }

    [Fact]
    public void ArrayReorderingDetection_CompleteReorder_ShouldDetectMoves()
    {
        string left = @"{
            ""items"": [""apple"", ""banana"", ""cherry"", ""date""]
        }";

        string right = @"{
            ""items"": [""date"", ""cherry"", ""banana"", ""apple""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("complete array reordering should be detected");
        changes.Where(c => c.IsMove).Should().HaveCountGreaterThan(0, "multiple moves should be detected");
    }

    [Fact]
    public void ArrayReorderingDetection_PartialReorder_ShouldDetectMoves()
    {
        string left = @"{
            ""items"": [""a"", ""b"", ""c"", ""d""]
        }";

        string right = @"{
            ""items"": [""b"", ""a"", ""c"", ""d""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("partial array reordering should be detected");
    }

    [Fact]
    public void NestedObjectsComparison_DifferentNestedValues_ShouldDetectChanges()
    {
        string left = @"{
            ""user"": {
                ""name"": ""John"",
                ""address"": {
                    ""city"": ""NYC"",
                    ""zip"": ""10001""
                }
            }
        }";

        string right = @"{
            ""user"": {
                ""name"": ""John"",
                ""address"": {
                    ""city"": ""SF"",
                    ""zip"": ""10001""
                }
            }
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("nested object value changes should be detected");
        changes.Should().Contain(c => c.Path.Contains("address/city"), "should identify nested property change");
    }

    [Fact]
    public void NestedObjectsComparison_NestedObjectAdded_ShouldDetectAdd()
    {
        string left = @"{
            ""user"": {
                ""name"": ""John""
            }
        }";

        string right = @"{
            ""user"": {
                ""name"": ""John"",
                ""address"": {
                    ""city"": ""NYC""
                }
            }
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("nested object addition should be detected");
    }

    [Fact]
    public void EmptyArraysVsMissingArrays_EmptyArrayVsNoProperty_ShouldDetectDifference()
    {
        string left = @"{
            ""items"": []
        }";

        string right = @"{}";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("empty array vs missing property should be different");
        changes.Should().Contain(c => c.IsRemove, "should detect property removal");
    }

    [Fact]
    public void EmptyArraysVsMissingArrays_NoPropertyVsEmptyArray_ShouldDetectDifference()
    {
        string left = @"{}";

        string right = @"{
            ""items"": []
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("missing property vs empty array should be different");
        changes.Should().Contain(c => c.IsAdd, "should detect property addition");
    }

    [Fact]
    public void BooleanComparison_TrueToFalse_ShouldDetectChange()
    {
        string left = @"{
            ""active"": true
        }";

        string right = @"{
            ""active"": false
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("boolean value changes should be detected");
        changes.Should().Contain(c => c.IsReplace, "boolean change should be marked as Replace");
    }

    [Fact]
    public void BooleanComparison_MultipleBooleans_ShouldDetectEachChange()
    {
        string left = @"{
            ""flag1"": true,
            ""flag2"": false,
            ""flag3"": true
        }";

        string right = @"{
            ""flag1"": false,
            ""flag2"": false,
            ""flag3"": false
        }";

        var changes = _comparator.Compare(left, right);

        changes.Where(c => c.IsReplace).Should().HaveCount(2, "two boolean changes should be detected");
    }

    [Fact]
    public void NumberComparison_IntegerChanges_ShouldDetectChange()
    {
        string left = @"{
            ""value"": 42
        }";

        string right = @"{
            ""value"": 100
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("integer value changes should be detected");
        changes.Should().Contain(c => c.IsReplace, "integer change should be marked as Replace");
    }

    [Fact]
    public void NumberComparison_FloatChanges_ShouldDetectChange()
    {
        string left = @"{
            ""price"": 19.99
        }";

        string right = @"{
            ""price"": 29.99
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("float value changes should be detected");
    }

    [Fact]
    public void NumberComparison_NegativeNumbers_ShouldDetectChange()
    {
        string left = @"{
            ""value"": -10
        }";

        string right = @"{
            ""value"": 10
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("negative to positive number change should be detected");
    }

    [Fact]
    public void NumberComparison_IntegerToFloat_ShouldDetectChange()
    {
        string left = @"{
            ""value"": 10
        }";

        string right = @"{
            ""value"": 10.5
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("integer to float change should be detected");
    }

    [Fact]
    public void StringComparison_NonEmptyString_ShouldDetectChange()
    {
        string left = @"{
            ""name"": ""John""
        }";

        string right = @"{
            ""name"": ""Jane""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("string value changes should be detected");
        changes.Should().Contain(c => c.IsReplace, "string change should be marked as Replace");
    }

    [Fact]
    public void StringComparison_EmptyString_ShouldDetectChange()
    {
        string left = @"{
            ""name"": ""John""
        }";

        string right = @"{
            ""name"": """"
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("change to empty string should be detected");
    }

    [Fact]
    public void StringComparison_FromEmptyString_ShouldDetectChange()
    {
        string left = @"{
            ""name"": """"
        }";

        string right = @"{
            ""name"": ""John""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("change from empty string should be detected");
    }

    [Fact]
    public void StringComparison_SpecialCharactersInString_ShouldDetectChange()
    {
        string left = @"{
            ""text"": ""Hello\nWorld!""
        }";

        string right = @"{
            ""text"": ""Hello\tWorld!""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("special character changes should be detected");
    }

    [Fact]
    public void MixedTypesInArrays_DifferentTypes_ShouldDetectChanges()
    {
        string left = @"{
            ""values"": [1, ""two"", true, null]
        }";

        string right = @"{
            ""values"": [2, ""two"", false, null]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("changes in mixed-type array should be detected");
    }

    [Fact]
    public void MixedTypesInArrays_ReorderMixedTypes_ShouldDetectMoves()
    {
        string left = @"{
            ""values"": [1, ""text"", true, null]
        }";

        string right = @"{
            ""values"": [true, 1, ""text"", null]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("reordering mixed-type array should be detected");
    }

    [Fact]
    public void MixedTypesInArrays_TypeChangeInArray_ShouldDetectChange()
    {
        string left = @"{
            ""values"": [1, 2, 3]
        }";

        string right = @"{
            ""values"": [""1"", ""2"", ""3""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("type changes in array elements should be detected");
    }

    [Fact]
    public void UnicodeHandling_UnicodeCharacters_ShouldDetectChanges()
    {
        string left = @"{
            ""text"": ""Hello 世界""
        }";

        string right = @"{
            ""text"": ""Hello 世界🌍""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("unicode character changes should be detected");
    }

    [Fact]
    public void UnicodeHandling_Emoji_ShouldDetectChanges()
    {
        string left = @"{
            ""emoji"": ""😀""
        }";

        string right = @"{
            ""emoji"": ""😎""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("emoji changes should be detected");
    }

    [Fact]
    public void UnicodeHandling_AccentedCharacters_ShouldDetectChanges()
    {
        string left = @"{
            ""name"": ""François""
        }";

        string right = @"{
            ""name"": ""Francois""
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("accented character changes should be detected");
    }

    [Fact]
    public void UnicodeHandling_UnicodeInArrays_ShouldDetectChanges()
    {
        string left = @"{
            ""items"": [""α"", ""β"", ""γ""]
        }";

        string right = @"{
            ""items"": [""γ"", ""β"", ""α""]
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("unicode array reordering should be detected");
    }

    [Fact]
    public void IdenticalJson_EvenWithDifferentFormatting_ShouldHaveNoChanges()
    {
        string left = @"{
            ""name"": ""John"",
            ""age"": 30,
            ""active"": true
        }";

        string right = @"{
    ""name""   :   ""John""   ,
    ""age""    :   30,
    ""active"" :   true
}";

        var changes = _comparator.Compare(left, right);

        changes.Should().BeEmpty("identical JSON should have no changes regardless of formatting");
    }

    [Fact]
    public void ComplexRealWorldExample_MultipleChangeTypes_ShouldDetectAllChanges()
    {
        string left = @"{
            ""firstName"": ""John"",
            ""lastName"": ""Doe"",
            ""age"": 30,
            ""hobbies"": [""reading"", ""coding"", ""gaming""],
            ""address"": {
                ""street"": ""123 Main St"",
                ""city"": ""NYC"",
                ""zip"": ""10001""
            }
        }";

        string right = @"{
            ""lastName"": ""Doe"",
            ""firstName"": ""Johnathan"",
            ""age"": 31,
            ""hobbies"": [""coding"", ""reading"", ""hiking""],
            ""address"": {
                ""street"": ""123 Main St"",
                ""city"": ""San Francisco"",
                ""zip"": ""94105"",
                ""country"": ""USA""
            }
        }";

        var changes = _comparator.Compare(left, right);

        changes.Should().NotBeEmpty("multiple changes should be detected");
        changes.Should().HaveCountGreaterOrEqualTo(6, "should detect 6+ semantic changes");
    }
}
