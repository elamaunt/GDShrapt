using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for subset/duplication methods that should preserve container types.
/// Methods like slice(), duplicate() should return the same typed container.
/// </summary>
[TestClass]
public class SubsetMethodTests
{
    #region Array.slice() Tests

    [TestMethod]
    public void ArraySlice_TypedArray_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var sliced: Array[int] = arr.slice(1, 3)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"slice() on Array[int] should return Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArraySlice_InferredArray_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3, 4, 5]
    var sliced = arr.slice(1, 3)
    var elem: int = sliced[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"slice() on inferred Array[int] should preserve element type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Array.duplicate() Tests

    [TestMethod]
    public void ArrayDuplicate_TypedArray_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[String] = [""a"", ""b""]
    var copy: Array[String] = arr.duplicate()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"duplicate() on Array[String] should return Array[String]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayDuplicate_DeepCopy_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var copy: Array[int] = arr.duplicate(true)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"duplicate(true) on Array[int] should return Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Array.filter() Returns Self Type Tests

    [TestMethod]
    public void ArrayFilter_TypedArray_ReturnsSelfType()
    {
        // filter() returns the same array type
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var filtered: Array[int] = arr.filter(func(x): return x > 2)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"filter() on Array[int] should return Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.duplicate() Tests

    [TestMethod]
    public void DictDuplicate_TypedDict_PreservesType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    var copy: Dictionary[String, int] = dict.duplicate()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"duplicate() on Dictionary[String,int] should return Dictionary[String,int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictDuplicate_DeepCopy_PreservesType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, Array[int]] = {}
    var copy: Dictionary[String, Array[int]] = dict.duplicate(true)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"duplicate(true) on nested Dictionary should preserve full type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Chained Subset Operations Tests

    [TestMethod]
    public void ArraySlice_ThenFilter_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var result: Array[int] = arr.slice(0, 3).filter(func(x): return x > 1)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Chained slice().filter() should preserve Array[int] type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayDuplicate_ThenSlice_PreservesType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[String] = [""a"", ""b"", ""c""]
    var result: Array[String] = arr.duplicate().slice(0, 2)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Chained duplicate().slice() should preserve Array[String] type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Element Access After Subset Tests

    [TestMethod]
    public void ArraySlice_ThenFront_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var first: int = arr.slice(2, 4).front()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"slice().front() should return int for Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayDuplicate_ThenIndexAccess_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[Node2D] = []
    var pos = arr.duplicate()[0].position
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"duplicate()[0] should return Node2D, allowing .position access. Found: {FormatDiagnostics(unguardedDiagnostics)}");
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

    private static List<GDDiagnostic> FilterTypeMismatchDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment).ToList();
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
