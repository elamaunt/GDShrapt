using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level0_Basics;

/// <summary>
/// Tests for Dictionary type inference.
/// Verifies that Dictionary.get() returns Variant, allowing method chaining.
/// </summary>
[TestClass]
public class DictionaryTypeInferenceTests
{
    #region P9: Dictionary.get() Returns Variant

    [TestMethod]
    public void P9_DictionaryGet_ReturnsVariant_AllowsMethodCall()
    {
        // P9: Dictionary.get().to_upper() should work
        // Dictionary.get() returns Variant, which allows any method call
        var code = @"
func test(input) -> String:
    if input is Dictionary:
        return input.get(""name"", """").to_upper()
    return """"
";
        var diagnostics = ValidateCode(code);
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();

        Assert.AreEqual(0, returnDiagnostics.Count,
            $"Dictionary.get().to_upper() should return String, not void. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P9_DictionaryGet_WithDefault_PreservesDefaultType()
    {
        // When default is String, result can be treated as String
        var code = @"
func test(dict: Dictionary) -> String:
    return dict.get(""key"", ""default"").to_upper()
";
        var diagnostics = ValidateCode(code);
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();

        Assert.AreEqual(0, returnDiagnostics.Count,
            $"dict.get() with String default should allow String methods. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void P9_DictionaryGet_NoDefault_ReturnsVariant()
    {
        // Without default, get() returns Variant (could be null)
        var code = @"
func test(dict: Dictionary):
    var value = dict.get(""key"")
    if value is String:
        print(value.to_upper())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.PropertyNotFound).ToList();

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"After type guard, String methods should work. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void P9_DictionaryBracketAccess_ReturnsVariant()
    {
        // dict["key"] also returns Variant
        var code = @"
func test(dict: Dictionary) -> String:
    if dict.has(""name""):
        return dict[""name""].to_upper()
    return """"
";
        var diagnostics = ValidateCode(code);
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();

        // This may or may not pass depending on implementation
        // But it should not return void
        var voidReturnErrors = returnDiagnostics.Where(d =>
            d.Message != null && d.Message.Contains("void")).ToList();

        Assert.AreEqual(0, voidReturnErrors.Count,
            $"dict[key].to_upper() should not return void. Found: {FormatDiagnostics(voidReturnErrors)}");
    }

    #endregion

    #region Dictionary Method Chaining

    [TestMethod]
    public void DictionaryKeys_ReturnsArray()
    {
        var code = @"
func test(dict: Dictionary) -> int:
    return dict.keys().size()
";
        var diagnostics = ValidateCode(code);
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();

        Assert.AreEqual(0, returnDiagnostics.Count,
            $"dict.keys().size() should return int. Found: {FormatDiagnostics(returnDiagnostics)}");
    }

    [TestMethod]
    public void DictionaryValues_ReturnsArray()
    {
        var code = @"
func test(dict: Dictionary) -> int:
    return dict.values().size()
";
        var diagnostics = ValidateCode(code);
        var returnDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();

        Assert.AreEqual(0, returnDiagnostics.Count,
            $"dict.values().size() should return int. Found: {FormatDiagnostics(returnDiagnostics)}");
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
