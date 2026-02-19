using GDShrapt.Abstractions;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level1;

/// <summary>
/// Level 1: Type guard and narrowing validation tests.
/// Tests that type guards (is checks) correctly narrow types
/// and that member access after narrowing is validated correctly.
/// </summary>
[TestClass]
public class TypeNarrowingValidationTests
{
    #region Single Is Check - Narrowing

    [TestMethod]
    public void IsCheck_SingleGuard_NarrowsType_NoDiagnostic()
    {
        var code = @"
func test(value):
    if value is String:
        print(value.length())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        // After 'is String', value is narrowed to String, so length() is valid
        Assert.AreEqual(0, memberDiagnostics.Count,
            $"After type guard, String.length() should be valid. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void IsCheck_Node2D_AccessPosition_NoDiagnostic()
    {
        var code = @"
func test(obj):
    if obj is Node2D:
        obj.position = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"After 'is Node2D', position should be accessible. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Multiple Is Checks - All Branches Narrowed

    [TestMethod]
    public void IsCheck_MultipleGuards_AllBranchesValid_NoDiagnostic()
    {
        var code = @"
func test(value):
    if value is int:
        print(value * 2)
    elif value is String:
        print(value.to_upper())
    elif value is Array:
        print(value.size())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"All branches have valid narrowed types. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    [TestMethod]
    public void IsCheck_NestedGuards_InnerNarrowingWorks()
    {
        var code = @"
func test(value):
    if value is Dictionary:
        if value.has(""name""):
            var name = value[""name""]
            if name is String:
                print(name.to_upper())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"Nested type guards should narrow correctly. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Typeof Checks

    [TestMethod]
    public void TypeofCheck_NarrowsToInt_NoDiagnostic()
    {
        var code = @"
func test(value):
    if typeof(value) == TYPE_INT:
        print(value + 10)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.AreEqual(0, typeDiagnostics.Count,
            $"After typeof check, arithmetic should be valid. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void TypeofCheck_MultipleTypes_AllValid()
    {
        var code = @"
func test(value):
    match typeof(value):
        TYPE_INT:
            print(value * 2)
        TYPE_STRING:
            print(value.length())
        TYPE_ARRAY:
            print(value.size())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"typeof match should narrow types. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Negative Cases - Access Without Guard

    [TestMethod]
    public void NoGuard_AccessStringMethod_DuckTypedNoWarning()
    {
        // length() exists in TypesMap (String), so duck typing infers the type
        // This is expected behavior - known methods are duck-typed
        var code = @"
func test(value):
    print(value.length())
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"length() is a known method - duck typing infers String. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void NoGuard_AccessUnknownMethod_ReportsWarning()
    {
        // unknown_method() does NOT exist in TypesMap, so warning should be shown
        var code = @"
func test(value):
    print(value.unknown_method())
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.IsTrue(unguardedDiagnostics.Count > 0,
            "Expected warning for unguarded call to unknown method on Variant");
    }

    [TestMethod]
    public void GuardInWrongBranch_AccessFails()
    {
        var code = @"
func test(value):
    if value is int:
        pass
    else:
        # value is NOT int here, could be anything
        print(value.length())  # Unguarded - value might not have length()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall ||
            d.Code == GDDiagnosticCode.MemberNotGuaranteed).ToList();

        var allDiags = diagnostics.ToList();

        Assert.IsTrue(unguardedDiagnostics.Count > 0,
            $"Expected warning for access in else branch (type not narrowed to String). All diagnostics ({allDiags.Count}): {string.Join("; ", allDiags.Select(d => $"{d.Code}:{d.Message} @L{d.StartLine}:{d.StartColumn}"))}");
    }

    #endregion

    #region Assert Type Guards

    [TestMethod]
    public void AssertIs_NarrowsType_NoDiagnostic()
    {
        var code = @"
func test(entity):
    assert(entity is Node2D)
    entity.position = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"After assert(is Node2D), position should be accessible. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region Early Return Pattern

    [TestMethod]
    public void EarlyReturn_AfterNullCheck_NarrowsType()
    {
        var code = @"
func test(obj):
    if obj == null:
        return
    # After this, obj is NOT null
    print(obj)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.AreEqual(0, typeDiagnostics.Count,
            $"After null check with early return, obj is narrowed. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void EarlyReturn_AfterIsCheck_NarrowsType()
    {
        var code = @"
func test(value):
    if not value is String:
        return
    # After this, value is String
    print(value.to_upper())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        Assert.AreEqual(0, memberDiagnostics.Count,
            $"After 'not is String' with early return, String methods should be valid. Found: {FormatDiagnostics(memberDiagnostics)}");
    }

    #endregion

    #region P1: In Operator Duck-Typed Check

    [TestMethod]
    public void P1_InOperator_AllowsDuckTypedMethodCall()
    {
        // P1: "method" in obj should allow obj.method() without GD7003
        var code = @"
func test(local):
    if ""custom_method"" in local:
        return local.custom_method()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"After 'method in obj' check, method call should be allowed. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region P8: Truthiness Check Narrowing

    [TestMethod]
    public void P8_TruthinessCheck_NarrowsAwayNull()
    {
        // P8: if collision: should narrow collision type to non-null
        var code = @"
extends CharacterBody2D
func test():
    var collision = get_slide_collision(0)
    if collision:
        var collider = collision.get_collider()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"After truthiness check, method call should be valid. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region P10: Null Equality Check with Early Return

    [TestMethod]
    public void P10_NullCheckReturn_NarrowsAfter()
    {
        // P10: if x == null: return should narrow x to non-null after
        var code = @"
extends Node
func test() -> Vector2:
    var parent = get_parent()
    if parent == null:
        return Vector2.ZERO
    var grandparent = parent.get_parent()
    return Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"After null check with return, method call should be valid. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void P10_NotNullCheck_InIfBranch()
    {
        // if x != null: x.method() should work
        var code = @"
extends Node
func test():
    var parent = get_parent()
    if parent != null:
        var name = parent.get_name()
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"After 'if x != null', method call should be valid. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return Enumerable.Empty<GDDiagnostic>();

        // Use composite provider that combines Godot types (Node2D.position, Vector2.ZERO, etc.)
        // with built-in GDScript types (String.length, Array.size, etc.)
        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,  // projectTypesProvider
            null,  // autoloadsProvider
            null); // sceneTypesProvider
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        // Use scriptFile.Class to validate the SAME AST that semantic model was built from
        var result = validator.Validate(scriptFile.Class);

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
