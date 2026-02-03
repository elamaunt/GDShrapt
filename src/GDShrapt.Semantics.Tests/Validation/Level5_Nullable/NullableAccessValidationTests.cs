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

    #region Builtin Type Method Returns - Should NOT Report (non-nullable)

    [TestMethod]
    public void ArrayFilterResult_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var arr: Array = [1, 2, 3]
    var filtered = arr.filter(func(x): return x > 1)
    var count = filtered.size()
    return count
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Array.filter() returns non-null Array. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void ArrayMapResult_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var arr: Array = [1, 2, 3]
    var mapped = arr.map(func(x): return x * 2)
    var first = mapped[0]
    return first
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Array.map() returns non-null Array. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void DictionaryKeysResult_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var dict: Dictionary = {""a"": 1, ""b"": 2}
    var keys = dict.keys()
    var first = keys[0]
    return first
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Dictionary.keys() returns non-null Array. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void StringMethodResult_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var str: String = ""hello""
    var upper = str.to_upper()
    var len = upper.length()
    return len
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"String.to_upper() returns non-null String. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void ChainedArrayMethods_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var arr: Array = [1, 2, 3, 4, 5]
    var result = arr.filter(func(x): return x > 2).map(func(x): return x * 2)
    var count = result.size()
    return count
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Chained Array methods return non-null Array. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void LambdaParameter_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var arr: Array = [""a"", ""b"", ""c""]
    var result = arr.filter(func(x): return x.length() > 0)
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        // Lambda parameters receive collection elements - they should not be null
        var lambdaParamDiagnostics = nullDiagnostics.Where(d => d.Message.Contains("'x'")).ToList();
        Assert.AreEqual(0, lambdaParamDiagnostics.Count,
            $"Lambda parameter 'x' should not be null. Found: {FormatDiagnostics(lambdaParamDiagnostics)}");
    }

    [TestMethod]
    public void UntypedVariableWithArrayInitializer_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var numbers = [1, 2, 3, 4, 5]
    var filtered = numbers.filter(func(x): return x > 2)
    var mapped = filtered.map(func(x): return x * 2)
    return mapped.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Untyped variables with Array initializer should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void PackedArraySlice_ShouldNotReportPotentiallyNull()
    {
        var code = @"
extends Node

func test():
    var arr: PackedStringArray = [""a"", ""b"", ""c""]
    var sliced = arr.slice(0, 2)
    var count = sliced.size()
    return count
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"PackedArray.slice() returns non-null PackedArray. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Visitor Pattern Tests - No Duplicate Diagnostics

    [TestMethod]
    public void IndexerOnNullable_ReportsOnlyGD7006()
    {
        // Indexer should report GD7006, not GD7005 or GD7007
        var code = @"
func test():
    var obj = null
    var x = obj[0]
";
        var diagnostics = ValidateCode(code);
        var relevantDiags = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullIndexer ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall).ToList();

        Assert.AreEqual(1, relevantDiags.Count,
            $"Expected exactly 1 diagnostic for indexer access. Found: {FormatDiagnostics(relevantDiags)}");
        Assert.AreEqual(GDDiagnosticCode.PotentiallyNullIndexer, relevantDiags[0].Code,
            $"Indexer should report GD7006. Found: {relevantDiags[0].Code}");
    }

    [TestMethod]
    public void NestedMethodCall_ReportsOnlyOuterNull()
    {
        // obj.method1().method2() - should only report for obj, not for method1() result
        var code = @"
func test():
    var obj = null
    obj.method1().method2()
";
        var diagnostics = ValidateCode(code);
        var objDiags = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall &&
            d.Message.Contains("'obj'")).ToList();

        Assert.AreEqual(1, objDiags.Count,
            $"Nested method call should report exactly 1 diagnostic for 'obj'. Found: {FormatDiagnostics(objDiags)}");
    }

    [TestMethod]
    public void PropertyThenMethod_ReportsBothAccessTypes()
    {
        // obj.prop.method() - currently reports both GD7005 (prop access) and GD7007 (method call)
        // This is expected behavior: both the property access and method call are on potentially null obj
        var code = @"
func test():
    var obj = null
    obj.prop.method()
";
        var diagnostics = ValidateCode(code);
        var objDiags = diagnostics.Where(d =>
            (d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
             d.Code == GDDiagnosticCode.PotentiallyNullMethodCall) &&
            d.Message.Contains("'obj'")).ToList();

        // Reports both: GD7005 for obj.prop and GD7007 for .method()
        Assert.AreEqual(2, objDiags.Count,
            $"obj.prop.method() reports both property access and method call. Found: {FormatDiagnostics(objDiags)}");
        Assert.IsTrue(objDiags.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullAccess),
            "Should include GD7005 for property access");
        Assert.IsTrue(objDiags.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            "Should include GD7007 for method call");
    }

    [TestMethod]
    public void MultipleIndependentAccesses_EachReportsOnce()
    {
        // Multiple independent accesses on same null variable
        var code = @"
func test():
    var obj = null
    obj.prop1
    obj.method()
    obj[0]
";
        var diagnostics = ValidateCode(code);
        var objDiags = diagnostics.Where(d =>
            d.Message.Contains("'obj'")).ToList();

        // Each access should report once: GD7005 for prop, GD7007 for method, GD7006 for indexer
        Assert.AreEqual(3, objDiags.Count,
            $"Three independent accesses should report 3 diagnostics. Found: {FormatDiagnostics(objDiags)}");
        Assert.IsTrue(objDiags.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullAccess),
            "Should include GD7005 for property access");
        Assert.IsTrue(objDiags.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            "Should include GD7007 for method call");
        Assert.IsTrue(objDiags.Any(d => d.Code == GDDiagnosticCode.PotentiallyNullIndexer),
            "Should include GD7006 for indexer access");
    }

    [TestMethod]
    public void ChainedCallsAfterNull_ReportsFirstAccessOnly()
    {
        // Long chain: obj.a().b().c() - should report GD7007 for obj.a() only
        var code = @"
func test():
    var obj = null
    obj.a().b().c()
";
        var diagnostics = ValidateCode(code);
        var objDiags = diagnostics.Where(d =>
            d.Message.Contains("'obj'")).ToList();

        Assert.AreEqual(1, objDiags.Count,
            $"Chained calls should report exactly 1 diagnostic for 'obj'. Found: {FormatDiagnostics(objDiags)}");
        Assert.AreEqual(GDDiagnosticCode.PotentiallyNullMethodCall, objDiags[0].Code,
            $"Should report GD7007 for the first method call. Found: {objDiags[0].Code}");
    }

    #endregion

    #region Lambda Flow State Tests (verifies flow analysis works, not AST workaround)

    [TestMethod]
    public void NestedLambda_InnerParameterAccess_ShouldNotReportNull()
    {
        // This test verifies that flow state correctly tracks parameters in nested lambdas.
        // An AST-based workaround would only check the immediate lambda, but flow state
        // properly maintains scope hierarchy.
        var code = @"
extends Node

func test():
    var arr = [[1, 2], [3, 4]]
    var result = arr.map(func(inner):
        return inner.filter(func(x): return x > 1)
    )
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        // Both 'inner' and 'x' are lambda parameters and should not be null
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Nested lambda parameters should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NestedLambda_OuterParameterUsedInInner_ShouldNotReportNull()
    {
        // Tests that outer lambda parameter is visible in inner lambda via captured state
        var code = @"
extends Node

func test():
    var arr = [1, 2, 3]
    var result = arr.map(func(outer):
        var inner_arr = [10, 20]
        return inner_arr.filter(func(inner): return inner > outer)
    )
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        // 'outer' is from outer lambda, 'inner' is from inner lambda - both non-null
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Outer lambda parameter captured in inner lambda should not be null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void LambdaParameterShadowing_ShouldTrackCorrectScope()
    {
        // Tests that parameter shadowing is handled correctly by flow state
        // The inner 'x' shadows the outer 'x', each should be tracked independently
        var code = @"
extends Node

func test():
    var arr = [[""a"", ""bb""], [""ccc""]]
    var result = arr.map(func(x):
        return x.filter(func(x): return x.length() > 1)
    )
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        // Both 'x' parameters (outer and inner, shadowed) should be non-null
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Shadowed lambda parameters should each be non-null in their scope. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void LambdaParameterReassignment_ShouldTrackNullability()
    {
        // Tests that flow state tracks reassignment within lambda body
        // This cannot be detected by AST-based workaround
        var code = @"
extends Node

func test():
    var arr = [""hello"", ""world""]
    var result = arr.map(func(x):
        var len = x.length()  # x is non-null here
        return len
    )
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Lambda parameter usage should be tracked by flow state. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TripleNestedLambda_AllParametersNonNull()
    {
        // Stress test for deeply nested lambdas
        var code = @"
extends Node

func test():
    var arr = [[[1, 2], [3]], [[4, 5, 6]]]
    var result = arr.map(func(a):
        return a.map(func(b):
            return b.filter(func(c): return c > 0)
        )
    )
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        // a, b, c are all lambda parameters at different nesting levels
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"All nested lambda parameters should be non-null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Type Guard Tests

    [TestMethod]
    public void OrTypeGuard_BothTypesProtectsFromNull()
    {
        var code = @"
extends Node

func test(value):
    if value is String or value is StringName:
        return value.length()
    return 0
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"or type guard should protect from null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void OrTypeGuard_MixedNumericTypes()
    {
        var code = @"
extends Node

func test(value):
    if value is int or value is float:
        return value * 2
    return 0
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"or type guard with numeric types should protect from null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NegatedIsTypeGuard_EarlyReturn_ProtectsAfter()
    {
        var code = @"
extends Node

func test(value):
    if not value is Dictionary:
        return null
    return value.keys()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'value'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"negated is guard with early return should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NegatedIsTypeGuard_WithStringReturn_ProtectsAfter()
    {
        var code = @"
extends Node

func test(data):
    if not data is Dictionary:
        return ""not dictionary""
    var keys = data.keys()
    return keys
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'data'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"negated is guard should protect data. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void WhileIsTypeGuard_ProtectsInBody()
    {
        var code = @"
extends Node

func test(data):
    var current = data
    while current is Dictionary:
        var val = current.keys()
        current = current.get(""next"")
    return current
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'current'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"while is guard should protect in body. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void WhileAndCondition_ProtectsInBody()
    {
        var code = @"
extends Node

func test(data):
    var current = data
    while current is Dictionary and current.has(""next""):
        var val = current.get(""value"")
        current = current.get(""next"")
    return current
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'current'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"while and condition should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TernaryIsTypeGuard_ProtectsInTrueBranch()
    {
        var code = @"
extends Node

func test(value):
    var result = value.length() if value is String else 0
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"ternary is guard should protect true branch. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void TernaryIsTypeGuard_FalseBranchNotProtected()
    {
        var code = @"
extends Node

func test(value):
    var result = 0 if value is String else value.length()
    return result
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            "False branch should still warn about potential null");
    }

    #endregion

    #region Guard Clause Inside Nested Blocks

    [TestMethod]
    public void NullGuardWithBreak_InWhileLoop_ProtectsAfter()
    {
        var code = @"
extends Node

func test(tokens, pos):
    var result = parse_term(tokens, pos)
    if result == null:
        return null

    var value = result[""value""]
    var new_pos = result[""pos""]

    while new_pos < 10:
        var right = parse_term(tokens, new_pos + 1)
        if right == null:
            break
        value = value + right[""value""]
        new_pos = right[""pos""]

    return {""value"": value, ""pos"": new_pos}

func parse_term(tokens, pos):
    if pos < 0:
        return null
    return {""value"": 1, ""pos"": pos + 1}
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'right'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clause with break should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullGuardWithReturn_InNestedIf_ProtectsAfter()
    {
        var code = @"
extends Node

func test(tokens, pos):
    if pos < 10:
        var inner = parse_expr(tokens, pos)
        if inner == null:
            return null
        var val = inner[""value""]
        return val
    return 0

func parse_expr(tokens, pos):
    if pos < 0:
        return null
    return {""value"": 1}
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'inner'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clause with return should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullGuardWithContinue_InForLoop_ProtectsAfter()
    {
        var code = @"
extends Node

func test(items):
    var results = []
    for item in items:
        var data = get_data(item)
        if data == null:
            continue
        results.append(data[""value""])
    return results

func get_data(item):
    if item < 0:
        return null
    return {""value"": item * 2}
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'data'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clause with continue should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullGuardInNestedWhile_ProtectsAfter()
    {
        var code = @"
extends Node

func parse_expression(tokens, pos):
    var left = parse_term(tokens, pos)
    if left == null:
        return null

    var value = left[""value""]
    var new_pos = left[""pos""]

    while new_pos < tokens.size():
        if tokens[new_pos] != ""+"":
            break
        var right = parse_term(tokens, new_pos + 1)
        if right == null:
            break
        value = value + right[""value""]
        new_pos = right[""pos""]

    return {""value"": value, ""pos"": new_pos}

func parse_term(tokens, pos):
    return {""value"": 1, ""pos"": pos + 1}
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'right'") || d.Message.Contains("'left'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clauses in nested while should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void CyclicInference_ParseExpr_Pattern()
    {
        // Real pattern from cyclic_inference.gd parse_expr function
        var code = @"
extends Node

func parse_expr(tokens, pos):
    var result = parse_term(tokens, pos)
    if result == null:
        return null

    var value = result[""value""]
    var new_pos = result[""pos""]

    while new_pos < tokens.size():
        var op = tokens[new_pos]
        if op != ""+"" and op != ""-"":
            break

        var right = parse_term(tokens, new_pos + 1)
        if right == null:
            break

        if op == ""+"":
            value = value + right[""value""]
        else:
            value = value - right[""value""]
        new_pos = right[""pos""]

    return {""value"": value, ""pos"": new_pos}

func parse_term(tokens, pos):
    if pos >= tokens.size():
        return null
    return {""value"": 1, ""pos"": pos + 1}
";
        var diagnostics = ValidateCode(code);
        // After 'if right == null: break', right is guaranteed non-null
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'right'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clause with break in while should protect 'right'. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void CyclicInference_ParseFactor_Pattern()
    {
        // Real pattern from cyclic_inference.gd parse_factor function
        var code = @"
extends Node

func parse_factor(tokens, pos):
    if pos >= tokens.size():
        return null

    var token = tokens[pos]

    if token == ""("":
        var inner = parse_expr(tokens, pos + 1)
        if inner == null:
            return null
        if inner[""pos""] >= tokens.size() or tokens[inner[""pos""]] != "")"":
            return null
        return {""value"": inner[""value""], ""pos"": inner[""pos""] + 1}

    return {""value"": 0, ""pos"": pos + 1}

func parse_expr(tokens, pos):
    if pos >= tokens.size():
        return null
    return {""value"": 1, ""pos"": pos + 1}
";
        var diagnostics = ValidateCode(code);
        // After 'if inner == null: return null', inner is guaranteed non-null
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'inner'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard clause with return in nested if should protect 'inner'. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Super Keyword Tests

    [TestMethod]
    public void SuperMethodCall_NeverWarnNull()
    {
        var code = @"
extends Node

func _ready():
    super._ready()

func custom_method():
    pass
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'super'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"super should never warn about null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void SuperMethodCall_InOverriddenMethod_NeverWarnNull()
    {
        var code = @"
extends Node2D

func _process(delta: float):
    super._process(delta)
    position.x += 1
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'super'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"super in overridden method should never warn. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void SuperMethodCall_MultipleCallsInMethod_NeverWarnNull()
    {
        var code = @"
extends Node

func custom():
    super.custom()
    print(""after super"")
    super.custom()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'super'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Multiple super calls should never warn. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Or Guard Clause Tests

    [TestMethod]
    public void NullOrInvalidGuard_ProtectsAfterReturn()
    {
        var code = @"
extends Node

var target: Node

func attack():
    if target == null or not is_instance_valid(target):
        return
    target.get_name()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'target'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard with 'null or not is_instance_valid' should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InvalidOrNullGuard_ReversedOrder_ProtectsAfterReturn()
    {
        var code = @"
extends Node

var target: Node

func attack():
    if not is_instance_valid(target) or target == null:
        return
    target.get_name()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'target'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard with reversed 'not is_instance_valid or null' should protect. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullOrNullGuard_BothSidesNullCheck_ProtectsAfterReturn()
    {
        var code = @"
extends Node

var a: Node
var b: Node

func test():
    if a == null or b == null:
        return
    a.get_name()
    b.get_name()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'a'") || d.Message.Contains("'b'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard with 'a == null or b == null' should protect both. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullOrInvalid_WithAdditionalCondition_ProtectsAfterReturn()
    {
        var code = @"
extends Node

var target: Node

func is_target_in_range() -> bool:
    if target == null or not is_instance_valid(target):
        return false
    return position.distance_to(target.position) <= 100.0
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'target'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Guard should protect target.position access. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void NullOrInvalid_MultipleAccessesAfterGuard_AllProtected()
    {
        var code = @"
extends Node

var target: Node

func process_target():
    if target == null or not is_instance_valid(target):
        return

    var name = target.get_name()
    var pos = target.position
    target.queue_free()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'target'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"All accesses after guard should be protected. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Signal Tests

    [TestMethod]
    public void InheritedSignal_NeverWarnNull()
    {
        var code = @"
extends Node

func test():
    tree_exiting.connect(func(): pass)
    tree_entered.connect(func(): pass)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'tree_exiting'") ||
            d.Message.Contains("'tree_entered'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Inherited signals should never warn. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void CustomSignal_NeverWarnNull()
    {
        var code = @"
extends Node

signal my_signal(value: int)

func test():
    my_signal.connect(func(v): pass)
    my_signal.emit(42)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'my_signal'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Custom signals should never warn. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void SignalFromCanvasItem_NeverWarnNull()
    {
        var code = @"
extends Node2D

func test():
    visibility_changed.connect(func(): pass)
    hidden.connect(func(): pass)
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics).Where(d =>
            d.Message.Contains("'visibility_changed'") ||
            d.Message.Contains("'hidden'")).ToList();
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"CanvasItem signals should never warn. Found: {FormatDiagnostics(nullDiagnostics)}");
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
