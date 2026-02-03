using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for lambda parameter type inference from container methods.
/// Validates that methods like filter(), map(), reduce() correctly infer
/// lambda parameter types from the container's element type.
/// </summary>
[TestClass]
public class LambdaParameterInferenceTests
{
    #region Array.filter() Tests

    [TestMethod]
    public void ArrayFilter_TypedArray_InfersIntFromElements()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var filtered = arr.filter(func(x): return x > 2)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int from Array[int]. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void ArrayFilter_InferredArray_InfersIntFromLiteralElements()
    {
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3, 4, 5]
    var filtered = arr.filter(func(x): return x > 2)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int from literal [1,2,3,4,5]. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void ArrayFilter_TypedArrayString_InfersStringFromElements()
    {
        var code = @"
extends Node
func test():
    var names: Array[String] = [""alice"", ""bob""]
    var long_names = names.filter(func(name): return name.length() > 3)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"name should be String, allowing .length() call. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Array.map() Tests

    [TestMethod]
    public void ArrayMap_TypedArray_InfersIntFromElements()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var doubled = arr.map(func(x): return x * 2)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMap_InferredArray_InfersIntFromLiteralElements()
    {
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3]
    var doubled = arr.map(func(x): return x * 2)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int from literal array. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Array.reduce() Tests

    [TestMethod]
    public void ArrayReduce_TypedArray_InfersAccumulatorAndElement()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var sum = arr.reduce(func(acc, x): return acc + x, 0)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be int, acc should be Variant. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Array.any() and Array.all() Tests

    [TestMethod]
    public void ArrayAny_TypedArray_InfersIntFromElements()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var has_positive = arr.any(func(x): return x > 0)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAll_TypedArray_InfersIntFromElements()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var all_positive = arr.all(func(x): return x > 0)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"x should be inferred as int. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Array.sort_custom() Tests

    [TestMethod]
    public void ArraySortCustom_TypedArray_InfersBothParamsFromElements()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [3, 1, 2]
    arr.sort_custom(func(a, b): return a < b)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Both a and b should be inferred as int. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Complex Scenarios

    [TestMethod]
    public void ArrayFilter_ChainedWithMap_InfersCorrectTypes()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var result = arr.filter(func(x): return x > 2).map(func(y): return y * 2)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Both x and y should be inferred as int in chained calls. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void ArrayFilter_WithMethodCallOnElement_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var nodes: Array[Node2D] = []
    var visible = nodes.filter(func(n): return n.visible)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"n should be Node2D, allowing .visible access. Found: {FormatDiagnostics(unguardedDiagnostics)}");
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

    private static List<GDDiagnostic> FilterUnguardedDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedPropertyAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
