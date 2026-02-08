using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for ternary operator (x if cond else y) type inference
/// through the GDSemanticModel's TypeSystem API.
/// </summary>
[TestClass]
public class TernaryTypeInferenceTests
{
    [TestMethod]
    public void Ternary_SameIntBranches_ReturnsInt()
    {
        var code = @"
extends Node

var flag: bool = true
var a: int = 10

func test_same_type():
    var result = a if flag else 20
";
        var model = CreateSemanticModel(code);

        var ternary = FindFirstTernaryInMethod(model, "test_same_type");
        ternary.Should().NotBeNull("should find ternary expression in test_same_type");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int",
            "both branches are int, so the ternary should infer as int");
    }

    [TestMethod]
    public void Ternary_DifferentTypes_IntAndFloat_ReturnsUnionOrVariant()
    {
        var code = @"
extends Node

var flag: bool = true
var a: int = 10
var b: float = 3.14

func test_different_types():
    var result = a if flag else b
";
        var model = CreateSemanticModel(code);

        var ternary = FindFirstTernaryInMethod(model, "test_different_types");
        ternary.Should().NotBeNull("should find ternary expression in test_different_types");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();

        var name = type.DisplayName;
        var isValid = name == "int" ||
                      name == "float" ||
                      name == "Variant" ||
                      name.Contains("|");
        isValid.Should().BeTrue(
            $"expected int, float, union, or Variant for mixed int/float ternary, got: {name}");
    }

    [TestMethod]
    public void Ternary_StringBranches_ReturnsString()
    {
        var code = @"
extends Node

var flag: bool = true
var name: String = ""hello""

func test_string():
    var result = name if flag else ""world""
";
        var model = CreateSemanticModel(code);

        var ternary = FindFirstTernaryInMethod(model, "test_string");
        ternary.Should().NotBeNull("should find ternary expression in test_string");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("String",
            "both branches are String, so the ternary should infer as String");
    }

    [TestMethod]
    public void Ternary_NullBranch_ReturnsNullableOrTrueBranchType()
    {
        var code = @"
extends Node

var flag: bool = true

func test_null_branch():
    var result = self if flag else null
";
        var model = CreateSemanticModel(code);

        var ternary = FindFirstTernaryInMethod(model, "test_null_branch");
        ternary.Should().NotBeNull("should find ternary expression in test_null_branch");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();

        var name = type.DisplayName;
        var isValid = name == "Node" ||
                      name == "self" ||
                      name == "Variant" ||
                      name == "null" ||
                      name.Contains("null") ||
                      name.Contains("|");
        isValid.Should().BeTrue(
            $"expected Node, self, null, union, or Variant for self-or-null ternary, got: {name}");
    }

    [TestMethod]
    public void Ternary_Nested_AllIntBranches_ReturnsInt()
    {
        var code = @"
extends Node

var flag: bool = true
var a: int = 10
var b: float = 3.14

func test_nested():
    var result = a if flag else (20 if !flag else 0)
";
        var model = CreateSemanticModel(code);

        var varDecl = FindVarDeclInMethod(model, "test_nested", "result");
        varDecl.Should().NotBeNull("should find 'result' variable in test_nested");
        varDecl!.Initializer.Should().NotBeNull("result variable should have an initializer");

        var type = model.TypeSystem.GetType(varDecl.Initializer!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int",
            "all branches produce int literals or int variable, so result should be int");
    }

    [TestMethod]
    public void Ternary_AsArgument_InfersCorrectType()
    {
        var code = @"
extends Node

var flag: bool = true
var a: int = 10

func test_as_argument():
    print(a if flag else 0)
";
        var model = CreateSemanticModel(code);

        var ternary = model.ScriptFile.Class!.AllNodes
            .OfType<GDIfExpression>()
            .FirstOrDefault();
        ternary.Should().NotBeNull("should find ternary expression used as print argument");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int",
            "both branches are int, so the ternary argument should infer as int");
    }

    [TestMethod]
    public void Ternary_AssignedToTypedVar_InfersCorrectType()
    {
        var code = @"
extends Node

var flag: bool = true
var a: int = 10

func test_typed_var():
    var result: int = a if flag else 0
";
        var model = CreateSemanticModel(code);

        var ternary = FindFirstTernaryInMethod(model, "test_typed_var");
        ternary.Should().NotBeNull("should find ternary expression in test_typed_var");

        var type = model.TypeSystem.GetType(ternary!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int",
            "ternary with int branches assigned to typed int var should infer as int");
    }

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/ternary_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        scriptFile.Analyze(runtimeProvider);
        return scriptFile.SemanticModel!;
    }

    private static GDIfExpression? FindFirstTernaryInMethod(GDSemanticModel model, string methodName)
    {
        var method = model.ScriptFile.Class?.Methods?
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
        if (method == null)
            return null;

        return method.AllNodes.OfType<GDIfExpression>().FirstOrDefault();
    }

    private static GDVariableDeclarationStatement? FindVarDeclInMethod(
        GDSemanticModel model, string methodName, string varName)
    {
        var method = model.ScriptFile.Class?.Methods?
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
        if (method == null)
            return null;

        return method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName);
    }

    #endregion
}
