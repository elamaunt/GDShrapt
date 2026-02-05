using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Level 0: Comparison operator validation tests.
/// Tests validate that comparison operators with null/incompatible types are detected.
/// </summary>
[TestClass]
public class ComparisonOperatorValidationTests
{
    #region Error: Exact Null Type

    [TestMethod]
    public void ComparisonWithNull_ExplicitNull_ReportsError()
    {
        var code = @"
func test():
    var x = null
    if x < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"Expected GD3019 for null < 5. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonWithNull_NullLiteral_ReportsError()
    {
        var code = @"
func test():
    if null > 10:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"Expected GD3019 for null > 10. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonWithNull_NullOnRight_ReportsError()
    {
        var code = @"
func test():
    if 5 <= null:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"Expected GD3019 for 5 <= null. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonWithNull_GreaterOrEqual_ReportsError()
    {
        var code = @"
func test():
    var x = null
    if x >= 0:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"Expected GD3019 for null >= 0. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Warning: Potentially Null Variable

    [TestMethod]
    public void ComparisonPotentiallyNull_UntypedParam_ReportsWarning()
    {
        var code = @"
func test(x):
    if x < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"Expected GD3020 for untyped param in comparison. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonPotentiallyNull_AfterNullCheck_NoWarning()
    {
        var code = @"
func test(x):
    if x != null and x < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"After null check, comparison should be safe. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonPotentiallyNull_TypedParam_NoWarning()
    {
        var code = @"
func test(x: int):
    if x < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"Typed param should not warn. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonPotentiallyNull_AfterIsCheck_NoWarning()
    {
        var code = @"
func test(x):
    if x is int and x < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"After 'is' check, comparison should be safe. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Warning: Incompatible Types

    [TestMethod]
    public void ComparisonIncompatible_StringAndInt_ReportsWarning()
    {
        var code = @"
func test():
    if ""str"" < 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.IncompatibleComparisonTypes),
            $"Expected GD3021 for String < int. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ComparisonIncompatible_ArrayAndInt_ReportsWarning()
    {
        var code = @"
func test():
    if [1, 2] > 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.IncompatibleComparisonTypes),
            $"Expected GD3021 for Array > int. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Valid Comparisons - No Diagnostics

    [TestMethod]
    public void ComparisonCompatible_IntAndInt_NoWarning()
    {
        var code = @"
func test():
    if 5 < 10:
        pass
";
        var diagnostics = ValidateCode(code);
        var comparisonDiagnostics = FilterComparisonDiagnostics(diagnostics);
        Assert.AreEqual(0, comparisonDiagnostics.Count,
            $"int < int should be valid. Found: {FormatDiagnostics(comparisonDiagnostics)}");
    }

    [TestMethod]
    public void ComparisonCompatible_IntAndFloat_NoWarning()
    {
        var code = @"
func test():
    if 5 < 3.14:
        pass
";
        var diagnostics = ValidateCode(code);
        var comparisonDiagnostics = FilterComparisonDiagnostics(diagnostics);
        Assert.AreEqual(0, comparisonDiagnostics.Count,
            $"int < float should be valid. Found: {FormatDiagnostics(comparisonDiagnostics)}");
    }

    [TestMethod]
    public void ComparisonCompatible_FloatAndInt_NoWarning()
    {
        var code = @"
func test():
    if 3.14 > 1:
        pass
";
        var diagnostics = ValidateCode(code);
        var comparisonDiagnostics = FilterComparisonDiagnostics(diagnostics);
        Assert.AreEqual(0, comparisonDiagnostics.Count,
            $"float > int should be valid. Found: {FormatDiagnostics(comparisonDiagnostics)}");
    }

    [TestMethod]
    public void ComparisonCompatible_Strings_NoWarning()
    {
        var code = @"
func test():
    if ""abc"" < ""def"":
        pass
";
        var diagnostics = ValidateCode(code);
        var comparisonDiagnostics = FilterComparisonDiagnostics(diagnostics);
        Assert.AreEqual(0, comparisonDiagnostics.Count,
            $"String < String should be valid. Found: {FormatDiagnostics(comparisonDiagnostics)}");
    }

    [TestMethod]
    public void ComparisonCompatible_TypedVariables_NoWarning()
    {
        var code = @"
func test():
    var a: int = 5
    var b: int = 10
    if a < b:
        pass
";
        var diagnostics = ValidateCode(code);
        var comparisonDiagnostics = FilterComparisonDiagnostics(diagnostics);
        Assert.AreEqual(0, comparisonDiagnostics.Count,
            $"Typed int variables comparison should be valid. Found: {FormatDiagnostics(comparisonDiagnostics)}");
    }

    [TestMethod]
    public void VarWithIntLiteralInitializer_NoGD3020()
    {
        // Bug fix: var x = 0 should infer int type, not Variant
        var code = @"
func test():
    var attempt = 0
    while attempt <= 5:
        attempt += 1
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"var x = 0 should infer int type. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void VarWithFloatLiteralInitializer_NoGD3020()
    {
        // Bug fix: var x = 5.0 should infer float type, not Variant
        var code = @"
func test():
    var timeout = 5.0
    if timeout > 0:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"var x = 5.0 should infer float type. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void VarWithStringLiteralInitializer_NoGD3020()
    {
        // Bug fix: var x = "" should infer String type, not Variant
        var code = @"
func test():
    var name = ""test""
    if name < ""z"":
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"var x = \"\" should infer String type. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void VarWithBoolLiteralInitializer_NoGD3020()
    {
        // Bug fix: var x = true should infer bool type, not Variant
        var code = @"
func test():
    var flag = true
    if flag == false:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"var x = true should infer bool type. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void VarWithMethodCallInitializer_NumericReturn_NoGD3020()
    {
        // Bug fix: method calls returning numeric types should not trigger GD3020
        // distance_squared_to returns float (or int for Vector2i/Vector3i)
        var code = @"
extends Node2D

func test(pos: Vector2):
    var d = pos.distance_squared_to(global_position)
    if d < 100:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"Method call returning numeric should not trigger GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void VarWithMethodCallInitializer_UntypedCaller_NoGD3020()
    {
        // Even with untyped caller, if the method returns numeric, no GD3020
        var code = @"
func test(position):
    var nearest_dist = position.distance_squared_to(Vector2.ZERO)
    var d = position.distance_squared_to(Vector2.ONE)
    if d < nearest_dist:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull),
            $"Numeric method on untyped caller should not trigger GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Equality Operators Allow Null

    [TestMethod]
    public void EqualityWithNull_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x == null:
        pass
    if x != null:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"== and != should allow null. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void EqualityWithNull_NullLiteral_NoDiagnostic()
    {
        var code = @"
func test():
    if null == null:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"null == null should be valid. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void NotEqualityWithNull_NoDiagnostic()
    {
        var code = @"
func test():
    var x = null
    if x != 5:
        pass
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ComparisonWithNull),
            $"!= should allow comparing null with any type. Found: {FormatDiagnostics(diagnostics)}");
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

        // Use GDCompositeRuntimeProvider with GDGodotTypesProvider to get full Godot type info
        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true,
            CheckComparisonOperators = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterComparisonDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.ComparisonWithNull ||
            d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull ||
            d.Code == GDDiagnosticCode.IncompatibleComparisonTypes).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
