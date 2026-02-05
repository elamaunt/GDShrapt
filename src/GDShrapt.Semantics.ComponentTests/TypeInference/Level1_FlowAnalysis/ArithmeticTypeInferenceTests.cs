using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level1;

/// <summary>
/// Tests for arithmetic type inference with typed parameters.
/// Verifies that int + int = int, not Variant.
/// </summary>
[TestClass]
public class ArithmeticTypeInferenceTests
{
    #region P5, P6: Arithmetic with Typed Parameters

    [TestMethod]
    public void P5_P6_IntArithmetic_PreservesIntType()
    {
        // P5: x:int + y:int + z:int should result in int, not Variant
        // P6: return result should not cause GD3007
        var code = @"
func bad_spacing(x:int, y:int, z:int) -> int:
    var result = x + y + z
    if result > 10:
        return result * 2
    else:
        return result
";
        var diagnostics = ValidateCode(code);

        // P5: No GD3002 (InvalidOperandType) for int arithmetic
        var operandDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
        Assert.AreEqual(0, operandDiagnostics.Count,
            $"int + int should be valid. Found: {FormatDiagnostics(operandDiagnostics)}");

        // P6: No GD3007 (IncompatibleReturnType) - return type should be int
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"Return int from int function should be valid. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P5_SimpleIntAddition_PreservesInt()
    {
        var code = @"
func test(a: int, b: int) -> int:
    return a + b
";
        var diagnostics = ValidateCode(code);

        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"a:int + b:int should be int. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P5_IntMultiplication_PreservesInt()
    {
        var code = @"
func test(x: int) -> int:
    return x * 2
";
        var diagnostics = ValidateCode(code);

        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"x:int * 2 should be int. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P5_ChainedIntArithmetic_PreservesInt()
    {
        var code = @"
func test(a: int, b: int, c: int, d: int) -> int:
    return a + b + c + d
";
        var diagnostics = ValidateCode(code);

        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"Chained int addition should be int. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P5_IntFloatMixedArithmetic_ReturnsFloat()
    {
        var code = @"
func test(a: int, b: float) -> float:
    return a + b
";
        var diagnostics = ValidateCode(code);

        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"int + float should be float. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P5_VariableAssignmentFromArithmetic_PreservesType()
    {
        var code = @"
func test(x: int, y: int) -> int:
    var result = x + y
    return result
";
        var diagnostics = ValidateCode(code);

        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, returnDiagnostics.Count,
            $"result assigned from int+int should be int. Found: {FormatDiagnostics(returnDiagnostics)}");
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
