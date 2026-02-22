using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

[TestClass]
public class ForLoopTypeValidationTests
{
    #region Correct For-Loops - No Diagnostics Expected

    [TestMethod]
    public void ForLoop_UntypedVariable_NoDiagnostic()
    {
        var code = @"
func test():
    for x in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Untyped for-loop variable should not produce diagnostics. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_IntVariableOverRange_NoDiagnostic()
    {
        var code = @"
func test():
    for x: int in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"int variable over range() should be compatible. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_StringVariableOverString_NoDiagnostic()
    {
        var code = @"
func test():
    for ch: String in ""hello"":
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"String variable over String should be compatible. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_TypedArrayMatchingElement_NoDiagnostic()
    {
        var code = @"
var arr: Array[int] = [1, 2, 3]

func test():
    for x: int in arr:
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterTypeDiagnostics(diagnostics)
            .Where(d => d.Message.Contains("for-loop"))
            .ToList();
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"int variable over Array[int] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_NodeOverTypedArray_NoDiagnostic()
    {
        var code = @"
var nodes: Array[Node] = []

func test():
    for n: Node in nodes:
        pass
";
        var diagnostics = ValidateCode(code);
        // Filter only for-loop related diagnostics (skip pre-existing GD3004 on class variable)
        var forLoopDiagnostics = FilterTypeDiagnostics(diagnostics)
            .Where(d => d.Message.Contains("for-loop"))
            .ToList();
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Node variable over Array[Node] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_FloatOverIntRange_NoDiagnostic()
    {
        var code = @"
func test():
    for x: float in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"float variable over range() (int->float widening) should be compatible. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Incorrect For-Loops - Diagnostics Expected

    [TestMethod]
    public void ForLoop_StringVariableOverRange_ReportsMismatch()
    {
        var code = @"
func test():
    for x: String in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch: String variable iterating over range() (int elements)");
        Assert.IsTrue(typeDiagnostics.Any(d =>
            d.Message.Contains("int") && d.Message.Contains("String")),
            $"Message should mention int and String. Got: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_IntVariableOverString_ReportsMismatch()
    {
        var code = @"
func test():
    for x: int in ""hello"":
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch: int variable iterating over String");
    }

    [TestMethod]
    public void ForLoop_StringVariableOverIntArray_ReportsMismatch()
    {
        var code = @"
var arr: Array[int] = [1, 2, 3]

func test():
    for x: String in arr:
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterTypeDiagnostics(diagnostics)
            .Where(d => d.Message.Contains("for-loop"))
            .ToList();

        Assert.IsTrue(forLoopDiagnostics.Count > 0,
            "Expected type mismatch: String variable iterating over Array[int]");
    }

    [TestMethod]
    public void ForLoop_BoolVariableOverRange_ReportsMismatch()
    {
        var code = @"
func test():
    for x: bool in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch: bool variable iterating over range() (int elements)");
    }

    [TestMethod]
    public void ForLoop_VariantVariableOverRange_ReportsWidening()
    {
        var code = @"
func test():
    for x: Variant in range(10):
        pass
";
        var diagnostics = ValidateCode(code);
        var wideningDiagnostics = diagnostics
            .Where(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred)
            .ToList();

        Assert.IsTrue(wideningDiagnostics.Count > 0,
            $"Expected GD3022: Variant annotation over range() when int is known. All: {FormatDiagnostics(diagnostics)}");
        Assert.IsTrue(wideningDiagnostics.Any(d =>
            d.Message.Contains("Variant") && d.Message.Contains("int")),
            $"Message should mention Variant and int. Got: {FormatDiagnostics(wideningDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_VariantVariableOverTypedArray_ReportsWidening()
    {
        var code = @"
var arr: Array[String] = []

func test():
    for x: Variant in arr:
        pass
";
        var diagnostics = ValidateCode(code);
        var wideningDiagnostics = diagnostics
            .Where(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred)
            .ToList();

        Assert.IsTrue(wideningDiagnostics.Count > 0,
            $"Expected GD3022: Variant annotation over Array[String] when String is known. All: {FormatDiagnostics(diagnostics)}");
        Assert.IsTrue(wideningDiagnostics.Any(d =>
            d.Message.Contains("Variant") && d.Message.Contains("String")),
            $"Message should mention Variant and String. Got: {FormatDiagnostics(wideningDiagnostics)}");
    }

    #endregion

    #region For-Loop Variable Used in Body

    [TestMethod]
    public void ForLoop_TypedVariable_UsedInBody_NoDiagnostic()
    {
        var code = @"
func test():
    var total: int = 0
    for x: int in range(10):
        total = total + x
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Typed for-loop variable used in arithmetic should not produce diagnostics. Found: {FormatDiagnostics(typeDiagnostics)}");
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

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
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

    private static List<GDDiagnostic> FilterTypeDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment ||
            d.Code == GDDiagnosticCode.TypeAnnotationMismatch ||
            d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
