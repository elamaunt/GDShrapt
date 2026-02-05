using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level6_RedundantGuards;

/// <summary>
/// Level 6: Redundant guard validation tests.
/// Tests validate that redundant type guards and null checks are detected.
/// </summary>
[TestClass]
public class RedundantGuardValidationTests
{
    #region Redundant Type Guard - Should Report

    [TestMethod]
    public void TypedVariable_IsCheck_ReportsRedundant()
    {
        var code = @"
func test():
    var arr: Array = []
    if arr is Array:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantTypeGuard),
            $"Expected GD7010 for redundant is check. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    [TestMethod]
    public void NestedIsCheck_SameType_ReportsRedundant()
    {
        var code = @"
func test(x):
    if x is Array:
        if x is Array:
            pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantNarrowedTypeGuard),
            $"Expected GD7011 for nested redundant is check. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Redundant Null Check - Should Report

    [TestMethod]
    public void PrimitiveVariable_NullCheck_ReportsRedundant()
    {
        var code = @"
func test():
    var x: int = 5
    if x != null:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantNullCheck),
            $"Expected GD7013 for redundant null check on int. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Different Type Guard - Should NOT Report

    [TestMethod]
    public void NestedIsCheck_DifferentType_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x is Array:
        if x is PackedByteArray:
            pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Different type is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Variant Variable - Should NOT Report

    [TestMethod]
    public void VariantVariable_IsCheck_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x is Array:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Variant variable type check is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    [TestMethod]
    public void VariantVariable_NullCheck_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x != null:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Variant variable null check is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
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
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckRedundantGuards = true,
            RedundantGuardSeverity = GDDiagnosticSeverity.Hint
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterRedundantDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.RedundantTypeGuard ||
            d.Code == GDDiagnosticCode.RedundantNarrowedTypeGuard ||
            d.Code == GDDiagnosticCode.RedundantHasMethodCheck ||
            d.Code == GDDiagnosticCode.RedundantNullCheck ||
            d.Code == GDDiagnosticCode.RedundantTruthinessCheck).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
