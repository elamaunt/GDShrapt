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

    #region Indexer Access - Typed Containers

    [TestMethod]
    public void IndexerAccess_TypedArray_MethodCall_NoDiagnostic()
    {
        // Array[String][index].length() should not produce GD7003
        var code = @"
func test():
    var names: Array[String] = [""hello"", ""world""]
    var len = names[0].length()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Array[String][index] should have type String. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void IndexerAccess_TypedArray_AppendMethod_NoDiagnostic()
    {
        // After dict[key] = [], dict[key].append() should work without warning
        var code = @"
var spatial_grid: Dictionary[Vector2i, Array] = {}

func update():
    var cell = Vector2i(0, 0)
    if not spatial_grid.has(cell):
        spatial_grid[cell] = []
    spatial_grid[cell].append(1)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Dictionary[K,Array][key].append() should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void IndexerAccess_TypedDictionary_PropertyAccess_NoDiagnostic()
    {
        // Dictionary[int, Node2D][key].position should not produce GD7002
        var code = @"
func test():
    var nodes: Dictionary[int, Node2D] = {}
    nodes[0] = Node2D.new()
    var pos = nodes[0].position
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Dictionary[int, Node2D][key].position should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void IndexerAccess_UntypedDict_AppendMethod_NoDiagnostic()
    {
        // For untyped dict, dict[key].append() should use Potential confidence
        // because append is a common container method
        var code = @"
var spatial_grid = {}

func update():
    var cell = Vector2i(0, 0)
    if not spatial_grid.has(cell):
        spatial_grid[cell] = []
    spatial_grid[cell].append(1)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Untyped dict[key].append() should use Potential confidence for common container methods. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void IndexerAccess_NestedTypedArray_MethodCall_NoDiagnostic()
    {
        // Array[Array[String]][i][j].length() should work
        var code = @"
func test():
    var matrix: Array[Array[String]] = [[""a"", ""b""], [""c"", ""d""]]
    var len = matrix[0][0].length()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Nested Array indexing should preserve types. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Method Chain on Untyped Object (GD4002 False Positive Fix)

    [TestMethod]
    public void MethodChain_GetScriptGetPath_ShouldNotReportWrongType()
    {
        // This was a false positive: get_path().get_file() reported as
        // "Method 'get_file' not found on type 'Vector3'"
        // because FindMethodReturnTypeInCommonTypes found NavigationPathQueryResult3D.get_path() -> Vector3[]
        // instead of Resource.get_path() -> String
        var code = @"
extends Node

func get_script_name(obj):
    return obj.get_script().get_path().get_file().get_basename()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        // Should NOT report GD4002 (MethodNotFound) claiming get_file doesn't exist on Vector3
        Assert.IsFalse(memberDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("Vector3")),
            $"Should NOT report MethodNotFound with Vector3 type. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void MethodChain_UntypedObject_ShouldNotInferWrongType()
    {
        // When caller type is unknown, we should NOT try to guess the return type
        // from all types that have a method with that name
        var code = @"
extends Node

func process_item(item):
    return item.get_path().get_file()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        // Should NOT report GD4002 claiming Vector3 doesn't have get_file
        Assert.IsFalse(memberDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("Vector3")),
            $"Should NOT infer wrong type from method name search. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void UniqueMethodName_ShouldStillInferType()
    {
        // Methods unique to one type should still have their return type inferred
        // e.g., to_upper() is unique to String, so return type should be String
        var code = @"
extends Node

func process_text(text):
    var upper = text.to_upper()
    return upper.length()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        // Should NOT report any member access issues since to_upper() is unique to String
        // and length() exists on String
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"Unique method names should still allow type inference. Found: {FormatDiagnostics(memberDiagnostics)}");
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

    private static List<GDDiagnostic> FilterUnguardedDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedPropertyAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
