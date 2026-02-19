using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class AnnotationNarrowingTests
{
    #region GD3022 - Annotation Wider Than Inferred

    [TestMethod]
    public void WiderAnnotation_NodeForSprite_ReportsDiagnostic()
    {
        var code = @"
var enemy: Node = Sprite2D.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Expected GD3022 for Node wider than Sprite2D. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_ExactType_NoDiagnostic()
    {
        var code = @"
var enemy: Sprite2D = Sprite2D.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Exact match should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_Variant_NoDiagnostic()
    {
        var code = @"
var data: Variant = Sprite2D.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Variant annotation should be skipped. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_NoAnnotation_NoDiagnostic()
    {
        var code = @"
var enemy = Sprite2D.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"No annotation means nothing to compare. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_Disabled_NoDiagnostic()
    {
        var code = @"
var enemy: Node = Sprite2D.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckAnnotationNarrowing = false
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Should not report when disabled. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_NullInitializer_NoDiagnostic()
    {
        var code = @"
var target: Node2D = null
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Null initializer is a standard pattern, should not report wider. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void WiderAnnotation_FloatOnIntLiteral_NoDiagnostic()
    {
        var code = @"
var speed: float = 5
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"float annotation on int literal is numeric conversion, not widening. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region GD7022 - Redundant Annotation

    [TestMethod]
    public void RedundantAnnotation_IntLiteral_ReportsDiagnostic()
    {
        var code = @"
var health: int = 100
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckRedundantAnnotations = true
        });
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"Expected GD7022 for redundant int annotation on int literal. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void RedundantAnnotation_StringLiteral_ReportsDiagnostic()
    {
        var code = @"
var name: String = ""player""
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckRedundantAnnotations = true
        });
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"Expected GD7022 for redundant String annotation on string literal. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void RedundantAnnotation_BoolLiteral_ReportsDiagnostic()
    {
        var code = @"
var active: bool = true
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckRedundantAnnotations = true
        });
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"Expected GD7022 for redundant bool annotation on bool literal. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void RedundantAnnotation_FloatOnInt_NoDiagnostic()
    {
        var code = @"
var speed: float = 5
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckRedundantAnnotations = true
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"float annotation on int literal is a conversion, not redundant. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void RedundantAnnotation_NonLiteral_NoDiagnostic()
    {
        var code = @"
var node: Node = Node.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckRedundantAnnotations = true
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"Non-literal initializer should not fire redundant annotation. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void RedundantAnnotation_DisabledByDefault_NoDiagnostic()
    {
        var code = @"
var health: int = 100
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantAnnotation),
            $"GD7022 should be disabled by default. Found: {FormatDiagnostics(diagnostics)}");
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

        options ??= new GDSemanticValidatorOptions();
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
