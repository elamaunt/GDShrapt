using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for GDArgumentTypeValidator and ArgumentTypeMismatch diagnostics.
/// Verifies that type mismatches at call sites are correctly detected.
/// </summary>
[TestClass]
public class ArgumentTypeValidatorTests
{
    #region No Mismatch Cases

    [TestMethod]
    public void ArgumentTypeValidator_ExactTypeMatch_NoDiagnostic()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process(42)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            $"No mismatch expected for int->int. Diagnostics: {string.Join(", ", mismatchDiagnostics.Select(d => d.Message))}");
    }

    [TestMethod]
    public void ArgumentTypeValidator_NumericCompatibility_NoDiagnostic()
    {
        // Arrange - int is compatible with float
        var code = @"
func process(x: float):
    pass

func test():
    process(42)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "int->float should be compatible");
    }

    [TestMethod]
    public void ArgumentTypeValidator_NullArgument_NoDiagnostic()
    {
        // Arrange - null is compatible with any type
        var code = @"
func process(x: String):
    pass

func test():
    process(null)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "null should be compatible with any type");
    }

    [TestMethod]
    public void ArgumentTypeValidator_VariantParameter_NoDiagnostic()
    {
        // Arrange - untyped parameter (Variant) accepts anything
        var code = @"
func process(x):
    pass

func test():
    process(42)
    process(""hello"")
    process([1, 2, 3])
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "Variant parameter should accept any type");
    }

    #endregion

    #region Type Mismatch Cases

    [TestMethod]
    public void ArgumentTypeValidator_StringToInt_ReportsMismatch()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process(""hello"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count,
            $"Expected 1 mismatch for String->int. Found: {mismatchDiagnostics.Count}");
        Assert.IsTrue(mismatchDiagnostics[0].Message.Contains("String"),
            "Message should mention String");
        Assert.IsTrue(mismatchDiagnostics[0].Message.Contains("int"),
            "Message should mention int");
    }

    [TestMethod]
    public void ArgumentTypeValidator_ArrayToInt_ReportsMismatch()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process([1, 2, 3])
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count,
            "Expected mismatch for Array->int");
    }

    [TestMethod]
    public void ArgumentTypeValidator_FloatToInt_ReportsMismatch()
    {
        // Arrange - float is NOT compatible with int (narrowing conversion)
        var code = @"
func process(x: int):
    pass

func test():
    process(3.14)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count,
            "float->int is a narrowing conversion and should be reported");
    }

    #endregion

    #region Multiple Parameters

    [TestMethod]
    public void ArgumentTypeValidator_MultipleMismatches_ReportsAll()
    {
        // Arrange
        var code = @"
func process(a: int, b: String, c: float):
    pass

func test():
    process(""wrong"", 42, ""also wrong"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(3, mismatchDiagnostics.Count,
            $"Expected 3 mismatches. Found: {mismatchDiagnostics.Count}");
    }

    [TestMethod]
    public void ArgumentTypeValidator_PartialMismatch_ReportsOnlyMismatched()
    {
        // Arrange
        var code = @"
func process(a: int, b: String, c: float):
    pass

func test():
    process(42, ""correct"", 3.14)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(0, mismatchDiagnostics.Count,
            "No mismatches expected for correct types");
    }

    #endregion

    #region Union Type Parameters

    [TestMethod]
    public void ArgumentTypeValidator_ParameterWithTypeGuard_AcceptsMatchingTypes()
    {
        // Arrange - parameter has type guards, should accept those types
        var code = @"
func process(x):
    if x is int:
        return x * 2
    if x is String:
        return x.length()

func test():
    process(42)
    process(""hello"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(0, mismatchDiagnostics.Count,
            $"No mismatch expected for types matching type guards. Found: {mismatchDiagnostics.Count}");
    }

    #endregion

    #region Self Method Calls

    [TestMethod]
    public void ArgumentTypeValidator_SelfMethodCall_ValidatesTypes()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    self.process(""hello"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count,
            "self.method() calls should also be validated");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ArgumentTypeValidator_NoArguments_NoDiagnostic()
    {
        // Arrange
        var code = @"
func process():
    pass

func test():
    process()
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count());
    }

    [TestMethod]
    public void ArgumentTypeValidator_MoreArgsThanParams_SkipsExtra()
    {
        // Arrange - only first arg should be validated
        var code = @"
func process(x: int):
    pass

func test():
    process(42, ""extra"", [1, 2])
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "Extra args beyond parameter count should not cause type mismatch");
    }

    [TestMethod]
    public void ArgumentTypeValidator_FewerArgsThanParams_ValidatesProvided()
    {
        // Arrange - only first arg should be validated
        var code = @"
func process(x: int, y: String):
    pass

func test():
    process(42)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "Missing args should not cause type mismatch (handled by argument count validator)");
    }

    [TestMethod]
    public void ArgumentTypeValidator_UnknownFunction_NoDiagnostic()
    {
        // Arrange - unknown function (not defined) should not cause error
        var code = @"
func test():
    unknown_function(""hello"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "Unknown functions should not be validated for type mismatches");
    }

    #endregion

    #region Severity Options

    [TestMethod]
    public void ArgumentTypeValidator_WithErrorSeverity_ReportsAsError()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process(""hello"")
";
        // Act
        var options = new GDSemanticValidatorOptions { ArgumentTypeSeverity = GDDiagnosticSeverity.Error };
        var diagnostics = ValidateCode(code, options);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count);
        Assert.AreEqual(GDDiagnosticSeverity.Error, mismatchDiagnostics[0].Severity);
    }

    [TestMethod]
    public void ArgumentTypeValidator_WithHintSeverity_ReportsAsHint()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process(""hello"")
";
        // Act
        var options = new GDSemanticValidatorOptions { ArgumentTypeSeverity = GDDiagnosticSeverity.Hint };
        var diagnostics = ValidateCode(code, options);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(1, mismatchDiagnostics.Count);
        Assert.AreEqual(GDDiagnosticSeverity.Hint, mismatchDiagnostics[0].Severity);
    }

    [TestMethod]
    public void ArgumentTypeValidator_Disabled_NoDiagnostics()
    {
        // Arrange
        var code = @"
func process(x: int):
    pass

func test():
    process(""hello"")
";
        // Act
        var options = new GDSemanticValidatorOptions { CheckArgumentTypes = false };
        var diagnostics = ValidateCode(code, options);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
        Assert.AreEqual(0, mismatchDiagnostics.Count(),
            "Disabled validator should not report diagnostics");
    }

    #endregion

    #region P4: Variant Parameter Accepts Any Type

    [TestMethod]
    public void P4_VariantParameter_AcceptsInt()
    {
        // P4: Variant parameter with null default should accept int
        var code = @"
func get_item_property(key: String, default_val: Variant = null):
    return default_val

func test():
    get_item_property(""key"", 42)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(0, mismatchDiagnostics.Count,
            $"Variant parameter should accept int. Found: {string.Join(", ", mismatchDiagnostics.Select(d => d.Message))}");
    }

    [TestMethod]
    public void P4_VariantParameter_AcceptsString()
    {
        // P4: Variant parameter with null default should accept String
        var code = @"
func get_item_property(key: String, default_val: Variant = null):
    return default_val

func test():
    get_item_property(""key"", ""default_string"")
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(0, mismatchDiagnostics.Count,
            "Variant parameter should accept String");
    }

    [TestMethod]
    public void P4_VariantParameter_AcceptsNull()
    {
        // P4: Variant parameter should accept null explicitly
        var code = @"
func get_item_property(key: String, default_val: Variant = null):
    return default_val

func test():
    get_item_property(""key"", null)
";
        // Act
        var diagnostics = ValidateCode(code);

        // Assert
        var mismatchDiagnostics = diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
        Assert.AreEqual(0, mismatchDiagnostics.Count,
            "Variant parameter should accept null");
    }

    #endregion

    #region Helper Methods

    private static System.Collections.Generic.IEnumerable<GDDiagnostic> ValidateCode(
        string code,
        GDSemanticValidatorOptions? options = null)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    #endregion
}
