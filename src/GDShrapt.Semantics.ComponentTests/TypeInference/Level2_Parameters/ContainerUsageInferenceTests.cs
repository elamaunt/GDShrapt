using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Integration tests for container type inference with union types.
/// Tests that untyped arrays infer union types from usage patterns.
/// </summary>
[TestClass]
public class ContainerUsageInferenceTests
{
    #region Union Type Tests - Usage-Based Inference

    [TestMethod]
    public void UntypedArray_MixedAppends_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var arr = []
    arr.append(1)
    arr.append(""hello"")
    arr.append(2.5)
";
        var model = CreateSemanticModel(code);
        var arrType = GetVariableType(model, "arr");

        // Should be Array[String|float|int] (sorted)
        arrType.Should().Be("Array[String|float|int]");
    }

    [TestMethod]
    public void UntypedArray_SingleTypeAppends_ReturnsSingleType()
    {
        var code = @"
extends Node
func test():
    var arr = []
    arr.append(1)
    arr.append(2)
    arr.append(3)
";
        var model = CreateSemanticModel(code);
        var arrType = GetVariableType(model, "arr");

        arrType.Should().Be("Array[int]");
    }

    [TestMethod]
    public void UntypedArray_MixedInitializer_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var arr = [1, ""hello"", true]
";
        var model = CreateSemanticModel(code);
        var arrType = GetVariableType(model, "arr");

        // Should be Array with union of String, bool, int (order may vary)
        AssertUnionTypeEquals(arrType, "Array[String|bool|int]");
    }

    [TestMethod]
    public void UntypedArray_IndexAssign_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var arr = []
    arr[0] = 1
    arr[1] = ""hello""
";
        var model = CreateSemanticModel(code);
        var arrType = GetVariableType(model, "arr");

        arrType.Should().Be("Array[String|int]");
    }

    [TestMethod]
    public void UntypedArrayAddition_BothInferred_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var a = [1, 2]
    var b = [""x"", ""y""]
    var c = a + b
";
        var model = CreateSemanticModel(code);
        var cType = GetVariableType(model, "c");

        cType.Should().Be("Array[String|int]");
    }

    [TestMethod]
    public void UntypedArrayAddition_BothWithUsageInference_ReturnsUnionType()
    {
        // Both arrays are untyped but have types inferred from usage (append calls)
        var code = @"
extends Node
func test():
    var a = []
    a.append(1)
    a.append(2)

    var b = []
    b.append(""hello"")
    b.append(""world"")

    var c = a + b
";
        var model = CreateSemanticModel(code);
        var aType = GetVariableType(model, "a");
        var bType = GetVariableType(model, "b");
        var cType = GetVariableType(model, "c");

        aType.Should().Be("Array[int]");
        bType.Should().Be("Array[String]");
        cType.Should().Be("Array[String|int]");
    }

    [TestMethod]
    public void UntypedArrayAddition_MixedUsageTypes_ReturnsUnionType()
    {
        // Both arrays have mixed types from usage
        var code = @"
extends Node
func test():
    var a = []
    a.append(1)
    a.append(true)

    var b = []
    b.append(""hello"")
    b.append(2.5)

    var c = a + b
";
        var model = CreateSemanticModel(code);
        var cType = GetVariableType(model, "c");

        // a is Array[bool|int], b is Array[String|float]
        // c should be Array[String|bool|float|int]
        cType.Should().Be("Array[String|bool|float|int]");
    }

    [TestMethod]
    public void UntypedArrayAddition_ChainedWithUsage_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var a = []
    a.append(1)

    var b = []
    b.append(""x"")

    var c = []
    c.append(true)

    var result = a + b + c
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");

        resultType.Should().Be("Array[String|bool|int]");
    }

    #endregion

    #region Dictionary Literal Inference

    [TestMethod]
    public void UntypedDictionary_StringIntLiteral_ReturnsDictionaryType()
    {
        var code = @"
extends Node
func test():
    var dict = {""a"": 1, ""b"": 2}
";
        var model = CreateSemanticModel(code);
        var dictType = GetVariableType(model, "dict");

        // Symbol-level inference returns comma without space (via BuildName)
        AssertDictionaryType(dictType, "String", "int");
    }

    [TestMethod]
    public void UntypedDictionary_MixedValueTypes_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var dict = {""a"": 1, ""b"": ""hello""}
";
        var model = CreateSemanticModel(code);

        // Mixed value types produce a union which can't be represented as GDTypeNode.
        // The symbol-level inference handles this via the variable declaration path.
        var varDecl = model.ScriptFile.Class?.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "dict");

        var dictType = model.GetTypeForNode(varDecl);
        AssertDictionaryType(dictType, "String", "String", "int");
    }

    [TestMethod]
    public void UntypedDictionary_EmptyLiteral_ReturnsDictionary()
    {
        var code = @"
extends Node
func test():
    var dict = {}
";
        var model = CreateSemanticModel(code);
        var dictType = GetVariableType(model, "dict");

        dictType.Should().Be("Dictionary");
    }

    [TestMethod]
    public void UntypedDictionary_IntKeys_ReturnsDictionaryType()
    {
        var code = @"
extends Node
func test():
    var dict = {1: ""a"", 2: ""b""}
";
        var model = CreateSemanticModel(code);
        var dictType = GetVariableType(model, "dict");

        AssertDictionaryType(dictType, "int", "String");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    private static string GetVariableType(GDSemanticModel model, string varName)
    {
        // First, try to find any usage of the variable (identifier expression) in code
        // This returns the inferred type including container usage analysis
        var identifier = model.ScriptFile.Class?.AllNodes
            .OfType<GDIdentifierExpression>()
            .FirstOrDefault(id => id.Identifier?.Sequence == varName);

        if (identifier != null)
        {
            return model.GetExpressionType(identifier)?.DisplayName;
        }

        // If no identifier usage found (variable not used after declaration),
        // fall back to the variable's initializer type
        var varDecl = model.ScriptFile.Class?.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName);

        if (varDecl?.Initializer != null)
        {
            return model.GetExpressionType(varDecl.Initializer)?.DisplayName;
        }

        return null;
    }

    private static void AssertUnionTypeEquals(string actual, string expected)
    {
        // Compare container types with union elements, ignoring order
        var actualBase = GDGenericTypeHelper.ExtractBaseTypeName(actual ?? "");
        var expectedBase = GDGenericTypeHelper.ExtractBaseTypeName(expected ?? "");

        if (!GDGenericTypeHelper.IsGenericType(actual) || !GDGenericTypeHelper.IsGenericType(expected))
        {
            actual.Should().Be(expected);
            return;
        }

        actualBase.Should().Be(expectedBase, "container type should match");

        var actualElement = GDGenericTypeHelper.ExtractArrayElementType(actual);
        var expectedElement = GDGenericTypeHelper.ExtractArrayElementType(expected);

        if (actualElement == null || expectedElement == null)
        {
            actual.Should().Be(expected);
            return;
        }

        var actualTypes = GDGenericTypeHelper.SplitUnionTypes(actualElement).OrderBy(t => t).ToArray();
        var expectedTypes = GDGenericTypeHelper.SplitUnionTypes(expectedElement).OrderBy(t => t).ToArray();

        actualTypes.Should().BeEquivalentTo(expectedTypes, "union types should contain same elements");
    }

    /// <summary>
    /// Asserts dictionary type with expected key type and value type(s).
    /// Handles formatting differences (comma spacing, union pipe spacing).
    /// </summary>
    private static void AssertDictionaryType(string actual, string expectedKeyType, params string[] expectedValueTypes)
    {
        actual.Should().NotBeNull("dictionary type should not be null");

        // Parse "Dictionary[Key,Value]" or "Dictionary[Key, Value]" or "Dictionary[Key, V1 | V2]"
        var match = System.Text.RegularExpressions.Regex.Match(actual, @"^Dictionary\[(.+?),\s*(.+)\]$");
        match.Success.Should().BeTrue($"'{actual}' should be a Dictionary[K,V] type");

        var actualKey = match.Groups[1].Value.Trim();
        actualKey.Should().Be(expectedKeyType, "key type should match");

        var actualValueParts = match.Groups[2].Value.Split(new[] { '|', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(t => t).ToArray();
        var expectedValueParts = expectedValueTypes.OrderBy(t => t).ToArray();

        actualValueParts.Should().BeEquivalentTo(expectedValueParts, "value types should match");
    }

    #endregion
}
