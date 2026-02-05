using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for merge methods that combine container types.
/// Methods like append_array() and merge() should update the container type
/// to reflect the union of element types.
/// </summary>
[TestClass]
public class MergeMethodTests
{
    #region Array.append_array() - Same Type Tests

    [TestMethod]
    public void ArrayAppendArray_SameType_KeepsType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2]
    arr.append_array([3, 4])
    var elem: int = arr[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"append_array with same type should keep Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAppendArray_InferredSameType_KeepsType()
    {
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3]
    arr.append_array([4, 5, 6])
    var elem: int = arr[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Inferred int array + int array should stay int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Array.append_array() - Union Type Tests

    [TestMethod]
    public void ArrayAppendArray_DifferentTypes_CreatesUnion()
    {
        // After appending float array to int array, type should be int|float
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3]
    arr.append_array([1.0, 2.0])
    # arr is now Array[int|float] - both int and float operations should work
    var elem = arr[0]
";
        var diagnostics = ValidateCode(code);
        // This test verifies no errors - union type should be valid
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Union type int|float should be valid after append_array. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.merge() - Same Type Tests

    [TestMethod]
    public void DictMerge_SameTypes_KeepsType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    dict.merge({""a"": 1})
    var val: int = dict[""a""]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"merge with same types should keep Dictionary[String,int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictMerge_InferredSameType_KeepsType()
    {
        var code = @"
extends Node
func test():
    var dict = {""a"": 1}
    dict.merge({""b"": 2})
    var val: int = dict[""a""]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Inferred dict merge with same types should work. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.merged() Tests

    [TestMethod]
    public void DictMerged_ReturnsNewDict_PreservesType()
    {
        var code = @"
extends Node
func test():
    var dict1: Dictionary[String, int] = {""a"": 1}
    var dict2: Dictionary[String, int] = {""b"": 2}
    var combined: Dictionary[String, int] = dict1.merged(dict2)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"merged() should return Dictionary[String,int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictMerged_ChainedAccess_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var dict1: Dictionary[String, int] = {""a"": 1}
    var dict2: Dictionary[String, int] = {""b"": 2}
    var val: int = dict1.merged(dict2)[""a""]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"merged()[key] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Complex Merge Scenarios

    [TestMethod]
    public void ArrayAppendArray_AfterFilter_KeepsType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3, 4, 5]
    var filtered = arr.filter(func(x): return x > 2)
    filtered.append_array([10, 20])
    var elem: int = filtered[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"filter result + append_array should keep int type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictMerge_WithOverwrite_KeepsType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {""a"": 1}
    dict.merge({""a"": 2}, true)
    var val: int = dict[""a""]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"merge with overwrite should keep type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Multiple Appends/Merges

    [TestMethod]
    public void ArrayAppendArray_MultipleTimes_KeepsType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = []
    arr.append_array([1, 2])
    arr.append_array([3, 4])
    arr.append_array([5, 6])
    var elem: int = arr[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Multiple append_array should keep Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictMerge_MultipleTimes_KeepsType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    dict.merge({""a"": 1})
    dict.merge({""b"": 2})
    dict.merge({""c"": 3})
    var val: int = dict[""a""]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Multiple merge should keep Dictionary[String,int]. Found: {FormatDiagnostics(typeDiagnostics)}");
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

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
