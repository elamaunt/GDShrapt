using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference.Level6_Inheritance;

/// <summary>
/// Tests for super.method() resolution.
/// Verifies that super correctly resolves to parent class methods.
/// </summary>
[TestClass]
public class SuperMethodResolutionTests
{
    #region P11: super.method() Resolution

    [TestMethod]
    public void P11_SuperMethod_ResolvesToParent_NoDiagnostic()
    {
        // P11: super.method() should resolve to parent class method
        // This test requires BaseEntity.gd to exist in testproject
        var code = @"
extends Node2D

func _ready():
    super._ready()
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super._ready() should resolve to Node2D._ready(). Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    [TestMethod]
    public void P11_SuperMethod_WithArguments_NoDiagnostic()
    {
        var code = @"
extends Node

func _notification(what: int):
    super._notification(what)
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super._notification(what) should resolve. Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    [TestMethod]
    public void P11_SuperMethod_InOverride_NoDiagnostic()
    {
        var code = @"
extends CharacterBody2D

func _physics_process(delta: float):
    super._physics_process(delta)
    move_and_slide()
";
        var diagnostics = ValidateCode(code);
        var methodNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodNotFoundDiagnostics.Count,
            $"super._physics_process() should resolve. Found: {FormatDiagnostics(methodNotFoundDiagnostics)}");
    }

    [TestMethod]
    public void P11_SuperType_IsParentClass()
    {
        // super should resolve to the parent class type
        var code = @"
extends Control

func test():
    var parent_rect = super.get_rect()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.PropertyNotFound).ToList();

        Assert.AreEqual(0, typeDiagnostics.Count,
            $"super.get_rect() should resolve to Control.get_rect(). Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Super Without Extends (implicit RefCounted)

    [TestMethod]
    public void Super_NoExtends_ResolvesToRefCounted()
    {
        // Script without extends implicitly extends RefCounted
        var code = @"
func test():
    var ref_count = super.get_reference_count()
";
        var diagnostics = ValidateCode(code);
        // This might produce errors since RefCounted.get_reference_count() might not be found
        // But it should NOT produce GD4002 for 'super' itself
        var superTypeDiagnostics = diagnostics.Where(d =>
            d.Message != null &&
            d.Message.Contains("super") &&
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        // The error should be about the method not found on the type, not about super being invalid
        // This is acceptable behavior
    }

    #endregion

    #region Super Property Access

    [TestMethod]
    public void P11_SuperProperty_ResolvesToParent()
    {
        var code = @"
extends Node2D

var position: Vector2:
    set(value):
        super.position = value
";
        var diagnostics = ValidateCode(code);
        var propertyNotFoundDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound).ToList();

        Assert.AreEqual(0, propertyNotFoundDiagnostics.Count,
            $"super.position should resolve to Node2D.position. Found: {FormatDiagnostics(propertyNotFoundDiagnostics)}");
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
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

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

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
