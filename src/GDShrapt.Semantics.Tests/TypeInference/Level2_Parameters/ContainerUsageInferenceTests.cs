using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

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
            return model.GetExpressionType(identifier);
        }

        // If no identifier usage found (variable not used after declaration),
        // fall back to the variable's initializer type
        var varDecl = model.ScriptFile.Class?.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName);

        if (varDecl?.Initializer != null)
        {
            return model.GetExpressionType(varDecl.Initializer);
        }

        return null;
    }

    private static void AssertUnionTypeEquals(string actual, string expected)
    {
        // Parse container types like "Array[String|bool|int]" and compare union contents ignoring order
        var actualMatch = System.Text.RegularExpressions.Regex.Match(actual ?? "", @"^(\w+)\[(.+)\]$");
        var expectedMatch = System.Text.RegularExpressions.Regex.Match(expected ?? "", @"^(\w+)\[(.+)\]$");

        if (!actualMatch.Success || !expectedMatch.Success)
        {
            actual.Should().Be(expected);
            return;
        }

        actualMatch.Groups[1].Value.Should().Be(expectedMatch.Groups[1].Value, "container type should match");

        var actualTypes = actualMatch.Groups[2].Value.Split('|').OrderBy(t => t).ToArray();
        var expectedTypes = expectedMatch.Groups[2].Value.Split('|').OrderBy(t => t).ToArray();

        actualTypes.Should().BeEquivalentTo(expectedTypes, "union types should contain same elements");
    }

    #endregion
}
