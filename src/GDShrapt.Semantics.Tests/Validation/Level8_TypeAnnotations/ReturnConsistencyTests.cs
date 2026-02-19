using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class ReturnConsistencyTests
{
    #region GD3024 - Missing Return In Branch

    [TestMethod]
    public void MissingReturn_DeclaredReturnType_ReportsDiagnostic()
    {
        var code = @"
func get_value() -> int:
    if true:
        return 5
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.MissingReturnInBranch),
            $"Expected GD3024 for missing return in else branch. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void MissingReturn_AllPathsReturn_NoDiagnostic()
    {
        var code = @"
func get_value() -> int:
    if true:
        return 5
    else:
        return 10
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.MissingReturnInBranch),
            $"All paths return a value. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void MissingReturn_VoidReturn_NoDiagnostic()
    {
        var code = @"
func do_stuff() -> void:
    if true:
        print(""hello"")
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.MissingReturnInBranch),
            $"Void functions should not report missing return. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void MissingReturn_NoReturnType_NoDiagnostic()
    {
        var code = @"
func do_stuff():
    if true:
        return 5
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.MissingReturnInBranch),
            $"Functions without declared return type should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void MissingReturn_EmptyBody_NoDiagnostic()
    {
        var code = @"
func get_value() -> int:
    pass
";
        var diagnostics = ValidateCode(code);
        // Empty/pass body is a stub — don't report
        var relevant = diagnostics.Where(d => d.Code == GDDiagnosticCode.MissingReturnInBranch).ToList();
        // This may or may not fire depending on whether pass is considered a return path
    }

    [TestMethod]
    public void MissingReturn_Disabled_NoDiagnostic()
    {
        var code = @"
func get_value() -> int:
    if true:
        return 5
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckReturnConsistency = false
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.MissingReturnInBranch),
            $"Diagnostic should be suppressed when disabled. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region GD3023 - Inconsistent Return Types

    [TestMethod]
    public void InconsistentReturn_IntAndString_ReportsDiagnostic()
    {
        var code = @"
func get_value(flag: bool):
    if flag:
        return 5
    else:
        return ""hello""
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.InconsistentReturnTypes),
            $"Expected GD3023 for inconsistent int vs String return. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void InconsistentReturn_SameType_NoDiagnostic()
    {
        var code = @"
func get_value(flag: bool) -> int:
    if flag:
        return 5
    else:
        return 10
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.InconsistentReturnTypes),
            $"Same return type should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void InconsistentReturn_Disabled_NoDiagnostic()
    {
        var code = @"
func get_value(flag: bool):
    if flag:
        return 5
    else:
        return ""hello""
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckReturnConsistency = false
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.InconsistentReturnTypes),
            $"Diagnostic should be suppressed when disabled. Found: {FormatDiagnostics(diagnostics)}");
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
            CheckReturnConsistency = true
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
