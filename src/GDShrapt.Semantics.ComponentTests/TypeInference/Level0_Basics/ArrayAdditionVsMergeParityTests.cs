using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level0;

/// <summary>
/// Tests for parity between array + operator and append_array/merge methods.
/// Both should produce identical type inference results.
/// </summary>
[TestClass]
public class ArrayAdditionVsMergeParityTests
{
    #region Same Type - Array + vs append_array

    [TestMethod]
    public void Parity_AdditionVsAppendArray_SameType_IdenticalResult()
    {
        // Addition
        var additionCode = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    var b: Array[int] = [3, 4]
    var c = a + b
    var elem: int = c[0]
";
        // append_array
        var appendCode = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    a.append_array([3, 4])
    var elem: int = a[0]
";
        AssertBothHaveZeroTypeDiagnostics(additionCode, appendCode,
            "Addition and append_array with same type should both work");
    }

    [TestMethod]
    public void Parity_AdditionVsAppendArray_InferredType_IdenticalResult()
    {
        // Addition
        var additionCode = @"
extends Node
func test():
    var a = [1, 2]
    var b = [3, 4]
    var c = a + b
    var elem: int = c[0]
";
        // append_array
        var appendCode = @"
extends Node
func test():
    var a = [1, 2]
    a.append_array([3, 4])
    var elem: int = a[0]
";
        AssertBothHaveZeroTypeDiagnostics(additionCode, appendCode,
            "Addition and append_array with inferred type should both work");
    }

    #endregion

    #region Empty Array Cases

    [TestMethod]
    public void Parity_AdditionVsAppendArray_EmptyArray_IdenticalResult()
    {
        var additionCode = @"
extends Node
func test():
    var a = [1, 2]
    var b: Array = []
    var c = a + b
    var elem: int = c[0]
";
        var appendCode = @"
extends Node
func test():
    var a = [1, 2]
    a.append_array([])
    var elem: int = a[0]
";
        AssertBothHaveZeroTypeDiagnostics(additionCode, appendCode,
            "Addition and append_array with empty array should both work");
    }

    #endregion

    #region Dictionary merge vs merged

    [TestMethod]
    public void Parity_DictMergeVsMerged_SameType_IdenticalResult()
    {
        // merge() mutates
        var mutateCode = @"
extends Node
func test():
    var a: Dictionary[String, int] = {""a"": 1}
    a.merge({""b"": 2})
    var val: int = a[""a""]
";
        // merged() returns new
        var newDictCode = @"
extends Node
func test():
    var a: Dictionary[String, int] = {""a"": 1}
    var b = a.merged({""b"": 2})
    var val: int = b[""a""]
";
        AssertBothHaveZeroTypeDiagnostics(mutateCode, newDictCode,
            "merge() and merged() should both preserve type");
    }

    [TestMethod]
    public void Parity_DictMerged_ChainedAccess_PreservesType()
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

    #region Type-Preserving Operations

    [TestMethod]
    public void Addition_TypedPlusInferred_WorksCorrectly()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    var b = [3, 4]
    var c = a + b
    var elem: int = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Typed + inferred should work. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void AppendArray_AfterFilter_PreservesType()
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
            $"filter result + append_array should keep type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static void AssertBothHaveZeroTypeDiagnostics(string code1, string code2, string message)
    {
        var diag1 = FilterTypeMismatchDiagnostics(ValidateCode(code1));
        var diag2 = FilterTypeMismatchDiagnostics(ValidateCode(code2));

        Assert.AreEqual(0, diag1.Count,
            $"{message} (first code). Found: {FormatDiagnostics(diag1)}");
        Assert.AreEqual(0, diag2.Count,
            $"{message} (second code). Found: {FormatDiagnostics(diag2)}");
    }

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
            CheckArgumentTypes = true,
            CheckIndexers = true
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
