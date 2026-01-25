using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Level 0: Basic member access validation tests.
/// Single file, single method, no inheritance or cross-file.
/// Tests validate that property/method access on typed variables is checked.
/// </summary>
[TestClass]
public class BasicMemberAccessValidationTests
{
    #region Property Access - Valid

    [TestMethod]
    public void PropertyAccess_Node2D_Position_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node2D = Node2D.new()
    var pos = node.position
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"position exists on Node2D. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void PropertyAccess_String_Length_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello""
    var len = s.length()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"length() exists on String. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void PropertyAccess_Array_Size_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var count = arr.size()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"size() exists on Array. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Property Access - Invalid

    [TestMethod]
    public void PropertyAccess_Node_UnknownProperty_ReportsDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    var x = node.nonexistent_property
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.IsTrue(memberDiagnostics.Count > 0,
            "Expected diagnostic for accessing non-existent property on Node");
    }

    [TestMethod]
    public void PropertyAccess_Node_Velocity_ReportsDiagnostic()
    {
        // velocity is on CharacterBody2D, not Node
        var code = @"
func test():
    var node: Node = Node.new()
    var v = node.velocity
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.IsTrue(memberDiagnostics.Count > 0,
            "Expected diagnostic: velocity is not on Node (it's on CharacterBody2D)");
    }

    #endregion

    #region Method Access - Valid

    [TestMethod]
    public void MethodAccess_Node_QueueFree_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.queue_free()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"queue_free() exists on Node. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void MethodAccess_Node_GetChildren_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    var children = node.get_children()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"get_children() exists on Node. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Method Access - Invalid

    [TestMethod]
    public void MethodAccess_Node_UnknownMethod_ReportsDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.unknown_method()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.IsTrue(memberDiagnostics.Count > 0,
            "Expected diagnostic for calling non-existent method on Node");
    }

    [TestMethod]
    public void MethodAccess_Int_NoMethods_ReportsDiagnostic()
    {
        var code = @"
func test():
    var x: int = 42
    x.some_method()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.IsTrue(memberDiagnostics.Count > 0,
            "Expected diagnostic: int has no methods");
    }

    #endregion

    #region Variant/Untyped Access - Warnings for duck typing

    [TestMethod]
    public void PropertyAccess_Variant_UnguardedAccess_ReportsWarning()
    {
        var code = @"
func test(obj):
    var x = obj.some_property
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedPropertyAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodAccess).ToList();

        // Unguarded access on Variant should produce a warning
        Assert.IsTrue(unguardedDiagnostics.Count > 0,
            "Expected warning for unguarded property access on Variant parameter");
    }

    [TestMethod]
    public void MethodAccess_Variant_UnguardedCall_ReportsWarning()
    {
        var code = @"
func test(obj):
    obj.some_method()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodCall ||
            d.Code == GDDiagnosticCode.UnguardedMethodAccess).ToList();

        Assert.IsTrue(unguardedDiagnostics.Count > 0,
            "Expected warning for unguarded method call on Variant parameter");
    }

    #endregion

    #region Chained Member Access

    [TestMethod]
    public void ChainedAccess_Valid_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node2D = Node2D.new()
    var parent = node.get_parent()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"Chained access should work. Found: {FormatDiagnostics(memberDiagnostics)}");
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

        // Use composite provider that combines Godot types (Node2D.position, etc.)
        // with built-in GDScript types (String.length, Array.size, etc.)
        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,  // projectTypesProvider
            null,  // autoloadsProvider
            null); // sceneTypesProvider
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

    private static List<GDDiagnostic> FilterMemberAccessDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound ||
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.MemberNotAccessible ||
            d.Code == GDDiagnosticCode.NotCallable).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
