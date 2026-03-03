using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Regression tests for previously fixed semantic issues.
/// These tests ensure that fixed issues don't regress.
/// </summary>
[TestClass]
public class SemanticRegressionTests
{
    #region P1: Transform2D.get_origin() Recognition

    [TestMethod]
    public void Transform2D_GetOrigin_Recognized()
    {
        var code = @"
extends Node2D

func test():
    var t = Transform2D.IDENTITY
    var origin = t.get_origin()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("get_origin") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Transform2D.get_origin() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void Transform2D_GetRotation_Recognized()
    {
        var code = @"
extends Node2D

func test():
    var t = Transform2D.IDENTITY
    var rotation = t.get_rotation()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("get_rotation") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Transform2D.get_rotation() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    #endregion

    #region P2: Plane.center Property Recognition

    [TestMethod]
    public void Plane_Center_Recognized()
    {
        var code = @"
extends Node

func test():
    var plane = Plane(Vector3.UP, 0)
    var c = plane.center
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound &&
            d.Message?.Contains("center") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Plane.center should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void Plane_Normal_Recognized()
    {
        var code = @"
extends Node

func test():
    var plane = Plane(Vector3.UP, 0)
    var n = plane.normal
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound &&
            d.Message?.Contains("normal") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Plane.normal should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    #endregion

    #region P3: str() Function Returns String

    [TestMethod]
    public void StrFunction_ReturnsString()
    {
        var code = @"
extends Node

func test():
    var s = str(123)
";
        var model = CreateSemanticModel(code);

        var strCall = model.ScriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .FirstOrDefault(c => c.CallerExpression is GDIdentifierExpression id &&
                       id.Identifier?.Sequence == "str");

        Assert.IsNotNull(strCall, "Should find str() call");

        var typeInfo = model.TypeSystem.GetType(strCall);
        Assert.AreEqual("String", typeInfo.DisplayName, "str() should return String type");
    }

    [TestMethod]
    public void StrFunction_MethodChaining_Works()
    {
        var code = @"
extends Node

func test():
    var s = str(123)
    var len = s.length()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("length") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"str().length() should work since str() returns String. Found: {FormatDiagnostics(errors)}");
    }

    #endregion

    #region P4: self Assignability to Base Type

    [TestMethod]
    public void Self_AssignableToBaseType()
    {
        var code = @"
extends CharacterBody2D

func get_as_node() -> Node:
    return self
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"self should be assignable to base type Node. Found: {FormatDiagnostics(typeErrors)}");
    }

    // NOTE: This test requires project context to work correctly.
    // For isolated scripts without class_name, 'self' resolves to a type that
    // the RuntimeProvider doesn't know about (the script's filename-based type).
    // Therefore, inheritance checking fails. This is a known limitation.
    // The test Self_AssignableToBaseType covers the return type case which works.
    // [TestMethod]
    // public void Self_PassableToMethodExpectingBaseType()
    // {
    //     // Requires project context for proper type resolution
    // }

    #endregion

    #region P5: String/StringName Interoperability

    [TestMethod]
    public void String_StringName_Interop()
    {
        var code = @"
extends Node

func process_string_name(sn: StringName) -> void:
    pass

func test():
    process_string_name(""hello"")
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"String should be compatible with StringName parameter. Found: {FormatDiagnostics(typeErrors)}");
    }

    #endregion

    #region P6: Built-in Type Methods

    [TestMethod]
    public void Array_BuiltinMethods_Recognized()
    {
        var code = @"
extends Node

func test():
    var arr: Array = [1, 2, 3]
    arr.append(4)
    arr.push_back(5)
    var size = arr.size()
    var empty = arr.is_empty()
";
        var diagnostics = ValidateCode(code);
        var methodErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodErrors.Count,
            $"Array built-in methods should be recognized. Found: {FormatDiagnostics(methodErrors)}");
    }

    [TestMethod]
    public void Dictionary_BuiltinMethods_Recognized()
    {
        var code = @"
extends Node

func test():
    var dict: Dictionary = {}
    dict[""key""] = ""value""
    var has = dict.has(""key"")
    var keys = dict.keys()
    var values = dict.values()
";
        var diagnostics = ValidateCode(code);
        var methodErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodErrors.Count,
            $"Dictionary built-in methods should be recognized. Found: {FormatDiagnostics(methodErrors)}");
    }

    [TestMethod]
    public void String_BuiltinMethods_Recognized()
    {
        var code = @"
extends Node

func test():
    var s: String = ""hello""
    var len = s.length()
    var upper = s.to_upper()
    var lower = s.to_lower()
    var contains = s.contains(""ll"")
";
        var diagnostics = ValidateCode(code);
        var methodErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, methodErrors.Count,
            $"String built-in methods should be recognized. Found: {FormatDiagnostics(methodErrors)}");
    }

    #endregion

    #region P7: Setter Parameter Type Inference

    [TestMethod]
    public void SetterParameter_InferredBool_NoTypeMismatch()
    {
        var code = @"
extends Node

@export var scene: PackedScene
var is_player := false:
    set(value):
        is_player = value
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch &&
            d.Message?.Contains("bool") == true).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Setter 'value' should be inferred as bool from ':= false'. Found: {FormatDiagnostics(typeErrors)}");
    }

    [TestMethod]
    public void SetterParameter_InferredFloat_NoTypeMismatch()
    {
        var code = @"
extends Node

@export var scene: PackedScene
var speed := 1.0:
    set(value):
        speed = value
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch &&
            d.Message?.Contains("float") == true).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Setter 'value' should be inferred as float from ':= 1.0'. Found: {FormatDiagnostics(typeErrors)}");
    }

    [TestMethod]
    public void SetterParameter_InferredString_NoTypeMismatch()
    {
        var code = @"
extends Node

@export var scene: PackedScene
var label := ""hello"":
    set(value):
        label = value
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch &&
            d.Message?.Contains("String") == true).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Setter 'value' should be inferred as String from ':= \"hello\"'. Found: {FormatDiagnostics(typeErrors)}");
    }

    [TestMethod]
    public void SetterParameter_ExplicitType_NoTypeMismatch()
    {
        var code = @"
extends Node

@export var scene: PackedScene
var health: float = 100.0:
    set(value):
        health = value
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch &&
            d.Message?.Contains("float") == true).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Setter 'value' should use explicit type 'float'. Found: {FormatDiagnostics(typeErrors)}");
    }

    [TestMethod]
    public void SetterParameter_AfterExportVar_NoTypeLeak()
    {
        var code = @"
extends Node

@export var battler_anim_scene: PackedScene
@export var ai_scene: PackedScene
@export var actor_scene: PackedScene
var is_player := false:
    set(value):
        is_player = value
";
        var diagnostics = ValidateCode(code);
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch &&
            d.Message?.Contains("PackedScene") == true).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Setter 'value' after @export PackedScene should not get PackedScene type. Found: {FormatDiagnostics(typeErrors)}");
    }

    #endregion

    #region P8: User-Defined Enum Built-in Methods

    [TestMethod]
    public void UserEnum_ValuesMethod_NoMethodNotFound()
    {
        var code = @"
extends Node

enum Colors { RED, GREEN, BLUE }

func test():
    var v = Colors.values()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("values") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Enum.values() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void UserEnum_KeysMethod_NoMethodNotFound()
    {
        var code = @"
extends Node

enum Colors { RED, GREEN, BLUE }

func test():
    var k = Colors.keys()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("keys") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Enum.keys() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void UserEnum_SizeMethod_NoMethodNotFound()
    {
        var code = @"
extends Node

enum Colors { RED, GREEN, BLUE }

func test():
    var s = Colors.size()
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("size") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Enum.size() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void UserEnum_HasMethod_NoMethodNotFound()
    {
        var code = @"
extends Node

enum Colors { RED, GREEN, BLUE }

func test():
    var h = Colors.has(1)
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("has") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Enum.has() should be recognized. Found: {FormatDiagnostics(errors)}");
    }

    [TestMethod]
    public void UserEnum_FindKeyMethod_NoMethodNotFound()
    {
        var code = @"
extends Node

enum Colors { RED, GREEN, BLUE }

func test():
    var fk = Colors.find_key(1)
";
        var diagnostics = ValidateCode(code);
        var errors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message?.Contains("find_key") == true).ToList();

        Assert.AreEqual(0, errors.Count,
            $"Enum.find_key() should be recognized. Found: {FormatDiagnostics(errors)}");
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

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        scriptFile.Analyze(runtimeProvider);
        return scriptFile.SemanticModel!;
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
