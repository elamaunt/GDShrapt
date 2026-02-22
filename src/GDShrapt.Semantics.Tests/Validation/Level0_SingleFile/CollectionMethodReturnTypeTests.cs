using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

[TestClass]
public class CollectionMethodReturnTypeTests
{
    #region Group 1: filter() preserves type (baseline)

    [TestMethod]
    public void ForLoop_OverFilteredArray_MatchingType_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for x: int in arr.filter(func(n): return n > 0):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"int variable over filtered Array[int] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_OverFilteredArray_TypeMismatch()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for x: String in arr.filter(func(n): return n > 0):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.IsTrue(forLoopDiagnostics.Count > 0,
            "Expected type mismatch: String variable iterating over filtered Array[int]");
    }

    #endregion

    #region Group 2: map() changes type (new behavior)

    [TestMethod]
    public void ForLoop_OverMappedArray_CorrectType_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var mapped = arr.map(func(x): return str(x))
    for s: String in mapped:
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"String variable over mapped Array[int]->Array[String] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_OverMappedArray_WrongType_ReportsMismatch()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var mapped = arr.map(func(x): return str(x))
    for n: int in mapped:
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.IsTrue(forLoopDiagnostics.Count > 0,
            "Expected type mismatch: int variable iterating over mapped Array[String]");
    }

    [TestMethod]
    public void ArrayMap_IntToFloat_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for f: float in arr.map(func(x): return float(x)):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"float variable over mapped Array[int]->Array[float] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    #endregion

    #region Group 3: Chained operations

    [TestMethod]
    public void FilterThenMap_ForLoop_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    for s: String in arr.filter(func(x): return x > 2).map(func(y): return str(y)):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"String variable over filter().map() chain should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void MapThenFilter_ForLoop_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(func(x): return str(x)).filter(func(y): return y.length() > 1):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"String variable over map().filter() chain should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    #endregion

    #region Group 4: Edge cases

    [TestMethod]
    public void ArrayMap_UntypedForLoopVar_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for x in arr.map(func(v): return v):
        pass
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Untyped for-loop variable over mapped array should not produce diagnostics. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMap_MethodReference_InfersReturnType_NoDiagnostic()
    {
        var code = @"
extends Node

func my_transform(x) -> String:
    return str(x)

func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(my_transform):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Method reference with -> String return in map() should produce Array[String]. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMap_MethodReference_WrongType_ReportsMismatch()
    {
        var code = @"
extends Node

func my_transform(x) -> String:
    return str(x)

func test():
    var arr: Array[int] = [1, 2, 3]
    for n: int in arr.map(my_transform):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.IsTrue(forLoopDiagnostics.Count > 0,
            "Expected type mismatch: int variable iterating over map(-> String) result");
    }

    [TestMethod]
    public void ArrayMap_MethodReference_NoAnnotation_InfersFromBody()
    {
        var code = @"
extends Node

func my_transform(x):
    return str(x)

func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(my_transform):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Method without -> annotation but returning str() should infer String. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMap_MethodReference_VariantAnnotation_InfersNarrowerType()
    {
        var code = @"
extends Node

func my_transform(x) -> Variant:
    return str(x)

func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(my_transform):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Method with -> Variant but body returning str() should infer String from body. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMap_ExplicitReturnType_InfersCorrectly()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(func(x) -> String: return str(x)):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Lambda with explicit -> String return type should produce Array[String]. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void ArraySlice_PreservesType_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    for x: int in arr.slice(0, 2):
        pass
";
        var diagnostics = ValidateCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"int variable over sliced Array[int] should be compatible. Found: {FormatDiagnostics(forLoopDiagnostics)}");
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

    private static List<GDDiagnostic> FilterForLoopDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return FilterTypeDiagnostics(diagnostics)
            .Where(d => d.Message.Contains("for-loop"))
            .ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
