using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level7_DynamicCalls;

/// <summary>
/// Level 7: Dynamic call validation tests.
/// Tests validate that call(), get(), set() with known strings are checked.
/// </summary>
[TestClass]
public class DynamicCallValidationTests
{
    #region Dynamic Call - Unknown Method - Should Report

    [TestMethod]
    public void DynamicCall_UnknownMethod_ReportsDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.call(""unknown_method"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.IsTrue(dynamicDiagnostics.Any(d => d.Code == GDDiagnosticCode.DynamicMethodNotFound),
            $"Expected GD7015 for call(unknown). Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    [TestMethod]
    public void DynamicGet_UnknownProperty_ReportsDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    var x = node.get(""nonexistent_property"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.IsTrue(dynamicDiagnostics.Any(d => d.Code == GDDiagnosticCode.DynamicPropertyNotFound),
            $"Expected GD7016 for get(unknown). Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    [TestMethod]
    public void DynamicSet_UnknownProperty_ReportsDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.set(""nonexistent_property"", 42)
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.IsTrue(dynamicDiagnostics.Any(d => d.Code == GDDiagnosticCode.DynamicPropertyNotFound),
            $"Expected GD7016 for set(unknown). Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    #endregion

    #region Dynamic Call - Known Method - Should NOT Report

    [TestMethod]
    public void DynamicCall_KnownMethod_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.call(""queue_free"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.AreEqual(0, dynamicDiagnostics.Count,
            $"queue_free exists on Node. Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    [TestMethod]
    public void DynamicGet_KnownProperty_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    var x = node.get(""name"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.AreEqual(0, dynamicDiagnostics.Count,
            $"name exists on Node. Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    #endregion

    #region Variant - Should NOT Report

    [TestMethod]
    public void DynamicCall_Variant_NoDiagnostic()
    {
        var code = @"
func test(obj):
    obj.call(""anything"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.AreEqual(0, dynamicDiagnostics.Count,
            $"Variant can have any method. Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    [TestMethod]
    public void DynamicGet_Variant_NoDiagnostic()
    {
        var code = @"
func test(obj):
    var x = obj.get(""anything"")
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.AreEqual(0, dynamicDiagnostics.Count,
            $"Variant can have any property. Found: {FormatDiagnostics(dynamicDiagnostics)}");
    }

    #endregion

    #region Dynamic Variable String - Should NOT Report

    [TestMethod]
    public void DynamicCall_VariableString_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    var method_name = ""some_method""
    node.call(method_name)
";
        var diagnostics = ValidateCode(code);
        var dynamicDiagnostics = FilterDynamicDiagnostics(diagnostics);
        Assert.AreEqual(0, dynamicDiagnostics.Count,
            $"Variable method name should be skipped. Found: {FormatDiagnostics(dynamicDiagnostics)}");
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
            null, null, null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckDynamicCalls = true,
            DynamicCallSeverity = GDDiagnosticSeverity.Warning
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterDynamicDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.DynamicMethodNotFound ||
            d.Code == GDDiagnosticCode.DynamicPropertyNotFound).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
