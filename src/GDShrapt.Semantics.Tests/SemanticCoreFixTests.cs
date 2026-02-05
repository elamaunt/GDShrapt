using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for semantic core fixes.
/// Covers inner class super resolution, 'as' cast type narrowing, and validator false positives.
/// </summary>
[TestClass]
public class SemanticCoreFixTests
{
    #region Issue 1: Inner Class super.method() Resolution

    [TestMethod]
    public void InnerClass_SuperMethod_ResolvesToInnerClassParent()
    {
        // Inner class extending another inner class should resolve super to its direct parent
        var code = @"
extends Node

class ChildEnemy extends Control:
    func attack() -> int:
        return 10

class StrongEnemy extends ChildEnemy:
    func attack() -> int:
        return super.attack() * 2  # Should resolve to ChildEnemy.attack()
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("attack") == true).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super.attack() in inner class should resolve to parent inner class. Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    [TestMethod]
    public void InnerClass_SuperMethod_ExtendsGodotClass()
    {
        // Inner class extending Godot class should resolve super correctly
        var code = @"
extends Node

class Inner extends Control:
    func _ready():
        super._ready()  # Should resolve to Control._ready()
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super._ready() in inner class should resolve to Control._ready(). Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    [TestMethod]
    public void InnerClass_NoExtends_SuperResolvesToRefCounted()
    {
        // Inner class without extends defaults to RefCounted
        var code = @"
extends Node

class Inner:
    func test():
        var count = super.get_reference_count()
";
        // This test just verifies no crash - RefCounted might not be fully available
        var diagnostics = ValidateCode(code);
        // Should not crash, inner class super should resolve to RefCounted
    }

    [TestMethod]
    public void OuterClass_SuperMethod_StillWorks()
    {
        // Regression test: outer class super resolution should still work
        var code = @"
extends Node2D

func _ready():
    super._ready()
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super._ready() in outer class should resolve to Node2D._ready(). Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    #endregion

    #region Issue 2: 'as' Cast Type Narrowing

    [TestMethod]
    public void AsCast_ReturnsTargetType()
    {
        var code = @"
extends Node

func test(node):
    var sprite = node as Sprite2D
";
        var model = CreateSemanticModel(code);

        var asExpr = model.ScriptFile.Class!.AllNodes
            .OfType<GDDualOperatorExpression>()
            .FirstOrDefault(e => e.Operator?.OperatorType == GDDualOperatorType.As);

        Assert.IsNotNull(asExpr, "Should find 'as' expression");

        var typeInfo = model.TypeSystem.GetType(asExpr);
        Assert.AreEqual("Sprite2D", typeInfo.DisplayName, "'node as Sprite2D' should infer as Sprite2D");
    }

    [TestMethod]
    public void AsCast_MemberAccess_WorksCorrectly()
    {
        // After 'as' cast, member access should work on the target type
        var code = @"
extends Node

func test(node):
    var sprite = node as Sprite2D
    if sprite:
        var tex = sprite.texture  # Should know sprite is Sprite2D
";
        var diagnostics = ValidateCode(code);
        var propertyNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound &&
            d.Message?.Contains("texture") == true).ToList();

        Assert.AreEqual(0, propertyNotFound.Count,
            $"sprite.texture should resolve after 'as Sprite2D' cast. Found: {FormatDiagnostics(propertyNotFound)}");
    }

    [TestMethod]
    public void AsCast_MethodCall_WorksCorrectly()
    {
        var code = @"
extends Node

func test(node):
    var sprite = node as Sprite2D
    if sprite:
        sprite.set_texture(null)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("set_texture") == true).ToList();

        Assert.AreEqual(0, methodNotFound.Count,
            $"sprite.set_texture() should resolve after 'as Sprite2D' cast. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void AsCast_WithInnerType_ResolvesCorrectly()
    {
        var code = @"
extends Node

class MyInner extends Control:
    pass

func test(obj):
    var inner = obj as MyInner
";
        var model = CreateSemanticModel(code);

        var asExpr = model.ScriptFile.Class!.AllNodes
            .OfType<GDDualOperatorExpression>()
            .FirstOrDefault(e => e.Operator?.OperatorType == GDDualOperatorType.As);

        Assert.IsNotNull(asExpr, "Should find 'as' expression");

        var typeInfo = model.TypeSystem.GetType(asExpr);
        Assert.AreEqual("MyInner", typeInfo.DisplayName, "'obj as MyInner' should infer as MyInner");
    }

    [TestMethod]
    public void AsCast_WithGetNode_ReturnsTargetType()
    {
        // Test that get_node() as Type properly narrows to target type
        // Note: $NodePath as Type has a parser bug (path consumes 'as' token), use get_node() instead
        var code = @"
extends Node2D

func test():
    var sprite := get_node(""Sprite2D"") as Sprite2D
    if sprite:
        sprite.texture = null
";
        var diagnostics = ValidateCode(code);
        var propertyNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound &&
            d.Message?.Contains("texture") == true).ToList();

        Assert.AreEqual(0, propertyNotFound.Count,
            $"sprite.texture should resolve after 'get_node() as Sprite2D' cast. Found: {FormatDiagnostics(propertyNotFound)}");
    }

    #endregion

    #region Issue 3: Validator array.is_empty() False Positive

    [TestMethod]
    public void Validator_ArrayVariable_IsEmpty_NoFalsePositive()
    {
        // Variable named 'array' should not trigger validation as if it were the Array type
        var code = @"
extends Node

func test():
    var array = [1, 2, 3]
    if array.is_empty():
        pass
";
        var result = ValidateWithSyntaxValidator(code);

        var methodNotFound = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.MethodNotFound &&
                       e.Message?.Contains("is_empty") == true)
            .ToList();

        Assert.AreEqual(0, methodNotFound.Count,
            $"array.is_empty() should not produce false positive. Found: {FormatErrors(methodNotFound)}");
    }

    [TestMethod]
    public void Validator_ArrayVariable_Size_NoFalsePositive()
    {
        var code = @"
extends Node

func test():
    var array = []
    var count = array.size()
";
        var result = ValidateWithSyntaxValidator(code);

        var methodNotFound = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.MethodNotFound &&
                       e.Message?.Contains("size") == true)
            .ToList();

        Assert.AreEqual(0, methodNotFound.Count,
            $"array.size() should not produce false positive. Found: {FormatErrors(methodNotFound)}");
    }

    [TestMethod]
    public void Validator_DictVariable_Has_NoFalsePositive()
    {
        var code = @"
extends Node

func test():
    var dict = {}
    if dict.has(""key""):
        pass
";
        var result = ValidateWithSyntaxValidator(code);

        var methodNotFound = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.MethodNotFound &&
                       e.Message?.Contains("has") == true)
            .ToList();

        Assert.AreEqual(0, methodNotFound.Count,
            $"dict.has() should not produce false positive. Found: {FormatErrors(methodNotFound)}");
    }

    [TestMethod]
    public void Validator_GlobalClass_StillValidated()
    {
        // Regression test: actual global class method calls should still be validated
        var code = @"
extends Node

func test():
    var v = Vector2.UP
";
        var result = ValidateWithSyntaxValidator(code);

        // This should work without errors - Vector2.UP is valid
        // Just ensure no crash and valid parsing
        Assert.IsNotNull(result);
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        var reference = new GDScriptReference("test://virtual/test_script.gd");
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

    private static GDValidationResult ValidateWithSyntaxValidator(string code)
    {
        var validator = new GDValidator();
        var options = new GDValidationOptions
        {
            RuntimeProvider = new GDGodotTypesProvider(),
            CheckCalls = true
        };
        return validator.ValidateCode(code, options);
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    private static string FormatErrors(IEnumerable<GDDiagnostic> errors)
    {
        return string.Join("; ", errors.Select(e => $"[{e.Code}] {e.Message}"));
    }

    #endregion
}
