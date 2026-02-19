using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class ParameterTypeHintTests
{
    #region GD7020 - Call Site Parameter Type Consensus

    [TestMethod]
    public void ParameterHint_TypedParameter_NoDiagnostic()
    {
        var code = @"
func take_damage(amount: int):
    pass

func test():
    take_damage(10)
    take_damage(20)
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckParameterTypeHints = true
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.CallSiteParameterTypeConsensus),
            $"Typed parameter should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ParameterHint_UnderscoredParam_NoDiagnostic()
    {
        var code = @"
func process(_delta):
    pass
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckParameterTypeHints = true
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.CallSiteParameterTypeConsensus),
            $"_ prefixed params should be skipped. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ParameterHint_DisabledByDefault_NoDiagnostic()
    {
        var code = @"
func take_damage(amount):
    pass

func test():
    take_damage(10)
    take_damage(20)
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.CallSiteParameterTypeConsensus),
            $"GD7020 should be disabled by default. Found: {FormatDiagnostics(diagnostics)}");
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
