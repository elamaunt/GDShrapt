using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level3_Methods;

/// <summary>
/// Tests that GDMemberOperatorExpression returns Callable signature for methods
/// and property type for properties, via GDExpressionTypeService.GetExpressionType().
/// </summary>
[TestClass]
public class MemberMethodCallableSignatureTests
{
    [TestMethod]
    public void MemberMethodRef_DictionaryDuplicate_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var d: Dictionary = {}
    var ref_val = d.duplicate
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "duplicate");

        memberExpr.Should().NotBeNull();

        // Verify caller type resolves correctly
        var callerIdent = memberExpr!.CallerExpression as GDIdentifierExpression;
        callerIdent.Should().NotBeNull();
        var callerType = semanticModel.GetExpressionType(callerIdent!)?.DisplayName;
        callerType.Should().Be("Dictionary",
            $"Caller 'd' should resolve to Dictionary, got: {callerType}");

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> Dictionary",
            $"Dictionary.duplicate member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberMethodRef_ArrayDuplicate_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var a: Array = []
    var ref_val = a.duplicate
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "duplicate");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> Array",
            $"Array.duplicate member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberMethodRef_Vector2MoveToward_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var v: Vector2 = Vector2.ZERO
    var ref_val = v.move_toward
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "move_toward");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> Vector2",
            $"Vector2.move_toward member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberMethodRef_Vector2DistanceTo_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var v: Vector2 = Vector2.ZERO
    var ref_val = v.distance_to
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "distance_to");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> float",
            $"Vector2.distance_to member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberMethodRef_StringToUpper_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var s: String = ""hello""
    var ref_val = s.to_upper
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "to_upper");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> String",
            $"String.to_upper member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberMethodRef_ArraySize_ReturnsCallableSignature()
    {
        var code = @"
class_name Test
extends Node

func test():
    var a: Array = []
    var ref_val = a.size
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "size");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().StartWith("Callable(")
            .And.Contain("-> int",
            $"Array.size member reference should be Callable with signature, got: {type}");
    }

    [TestMethod]
    public void MemberProperty_Vector2X_ReturnsPropertyType()
    {
        var code = @"
class_name Test
extends Node

func test():
    var v: Vector2 = Vector2.ZERO
    var ref_val = v.x
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var memberExpr = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "x"
                && m.CallerExpression is GDIdentifierExpression id
                && id.Identifier?.Sequence == "v");

        memberExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(memberExpr!)?.DisplayName;
        type.Should().Be("float",
            "Vector2.x is a property, should return property type, not Callable");
    }

    [TestMethod]
    public void MemberMethodRef_DirectCall_ReturnsReturnType()
    {
        var code = @"
class_name Test
extends Node

func test():
    var a: Array = []
    var result = a.size()
";
        var (semanticModel, scriptFile) = BuildSemanticModel(code);

        var callExpr = scriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .FirstOrDefault(c =>
                c.CallerExpression is GDMemberOperatorExpression m &&
                m.Identifier?.Sequence == "size");

        callExpr.Should().NotBeNull();

        var type = semanticModel.GetExpressionType(callExpr!)?.DisplayName;
        type.Should().Be("int",
            "Direct call a.size() should return int, not Callable");
    }

    #region Helper Methods

    private static (GDSemanticModel semanticModel, GDScriptFile scriptFile) BuildSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDGodotTypesProvider();
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (semanticModel, scriptFile);
    }

    #endregion
}
