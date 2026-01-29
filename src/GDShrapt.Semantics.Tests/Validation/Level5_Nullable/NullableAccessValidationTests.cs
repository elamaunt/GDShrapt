using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_Nullable;

/// <summary>
/// Level 5: Nullable access validation tests.
/// Tests validate that access on potentially-null variables is detected.
/// </summary>
[TestClass]
public class NullableAccessValidationTests
{
    #region Potentially Null Access - Should Report

    [TestMethod]
    public void NullVariable_PropertyAccess_ReportsDiagnostic()
    {
        var code = @"
func test():
    var data = null
    var x = data.foo
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullAccess),
            $"Expected GD7005 for null.foo. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullVariable_IndexerAccess_ReportsDiagnostic()
    {
        var code = @"
func test():
    var data = null
    var x = data[0]
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullIndexer),
            $"Expected GD7006 for null[0]. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullVariable_MethodCall_ReportsDiagnostic()
    {
        var code = @"
func test():
    var data = null
    data.method()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"Expected GD7007 for null.method(). Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region After Null Check - Should NOT Report

    [TestMethod]
    public void AfterNullCheck_PropertyAccess_NoDiagnostic()
    {
        var code = @"
func test():
    var data = null
    if data != null:
        var x = data.foo
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After null check, access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void AfterTruthinessCheck_PropertyAccess_NoDiagnostic()
    {
        var code = @"
func test():
    var data = null
    if data:
        var x = data.foo
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After truthiness check, access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Typed Variables - Should NOT Report (non-nullable)

    [TestMethod]
    public void TypedInt_PropertyAccess_NoDiagnostic()
    {
        // Primitive types are never null
        var code = @"
func test():
    var x: int = 5
    var y = x.abs()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"int cannot be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TypedString_PropertyAccess_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello""
    var len = s.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Typed String should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Self Access - Should NOT Report

    [TestMethod]
    public void SelfAccess_PropertyAccess_NoDiagnostic()
    {
        var code = @"
var my_prop: int = 0

func test():
    var x = self.my_prop
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"self is never null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Initialized Variables - Should NOT Report

    [TestMethod]
    public void InitializedArray_MethodCall_NoDiagnostic()
    {
        // Array initialized with [] should not report null
        var code = @"
var items: Array = []
func test():
    items.append(1)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Initialized array should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void ExportInitializedArray_MethodCall_NoDiagnostic()
    {
        // @export with initializer should not report null
        var code = @"
@export var points: Array[Vector2] = []
func test():
    if points.is_empty():
        pass
    points.append(Vector2.ZERO)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"@export with initializer should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void LocalInitializedArray_MethodCall_NoDiagnostic()
    {
        var code = @"
func test():
    var path: Array[Vector2] = []
    path.clear()
    path.append(Vector2.ZERO)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Local initialized array should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitializedDictionary_MethodCall_NoDiagnostic()
    {
        var code = @"
var data: Dictionary = {}
func test():
    data.clear()
    var size = data.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Initialized dictionary should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region is_instance_valid Guard - Should NOT Report

    [TestMethod]
    public void AfterIsInstanceValid_PropertyAccess_NoDiagnostic()
    {
        // is_instance_valid guards should suppress null warnings
        var code = @"
var target: Node2D = null
func test():
    if is_instance_valid(target):
        var pos = target.position
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After is_instance_valid, access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void AfterNotIsInstanceValid_EarlyReturn_NoDiagnostic()
    {
        // not is_instance_valid with early return should make code after safe
        var code = @"
var target: Node2D = null
func test():
    if not is_instance_valid(target):
        return
    var pos = target.position
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After early return from not is_instance_valid, access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void IsInstanceValid_NestedInAnd_NoDiagnostic()
    {
        var code = @"
var target: Node2D = null
func test():
    if is_instance_valid(target) and target.visible:
        pass
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"is_instance_valid in 'and' should narrow type. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Local Enums - Should NOT Report

    [TestMethod]
    public void LocalEnum_MemberAccess_NoDiagnostic()
    {
        // Local enums are never null
        var code = @"
enum State { IDLE, RUNNING, STOPPED }
var current: State = State.IDLE
func test():
    match current:
        State.IDLE:
            pass
        State.RUNNING:
            pass
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Enum access should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EnumInExpression_NoDiagnostic()
    {
        var code = @"
enum Mode { A, B, C }
func test():
    var m = Mode.A
    if m == Mode.B:
        pass
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Enum in expression should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EnumAssignment_NoDiagnostic()
    {
        var code = @"
enum Direction { UP, DOWN, LEFT, RIGHT }
func test():
    var dir = Direction.UP
    dir = Direction.DOWN
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Enum assignment should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Signals - Should NOT Report

    [TestMethod]
    public void SignalEmit_NoDiagnostic()
    {
        // Signals are always valid, never null
        var code = @"
signal my_signal(value: int)
func test():
    my_signal.emit(42)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Signal emit should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void SignalConnect_NoDiagnostic()
    {
        var code = @"
signal state_changed(new_state: int)
func test():
    state_changed.connect(_on_state_changed)

func _on_state_changed(state: int):
    pass
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Signal connect should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void SignalWithoutArgs_Emit_NoDiagnostic()
    {
        var code = @"
signal done
func test():
    done.emit()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Signal without args emit should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Inherited Properties - Should NOT Report

    [TestMethod]
    public void InheritedProperty_Position_NoDiagnostic()
    {
        // position is inherited from Node2D, always exists
        var code = @"
extends Node2D
func test():
    var dist = position.distance_to(Vector2.ZERO)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Inherited property 'position' should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InheritedProperty_Scale_NoDiagnostic()
    {
        var code = @"
extends Node2D
func test():
    scale = scale * 2.0
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Inherited property 'scale' should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InheritedProperty_Name_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var n = name.to_upper()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Inherited property 'name' should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Strictness Mode Tests

    [TestMethod]
    public void NullableStrictness_Error_ReportsAsError()
    {
        var code = @"
func test():
    var x = null
    x.to_string()
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Error
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(), "Should report nullable access");
        Assert.IsTrue(nullDiagnostics.All(d => d.Severity == GDDiagnosticSeverity.Error),
            $"All nullable diagnostics should be Error severity. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullableStrictness_Off_ReportsNothing()
    {
        var code = @"
func test():
    var x = null
    x.to_string()
    var y = x.foo
    var z = x[0]
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Off
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Off mode should report nothing. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void WarnOnUntypedParameters_False_SkipsUntypedParameters()
    {
        var code = @"
func process(items):
    items.append(1)
    var x = items.size()
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Strict,
            WarnOnUntypedParameters = false
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Should skip untyped parameters. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void WarnOnUntypedParameters_True_WarnsOnUntypedParameters()
    {
        var code = @"
func process(items):
    items.append(1)
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Strict,
            WarnOnUntypedParameters = true
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            $"Should warn on untyped parameter. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void WarnOnDictionaryIndexer_False_SkipsDictionaryIndexer()
    {
        var code = @"
func test():
    var data = {}
    data[""key""] = 1
    var val = data[""key""]
    val.to_string()
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Strict,
            WarnOnDictionaryIndexer = false
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Should skip dictionary indexer access. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullableStrictness_Normal_SkipsBothUntypedAndIndexer()
    {
        // Note: WarnOnDictionaryIndexer only affects DIRECT indexer access (data["key"].method()),
        // not variables assigned from indexer (var val = data["key"]; val.method()).
        // This is by design - the variable itself is potentially null.
        var code = @"
func process(items, data):
    items.append(1)
    data[""key""].foo()
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Normal,
            WarnOnUntypedParameters = false,
            WarnOnDictionaryIndexer = false
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Normal mode with both options off should skip. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TypedParameter_StillReportsNull()
    {
        var code = @"
func process(items: Array):
    var x = null
    x.foo()
";
        var options = new GDSemanticValidatorOptions
        {
            CheckNullableAccess = true,
            NullableStrictness = GDNullableStrictnessMode.Strict,
            WarnOnUntypedParameters = false
        };
        var diagnostics = ValidateCodeWithOptions(code, options);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            $"Should still warn on explicit null var. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Truthiness Guard in If Body - Parameters

    [TestMethod]
    public void Parameter_SimpleTruthiness_BodyAccess_NoDiagnostic()
    {
        // Simple case: if obj: obj.method()
        var code = @"
func test(obj):
    if obj:
        obj.method()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if obj', body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Parameter_TruthinessInAnd_BodyAccess_NoDiagnostic()
    {
        // With and: if obj and obj.has_method(): obj.call()
        var code = @"
func test(obj):
    if obj and obj.has_method(""foo""):
        obj.call(""foo"")
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if obj and ...', body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Parameter_TruthinessInAnd_PropertyAccess_NoDiagnostic()
    {
        // With and and property: if obj and prop in obj: obj.get()
        var code = @"
func test(obj, prop):
    if obj and prop in obj:
        return obj.get(prop)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if obj and prop in obj', body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Parameter_IsInstanceValidInAnd_BodyAccess_NoDiagnostic()
    {
        // With is_instance_valid in and: if is_instance_valid(obj) and ...: obj.method()
        var code = @"
func test(obj):
    if is_instance_valid(obj) and obj.has_method(""foo""):
        obj.call(""foo"")
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if is_instance_valid(obj) and ...', body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Parameter_NotEqualNullInAnd_BodyAccess_NoDiagnostic()
    {
        // With != null in and: if obj != null and ...: obj.method()
        var code = @"
func test(obj):
    if obj != null and obj.is_visible():
        obj.hide()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if obj != null and ...', body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Parameter_NotChecked_ShouldReport()
    {
        // Without check - should report
        var code = @"
func test(obj):
    obj.method()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            "Without check, parameter access should report warning");
    }

    [TestMethod]
    public void LocalNull_SimpleTruthiness_BodyAccess_NoDiagnostic()
    {
        // Local var initialized to null with truthiness check
        var code = @"
func test():
    var obj = null
    if obj:
        obj.method()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After 'if obj' for local null, body access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void Elif_TruthinessGuard_NoDiagnostic()
    {
        // Elif branch with truthiness check
        var code = @"
func test(a, b):
    if a:
        a.method()
    elif b:
        b.method()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"In elif with truthiness guard, access should be safe. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Is Type Guard

    [TestMethod]
    public void IsTypeGuard_InAndCondition_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item is String and item == ""stop"":
        return item.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"'is Type' check should suppress null warning. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void IsTypeGuard_InIfBody_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item is Array:
        return item.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"'is Type' check in if condition should suppress null warning. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void IsTypeGuard_WithMethodCallInBody_NoDiagnostic()
    {
        var code = @"
func test(current, key):
    if current is Array and key is int:
        if key >= current.size():
            return null
        return current[key]
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"'is Type' check should suppress null warning for method call. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Equality with Non-Null Literal

    [TestMethod]
    public void EqualityWithStringLiteral_NoDiagnostic()
    {
        // item == "stop" implies item is not null (otherwise equality would be false)
        var code = @"
func test(item):
    if item == ""stop"":
        return item.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Equality with string literal should imply non-null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EqualityWithNumberLiteral_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item == 42:
        return item.abs()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Equality with number literal should imply non-null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EqualityWithLiteralInAnd_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item is String and item == ""stop"":
        return item.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"'is Type' + equality should suppress null warning. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EqualityWithArrayLiteral_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item == []:
        return item.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Equality with array literal should imply non-null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EqualityWithDictLiteral_NoDiagnostic()
    {
        var code = @"
func test(item):
    if item == {}:
        return item.keys()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Equality with dict literal should imply non-null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void EqualityReversed_LiteralOnLeft_NoDiagnostic()
    {
        var code = @"
func test(item):
    if ""stop"" == item:
        return item.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Reversed equality (literal == var) should also work. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Typed Parameters - Should NOT Report

    [TestMethod]
    public void TypedParameter_Array_NoDiagnostic()
    {
        var code = @"
func loop_return(items: Array):
    return items.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Typed parameter 'items: Array' should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TypedParameter_Dictionary_NoDiagnostic()
    {
        var code = @"
func process(data: Dictionary):
    return data.get(""key"")
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Typed parameter 'data: Dictionary' should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TypedParameter_String_NoDiagnostic()
    {
        var code = @"
func process(name: String):
    return name.length()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Typed parameter 'name: String' should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TypedParameter_MultipleParams_NoDiagnostic()
    {
        var code = @"
func process(items: Array, data: Dictionary, name: String):
    var count = items.size()
    var value = data.get(""key"")
    var len = name.length()
    return count + len
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Multiple typed parameters should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckNullableAccess = true,
            NullableAccessSeverity = GDDiagnosticSeverity.Warning
        };
        return ValidateCodeWithOptions(code, options);
    }

    private static IEnumerable<GDDiagnostic> ValidateCodeWithOptions(string code, GDSemanticValidatorOptions options)
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

        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterNullableDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullIndexer ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall ||
            d.Code == GDDiagnosticCode.ClassVariableMayBeNull ||
            d.Code == GDDiagnosticCode.NullableTypeNotChecked).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
