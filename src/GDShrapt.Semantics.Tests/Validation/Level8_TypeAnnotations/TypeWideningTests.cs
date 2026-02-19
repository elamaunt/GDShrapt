using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class TypeWideningTests
{
    #region GD7019 - Type Widening Assignment

    [TestMethod]
    public void TypeWidening_SpriteToNode_ReportsDiagnostic()
    {
        var code = @"
func test():
    var sprite: Sprite2D = Sprite2D.new()
    sprite = Node.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"Expected GD7019 for widening Sprite2D to Node. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void TypeWidening_SameType_NoDiagnostic()
    {
        var code = @"
func test():
    var sprite: Sprite2D = Sprite2D.new()
    sprite = Sprite2D.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"Same type assignment should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void TypeWidening_NoAnnotation_NoDiagnostic()
    {
        var code = @"
func test():
    var sprite = Sprite2D.new()
    sprite = Node.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"Untyped variable should not report widening. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void TypeWidening_Disabled_NoDiagnostic()
    {
        var code = @"
func test():
    var sprite: Sprite2D = Sprite2D.new()
    sprite = Node.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckTypeWidening = false
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"Should not report when disabled. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void TypeWidening_IntToFloat_NoDiagnostic()
    {
        var code = @"
func test():
    var value: int = 0
    value = 3.14
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"int to float is implicit numeric conversion, not widening. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void TypeWidening_FloatToInt_NoDiagnostic()
    {
        var code = @"
func test():
    var value: float = 1.5
    value = 10
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"float to int is implicit numeric conversion, not widening. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code, GDSemanticValidatorOptions? options = null)
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
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        options ??= new GDSemanticValidatorOptions
        {
            CheckTypeWidening = true
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
