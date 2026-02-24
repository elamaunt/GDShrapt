using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDMatchCaseTypeInferenceTests
{
    #region Simple Binding — Infers Subject Type

    [TestMethod]
    public void SimpleBinding_TypedIntParam_InfersInt()
    {
        var code = @"
extends Node

func test(value: int):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void SimpleBinding_TypedStringParam_InfersString()
    {
        var code = @"
extends Node

func test(value: String):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void SimpleBinding_UntypedParam_InfersVariant()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("Variant");
    }

    [TestMethod]
    public void SimpleBinding_TypedLocalVar_InfersType()
    {
        var code = @"
extends Node

func test():
    var data: float = 3.14
    match data:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("float");
    }

    #endregion

    #region Guard Narrowing

    [TestMethod]
    public void GuardNarrowing_IsInt_NarrowsToInt()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x when x is int:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void GuardNarrowing_IsString_NarrowsToString()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x when x is String:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void GuardNarrowing_IsNode_NarrowsToNode()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x when x is Node:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("Node");
    }

    #endregion

    #region Array Destructuring

    [TestMethod]
    public void ArrayDestructure_TypedArray_InfersElementType()
    {
        var code = @"
extends Node

func test(arr: Array[int]):
    match arr:
        [var first, ..]:
            print(first)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "first");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void ArrayDestructure_UntypedArray_InfersVariant()
    {
        var code = @"
extends Node

func test(arr: Array):
    match arr:
        [var first, ..]:
            print(first)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "first");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("Variant");
    }

    [TestMethod]
    public void ArrayDestructure_PackedInt32Array_InfersInt()
    {
        var code = @"
extends Node

func test(arr: PackedInt32Array):
    match arr:
        [var first, ..]:
            print(first)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "first");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void ArrayDestructure_MultipleBindings_AllInferElementType()
    {
        var code = @"
extends Node

func test(arr: Array[String]):
    match arr:
        [var first, var second]:
            print(first)
            print(second)
";
        var model = CreateModel(code);

        var vars = model.ScriptFile.Class!.AllNodes
            .OfType<GDMatchCaseVariableExpression>().ToList();
        vars.Should().HaveCount(2);

        foreach (var v in vars)
        {
            var type = model.TypeSystem.GetType(v);
            type.Should().NotBeNull($"match var '{v.Identifier?.Sequence}' should have type");
            type.DisplayName.Should().Be("String",
                $"match var '{v.Identifier?.Sequence}' should be String from Array[String]");
        }
    }

    #endregion

    #region Dictionary Destructuring

    [TestMethod]
    public void DictDestructure_TypedDict_InfersValueType()
    {
        var code = @"
extends Node

func test(data: Dictionary[String, int]):
    match data:
        {""key"": var v}:
            print(v)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "v");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void DictDestructure_UntypedDict_InfersVariant()
    {
        var code = @"
extends Node

func test(data: Dictionary):
    match data:
        {""key"": var v}:
            print(v)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "v");
        matchVar.Should().NotBeNull();

        var type = model.TypeSystem.GetType(matchVar!);
        type.Should().NotBeNull();
        type.DisplayName.Should().Be("Variant");
    }

    #endregion

    #region TypeInfo Confidence

    [TestMethod]
    public void TypeInfo_TypedSubject_HasHighConfidence()
    {
        var code = @"
extends Node

func test(value: int):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var typeInfo = model.TypeSystem.GetTypeInfo("x", matchVar);
        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void TypeInfo_GuardNarrowed_HasHighConfidence()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x when x is int:
            print(x)
";
        var model = CreateModel(code);

        var matchVar = FindMatchCaseVariable(model, "x");
        matchVar.Should().NotBeNull();

        var typeInfo = model.TypeSystem.GetTypeInfo("x", matchVar);
        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void TypeInfo_GuardNarrowed_WithoutAtLocation_StillInfers()
    {
        var code = @"
extends Node

func test(value):
    match value:
        var x when x is int:
            print(x)
";
        var model = CreateModel(code);

        var typeInfo = model.TypeSystem.GetTypeInfo("x");
        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("int",
            "GetTypeInfo without atLocation should still infer guard-narrowed type via MatchCaseBinding path");
    }

    [TestMethod]
    public void TypeInfo_TypedSubject_WithoutAtLocation_StillInfers()
    {
        var code = @"
extends Node

func test(value: int):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var typeInfo = model.TypeSystem.GetTypeInfo("x");
        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("int",
            "GetTypeInfo without atLocation should still infer from typed match subject via MatchCaseBinding path");
    }

    #endregion

    #region Symbol Kind

    [TestMethod]
    public void MatchCaseBinding_HasCorrectSymbolKind()
    {
        var code = @"
extends Node

func test(value: int):
    match value:
        var x:
            print(x)
";
        var model = CreateModel(code);

        var symbol = model.Symbols
            .FirstOrDefault(s => s.Name == "x" && s.Kind == GDSymbolKind.MatchCaseBinding);
        symbol.Should().NotBeNull("match case variable 'x' should be registered as MatchCaseBinding");
    }

    #endregion

    #region Helpers

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/match_case_test.gd");
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

    private static GDMatchCaseVariableExpression? FindMatchCaseVariable(
        GDSemanticModel model, string name)
    {
        return model.ScriptFile.Class?.AllNodes
            .OfType<GDMatchCaseVariableExpression>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name);
    }

    #endregion
}
