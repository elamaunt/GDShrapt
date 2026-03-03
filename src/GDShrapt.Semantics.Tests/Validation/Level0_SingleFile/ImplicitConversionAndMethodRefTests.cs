using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for implicit type conversions, enum built-in methods,
/// method references (Callable), and type normalization.
/// </summary>
[TestClass]
public class ImplicitConversionAndMethodRefTests
{
    #region Implicit Conversions - int ↔ float

    [TestMethod]
    public void Assignment_FloatToInt_NoDiagnostic()
    {
        var code = @"
func test():
    var x: int = 3.14
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"float→int is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_IntToFloat_NoDiagnostic()
    {
        var code = @"
func test():
    var x: float = 42
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"int→float is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Implicit Conversions - String ↔ StringName

    [TestMethod]
    public void Assignment_StringToStringName_NoDiagnostic()
    {
        var code = @"
func test():
    var sn: StringName = ""hello""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"String→StringName is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_StringNameToString_NoDiagnostic()
    {
        var code = @"
func test():
    var sn: StringName = &""hello""
    var s: String = sn
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"StringName→String is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Implicit Conversions - Array ↔ PackedArrays

    [TestMethod]
    public void Assignment_ArrayToPackedStringArray_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [""a"", ""b""]
    var packed: PackedStringArray = arr
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array→PackedStringArray is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_PackedStringArrayToArray_NoDiagnostic()
    {
        var code = @"
func test():
    var packed: PackedStringArray = PackedStringArray()
    var arr: Array = packed
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"PackedStringArray→Array is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_ArrayToPackedInt32Array_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
    var packed: PackedInt32Array = arr
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array→PackedInt32Array is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_PackedVector2ArrayToArray_NoDiagnostic()
    {
        var code = @"
func test():
    var packed: PackedVector2Array = PackedVector2Array()
    var arr: Array = packed
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"PackedVector2Array→Array is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Implicit Conversions - String → NodePath

    [TestMethod]
    public void Assignment_StringToNodePath_NoDiagnostic()
    {
        var code = @"
func test():
    var path: NodePath = ""some/node/path""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"String→NodePath is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_StringToNodePath_Variable_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""path/to/node""
    var path: NodePath = s
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"String variable→NodePath is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Enum ↔ int Conversions

    [TestMethod]
    public void Assignment_EnumToInt_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var d = Direction.LEFT
    var i: int = d
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Enum→int is implicit in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_IntToEnum_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var d: int = 0
    var dir = d as int
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"int→enum is allowed in GDScript. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Return_LocalEnumValue_NoDiagnostic()
    {
        var code = @"
enum Points { NORTH, EAST, SOUTH, WEST }

func direction() -> Points:
    return Points.NORTH
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Returning local enum value from enum-typed function should not produce GD3007. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Return_IntFromLocalEnumFunc_NoDiagnostic()
    {
        var code = @"
enum Points { NORTH, EAST, SOUTH, WEST }

func direction() -> Points:
    return 0
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.IncompatibleReturnType).ToList();
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Returning int from local enum-typed function should not produce GD3007. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Enum Built-in Methods

    [TestMethod]
    public void EnumMethod_Values_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var vals = Direction.values()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Enum.values() is a built-in method. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void EnumMethod_Keys_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var k = Direction.keys()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Enum.keys() is a built-in method. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void EnumMethod_Size_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var s = Direction.size()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Enum.size() is a built-in method. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void EnumMethod_Has_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var h = Direction.has(0)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Enum.has() is a built-in method. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void EnumMethod_FindKey_NoDiagnostic()
    {
        var code = @"
enum Direction { LEFT, RIGHT, UP, DOWN }

func test():
    var k = Direction.find_key(0)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Enum.find_key() is a built-in method. Found: {FormatDiagnostics(methodNotFound)}");
    }

    #endregion

    #region Method Reference → Callable

    [TestMethod]
    public void MethodRef_CallDeferred_NoDiagnostic()
    {
        var code = @"
extends Timer

func test():
    start.call_deferred()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"timer.start is a method reference (Callable), call_deferred should work. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void MethodRef_Bind_NoDiagnostic()
    {
        var code = @"
extends Node

func _on_event(value: int):
    pass

func test():
    var cb = _on_event.bind(42)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = FilterMethodNotFoundDiagnostics(diagnostics);
        Assert.AreEqual(0, methodNotFound.Count,
            $"Method reference.bind() should work on Callable. Found: {FormatDiagnostics(methodNotFound)}");
    }

    [TestMethod]
    public void MethodRef_MemberAccess_IsCallable()
    {
        var code = @"
extends Node

func _on_event():
    pass

func test():
    var cb: Callable = _on_event
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Method reference should be Callable type. Found: {FormatDiagnostics(typeDiagnostics)}");
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

    private static List<GDDiagnostic> FilterTypeDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment ||
            d.Code == GDDiagnosticCode.TypeAnnotationMismatch ||
            d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
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

    #region Qualified Type Name Matching

    [TestMethod]
    public void Assignment_QualifiedEnumType_NoDiagnostic()
    {
        var code = @"
extends Node

enum GameState { MENU, PLAYING, PAUSED }

var state: GameState = GameState.MENU
";
        var diagnostics = ValidateCode(code);
        var typeDiags = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiags.Count,
            $"Enum assignment should not produce type mismatch. Found: {FormatDiagnostics(typeDiags)}");
    }

    [TestMethod]
    public void Reassignment_QualifiedTypeName_NoDiagnostic()
    {
        var code = @"
extends Node

enum TowerType { BASIC, FIRE, ICE }

var tower_type: TowerType = TowerType.BASIC

func change_type():
    tower_type = TowerType.FIRE
";
        var diagnostics = ValidateCode(code);
        var gd3001 = diagnostics.Where(d => d.Code == GDDiagnosticCode.TypeMismatch).ToList();
        Assert.AreEqual(0, gd3001.Count,
            $"Re-assignment with qualified enum value should not produce GD3001. Found: {FormatDiagnostics(gd3001)}");
    }

    #endregion

    #region Vector Negation

    [TestMethod]
    public void Negation_Vector2_NoDiagnostic()
    {
        var code = @"
extends Node

func test():
    var v := Vector2(10, 20)
    var neg := -v
";
        var diagnostics = ValidateCode(code);
        var operandDiags = diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
        Assert.AreEqual(0, operandDiags.Count,
            $"Vector2 supports unary negation. Found: {FormatDiagnostics(operandDiags)}");
    }

    [TestMethod]
    public void Negation_Vector3_NoDiagnostic()
    {
        var code = @"
extends Node

func test():
    var v := Vector3(1, 2, 3)
    var neg := -v
";
        var diagnostics = ValidateCode(code);
        var operandDiags = diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
        Assert.AreEqual(0, operandDiags.Count,
            $"Vector3 supports unary negation. Found: {FormatDiagnostics(operandDiags)}");
    }

    #endregion
}
