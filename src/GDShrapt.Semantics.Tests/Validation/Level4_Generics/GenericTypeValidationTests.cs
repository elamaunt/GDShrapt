using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level4_Generics;

/// <summary>
/// Level 4: Generic type validation tests.
/// Tests validate that generic type parameters are valid.
/// </summary>
[TestClass]
public class GenericTypeValidationTests
{
    #region Valid Generic Types - No Diagnostics Expected

    [TestMethod]
    public void Array_BuiltInType_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array[int] = []
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Array[int] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Array_StringType_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array[String] = []
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Array[String] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Array_NodeType_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array[Node] = []
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Array[Node] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_IntStringKey_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary[int, String] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Dictionary[int, String] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_StringInt_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary[String, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Dictionary[String, int] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_Vector2Key_NoDiagnostic()
    {
        // Vector2 is hashable
        var code = @"
func test():
    var dict: Dictionary[Vector2, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Dictionary[Vector2, int] should be valid (Vector2 is hashable). Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_NodeKey_NoDiagnostic()
    {
        // Node is hashable by identity
        var code = @"
func test():
    var dict: Dictionary[Node, String] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Dictionary[Node, String] should be valid (Node is hashable by identity). Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    #endregion

    #region Invalid Generic Types - Unknown Type

    [TestMethod]
    public void Array_UnknownType_ReportsInvalidGenericArgument()
    {
        var code = @"
func test():
    var arr: Array[UnknownType] = []
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.InvalidGenericArgument),
            $"Expected InvalidGenericArgument for unknown type. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_UnknownKeyType_ReportsInvalidGenericArgument()
    {
        var code = @"
func test():
    var dict: Dictionary[UnknownType, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.InvalidGenericArgument),
            $"Expected InvalidGenericArgument for unknown key type. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_UnknownValueType_ReportsInvalidGenericArgument()
    {
        var code = @"
func test():
    var dict: Dictionary[String, UnknownType] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.InvalidGenericArgument),
            $"Expected InvalidGenericArgument for unknown value type. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    #endregion

    #region Invalid Generic Types - Non-Hashable Keys

    [TestMethod]
    public void Dictionary_ArrayKey_ReportsNotHashable()
    {
        var code = @"
func test():
    var dict: Dictionary[Array, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.DictionaryKeyNotHashable),
            $"Expected DictionaryKeyNotHashable for Array key. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_DictionaryKey_ReportsNotHashable()
    {
        var code = @"
func test():
    var dict: Dictionary[Dictionary, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.DictionaryKeyNotHashable),
            $"Expected DictionaryKeyNotHashable for Dictionary key. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void Dictionary_PackedArrayKey_ReportsNotHashable()
    {
        var code = @"
func test():
    var dict: Dictionary[PackedByteArray, int] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);

        Assert.IsTrue(genericDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.DictionaryKeyNotHashable),
            $"Expected DictionaryKeyNotHashable for PackedByteArray key. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    #endregion

    #region Class-Level Variable Declarations

    [TestMethod]
    public void ClassVariable_ArrayInt_NoDiagnostic()
    {
        var code = @"
var scores: Array[int] = []
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Class variable with Array[int] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
    }

    [TestMethod]
    public void ClassVariable_DictionaryStringNode_NoDiagnostic()
    {
        var code = @"
var nodes: Dictionary[String, Node] = {}
";
        var diagnostics = ValidateCode(code);
        var genericDiagnostics = FilterGenericDiagnostics(diagnostics);
        Assert.AreEqual(0, genericDiagnostics.Count,
            $"Class variable with Dictionary[String, Node] should be valid. Found: {FormatDiagnostics(genericDiagnostics)}");
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
            CheckArgumentTypes = true,
            CheckIndexers = true,
            CheckSignalTypes = true,
            CheckGenericTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterGenericDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.WrongGenericParameterCount ||
            d.Code == GDDiagnosticCode.InvalidGenericArgument ||
            d.Code == GDDiagnosticCode.DictionaryKeyNotHashable).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
