using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for enum member access and builtin type method validation.
/// Verifies that accessing enum values (EnumName.VALUE) and builtin methods
/// does not produce false positive diagnostics.
/// </summary>
[TestClass]
public class EnumAndBuiltinTypeValidationTests
{
    #region Enum Access - No False Positives

    [TestMethod]
    public void EnumAccess_LocalEnum_NoDiagnostic()
    {
        var code = @"
enum AIState { PATROL, ATTACK, IDLE }

func test():
    var state = AIState.PATROL
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Expected no GD7002 for enum access. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_AllValues_NoDiagnostic()
    {
        var code = @"
enum AIState { PATROL, ATTACK, IDLE, FLEE }

func test():
    var a = AIState.PATROL
    var b = AIState.ATTACK
    var c = AIState.IDLE
    var d = AIState.FLEE
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"All enum value accesses should be valid. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_WithExplicitValue_NoDiagnostic()
    {
        var code = @"
enum Priority { LOW = 0, MEDIUM = 5, HIGH = 10 }

func test():
    var p = Priority.HIGH
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Enum with explicit values should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_InMatchStatement_NoDiagnostic()
    {
        var code = @"
enum State { A, B, C }

func test(s: int):
    match s:
        State.A:
            print(""a"")
        State.B:
            print(""b"")
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Enum access in match should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_InIfCondition_NoDiagnostic()
    {
        var code = @"
enum State { ON, OFF }

func test(current: int):
    if current == State.ON:
        print(""on"")
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Enum access in condition should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_InFunctionCall_NoDiagnostic()
    {
        var code = @"
enum Color { RED, GREEN, BLUE }

func set_color(c: int):
    pass

func test():
    set_color(Color.RED)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Enum access as function argument should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void EnumAccess_MultipleEnums_NoDiagnostic()
    {
        var code = @"
enum StateA { X, Y }
enum StateB { P, Q }

func test():
    var a = StateA.X
    var b = StateB.Q
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Multiple enum accesses should work. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Signal Methods - No False Positives

    [TestMethod]
    public void SignalEmit_DirectCall_NoDiagnostic()
    {
        var code = @"
signal my_signal(value: int)

func test():
    my_signal.emit(42)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Signal.emit() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void SignalConnect_DirectCall_NoDiagnostic()
    {
        var code = @"
signal my_signal

func on_signal():
    pass

func test():
    my_signal.connect(on_signal)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Signal.connect() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void SignalIsConnected_DirectCall_NoDiagnostic()
    {
        var code = @"
signal my_signal

func on_signal():
    pass

func test():
    var connected = my_signal.is_connected(on_signal)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Signal.is_connected() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Inherited Members (Implicit Self) - No False Positives

    [TestMethod]
    public void InheritedMember_Position_NoDiagnostic()
    {
        var code = @"
extends Node2D

func test():
    var dist = position.distance_squared_to(Vector2.ZERO)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Inherited 'position' should resolve to Vector2. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void InheritedMember_GlobalPosition_NoDiagnostic()
    {
        var code = @"
extends Node2D

func test():
    var pos = global_position
    var len = pos.length()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Inherited 'global_position' should resolve to Vector2. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void InheritedMember_Rotation_NoDiagnostic()
    {
        var code = @"
extends Node2D

func test():
    var r = rotation
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"Inherited 'rotation' should be valid. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Color Methods - No False Positives

    [TestMethod]
    public void ColorMethod_Inverted_NoDiagnostic()
    {
        var code = @"
func test():
    var c: Color = Color.RED
    var inv = c.inverted()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Color.inverted() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void ColorMethod_Lightened_NoDiagnostic()
    {
        var code = @"
func test():
    var c: Color = Color.BLUE
    var lighter = c.lightened(0.5)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Color.lightened() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void ColorMethod_Lerp_NoDiagnostic()
    {
        var code = @"
func test():
    var a: Color = Color.RED
    var b: Color = Color.BLUE
    var mid = a.lerp(b, 0.5)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Color.lerp() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Vector2 Methods - No False Positives

    [TestMethod]
    public void Vector2Method_Length_NoDiagnostic()
    {
        var code = @"
func test():
    var v: Vector2 = Vector2(3, 4)
    var len = v.length()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector2.length() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void Vector2Method_Normalized_NoDiagnostic()
    {
        var code = @"
func test():
    var v: Vector2 = Vector2(3, 4)
    var n = v.normalized()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector2.normalized() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void Vector2Method_DistanceSquaredTo_NoDiagnostic()
    {
        var code = @"
func test():
    var v: Vector2 = Vector2(1, 2)
    var d = v.distance_squared_to(Vector2.ZERO)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector2.distance_squared_to() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void Vector2Method_Rotated_NoDiagnostic()
    {
        var code = @"
func test():
    var v: Vector2 = Vector2(1, 0)
    var rotated = v.rotated(PI / 2)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector2.rotated() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void Vector2Method_Angle_NoDiagnostic()
    {
        var code = @"
func test():
    var v: Vector2 = Vector2(1, 1)
    var a = v.angle()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector2.angle() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Vector3 Methods - No False Positives

    [TestMethod]
    public void Vector3Method_Cross_NoDiagnostic()
    {
        var code = @"
func test():
    var a: Vector3 = Vector3.UP
    var b: Vector3 = Vector3.RIGHT
    var c = a.cross(b)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector3.cross() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void Vector3Method_DistanceTo_NoDiagnostic()
    {
        var code = @"
func test():
    var a: Vector3 = Vector3(1, 2, 3)
    var b: Vector3 = Vector3(4, 5, 6)
    var d = a.distance_to(b)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Vector3.distance_to() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region String Methods - No False Positives

    [TestMethod]
    public void StringMethod_ToUpper_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello""
    var upper = s.to_upper()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"String.to_upper() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void StringMethod_Split_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""a,b,c""
    var parts = s.split("","")
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"String.split() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void StringMethod_Contains_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello world""
    var has_hello = s.contains(""hello"")
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"String.contains() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Array Methods - No False Positives

    [TestMethod]
    public void ArrayMethod_Map_NoDiagnostic()
    {
        var code = @"
func double(x):
    return x * 2

func test():
    var arr: Array = [1, 2, 3]
    var doubled = arr.map(double)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Array.map() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void ArrayMethod_Filter_NoDiagnostic()
    {
        var code = @"
func is_positive(x):
    return x > 0

func test():
    var arr: Array = [-1, 0, 1, 2]
    var positive = arr.filter(is_positive)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Array.filter() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void ArrayMethod_PopBack_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var last = arr.pop_back()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Array.pop_back() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Dictionary Methods - No False Positives

    [TestMethod]
    public void DictionaryMethod_Keys_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary = {""a"": 1, ""b"": 2}
    var k = dict.keys()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Dictionary.keys() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region GD7007: Godot API Return Types - No False Positives

    [TestMethod]
    public void GetNodesInGroup_ReturnsArray_NoGD7007()
    {
        // First verify the provider returns the correct type
        var godotProvider = new GDGodotTypesProvider();

        // Debug: get raw method data
        var overloads = godotProvider.GetMethodOverloads("SceneTree", "get_nodes_in_group");
        if (overloads != null && overloads.Count > 0)
        {
            var methodData = overloads[0];
            Console.WriteLine($"GDScriptReturnTypeName: '{methodData.GDScriptReturnTypeName}'");
            Console.WriteLine($"CSharpReturnTypeFullName: '{methodData.CSharpReturnTypeFullName}'");
        }

        var memberInfo = godotProvider.GetMember("SceneTree", "get_nodes_in_group");
        Console.WriteLine($"GDGodotTypesProvider return type: '{memberInfo?.Type}'");

        // Verify that the return type is Array[Node] or at least Array (not Array`1)
        Assert.IsNotNull(memberInfo, "get_nodes_in_group method not found in SceneTree");
        Assert.IsTrue(
            memberInfo.Type == "Array[Node]" || memberInfo.Type == "Array",
            $"Expected Array[Node] or Array, got '{memberInfo.Type}'");

        // Test for false positive GD7007: get_nodes_in_group() always returns Array[Node], never null
        var code = @"
extends Node

func test(type_filter: String):
    var candidates = get_tree().get_nodes_in_group(type_filter)
    if candidates.is_empty():
        return null
    return candidates[0]
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall ||
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullIndexer).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"get_nodes_in_group() returns Array, should not report GD7007. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void DictionaryMethod_Has_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary = {""a"": 1}
    var has_a = dict.has(""a"")
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Dictionary.has() should be recognized. Found: {FormatDiagnostics(methodNotFound)}");
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
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();
    }

    private static List<GDDiagnostic> FilterMethodNotFoundDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
