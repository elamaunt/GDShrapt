using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level2_Indexers;

/// <summary>
/// Level 2: Indexer validation tests.
/// Tests validate that indexer key types are checked correctly.
/// </summary>
[TestClass]
public class IndexerValidationTests
{
    #region Valid Indexers - No Diagnostics Expected

    [TestMethod]
    public void Array_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var x = arr[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Array with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void TypedArray_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array[int] = [1, 2, 3]
    var x = arr[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Typed Array with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_AnyKey_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary = {""key"": 1}
    var x = dict[""key""]
    var y = dict[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Dictionary should accept any key type. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void TypedDictionary_MatchingKey_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary[String, int] = {""key"": 1}
    var x = dict[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Typed Dictionary with matching key type should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void String_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello""
    var c = s[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"String with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void PackedInt32Array_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: PackedInt32Array = PackedInt32Array([1, 2, 3])
    var x = arr[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"PackedInt32Array with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void PackedStringArray_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: PackedStringArray = PackedStringArray([""a"", ""b""])
    var x = arr[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"PackedStringArray with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void PackedVector2Array_IntKey_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: PackedVector2Array = PackedVector2Array()
    var x = arr[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"PackedVector2Array with int key should be valid. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void Variant_AnyIndexer_NoDiagnostic()
    {
        var code = @"
func test():
    var x = get_something()
    var y = x[0]
    var z = x[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Variant should accept any indexer. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    #endregion

    #region Invalid Indexers - Key Type Mismatch

    [TestMethod]
    public void Array_StringKey_ReportsKeyTypeMismatch()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var x = arr[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Expected IndexerKeyTypeMismatch for Array with String key. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void TypedArray_StringKey_ReportsKeyTypeMismatch()
    {
        var code = @"
func test():
    var arr: Array[int] = [1, 2, 3]
    var x = arr[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Expected IndexerKeyTypeMismatch for typed Array with String key. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void String_StringKey_ReportsKeyTypeMismatch()
    {
        var code = @"
func test():
    var s: String = ""hello""
    var c = s[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Expected IndexerKeyTypeMismatch for String with String key. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void PackedByteArray_StringKey_ReportsKeyTypeMismatch()
    {
        var code = @"
func test():
    var arr: PackedByteArray = PackedByteArray()
    var x = arr[""key""]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Expected IndexerKeyTypeMismatch for PackedByteArray with String key. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void TypedDictionary_WrongKeyType_ReportsKeyTypeMismatch()
    {
        var code = @"
func test():
    var dict: Dictionary[String, int] = {""key"": 1}
    var x = dict[42]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Expected IndexerKeyTypeMismatch for typed Dictionary with wrong key type. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    #endregion

    #region Not Indexable Types

    [TestMethod]
    public void Int_Indexed_ReportsNotIndexable()
    {
        var code = @"
func test():
    var x: int = 42
    var y = x[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.NotIndexable),
            $"Expected NotIndexable for int type. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void Float_Indexed_ReportsNotIndexable()
    {
        var code = @"
func test():
    var x: float = 3.14
    var y = x[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.NotIndexable),
            $"Expected NotIndexable for float type. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void Bool_Indexed_ReportsNotIndexable()
    {
        var code = @"
func test():
    var x: bool = true
    var y = x[0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.NotIndexable),
            $"Expected NotIndexable for bool type. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    #endregion

    #region Float to Int Coercion

    [TestMethod]
    public void Array_FloatKey_NoDiagnostic()
    {
        // Float is auto-converted to int for array indexing in GDScript
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var x = arr[1.0]
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);
        Assert.AreEqual(0, indexerDiagnostics.Count,
            $"Float key should be accepted (auto-converted to int). Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    #endregion

    #region For-Loop Iterator Type Inference (GD3013 False Positive Fix)

    [TestMethod]
    public void ForLoop_IteratingArrayOfDictionary_ShouldNotReportGD3013()
    {
        // This was a false positive: call_info["method"] reported as
        // "Type 'Array' expects integer index, got 'String'"
        // because call_info (the iterator) was incorrectly typed as Array instead of Dictionary
        var code = @"
extends Node

var _pending_calls = []

func add_call():
    _pending_calls.append({""method"": ""test"", ""args"": []})

func build():
    for call_info in _pending_calls:
        if call_info.has(""method""):
            print(call_info[""method""])
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        // Should NOT report GD3013 (IndexerKeyTypeMismatch) for call_info["method"]
        // because call_info should be inferred as Variant (element of untyped array)
        // and Variant accepts any key
        Assert.IsFalse(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch &&
            d.Message.Contains("Array")),
            $"Should NOT report IndexerKeyTypeMismatch for Dictionary iteration. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_IteratingTypedArrayOfDictionary_ShouldNotReportGD3013()
    {
        // Explicit typed array of Dictionary
        var code = @"
extends Node

var _pending_calls: Array[Dictionary] = []

func build():
    for call_info in _pending_calls:
        print(call_info[""method""])
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        Assert.IsFalse(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch),
            $"Should NOT report IndexerKeyTypeMismatch for typed Array[Dictionary]. Found: {FormatDiagnostics(indexerDiagnostics)}");
    }

    [TestMethod]
    public void ForLoop_IteratingTypedArrayOfInt_ShouldReportGD3013()
    {
        // This SHOULD report an error - array of int, string key doesn't make sense
        var code = @"
extends Node

func test():
    var items: Array[int] = [1, 2, 3]
    for item in items:
        print(item[""key""])
";
        var diagnostics = ValidateCode(code);
        var indexerDiagnostics = FilterIndexerDiagnostics(diagnostics);

        // Should report NotIndexable for int (int doesn't support indexing)
        Assert.IsTrue(indexerDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.NotIndexable),
            $"Should report NotIndexable for int. Found: {FormatDiagnostics(indexerDiagnostics)}");
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

    private static List<GDDiagnostic> FilterIndexerDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IndexerKeyTypeMismatch ||
            d.Code == GDDiagnosticCode.NotIndexable ||
            d.Code == GDDiagnosticCode.IndexOutOfRange).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
