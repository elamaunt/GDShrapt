using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Level 0: Static method validation tests.
/// Tests validate static vs instance method call patterns.
/// </summary>
[TestClass]
public class StaticMethodValidationTests
{
    #region Static Method Call - Valid

    [TestMethod]
    public void StaticMethodCall_OnClassName_WithInnerClass_NoDiagnostic()
    {
        var code = @"
class_name TestScript

class MyHelper:
    static func compute(x: int) -> int:
        return x * 2

func test():
    var result = MyHelper.compute(10)
";
        var diagnostics = ValidateCode(code);
        var staticDiagnostics = FilterStaticMethodDiagnostics(diagnostics);

        staticDiagnostics.Should().BeEmpty(
            "Static method call on class name should not produce warnings");
    }

    [TestMethod]
    public void StaticMethodCall_OnBuiltinClass_NoDiagnostic()
    {
        // Time is a built-in class with static methods
        var code = @"
func test():
    var ticks = Time.get_ticks_msec()
";
        var diagnostics = ValidateCode(code);
        var staticDiagnostics = FilterStaticMethodDiagnostics(diagnostics);

        staticDiagnostics.Should().BeEmpty(
            "Static method call on built-in class should not produce warnings");
    }

    #endregion

    #region Instance Method Call - Valid

    [TestMethod]
    public void InstanceMethodCall_OnInstance_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = Node.new()
    node.add_child(Node.new())
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        memberDiagnostics.Should().BeEmpty(
            "Instance method call on instance should not produce warnings");
    }

    #endregion

    #region Invalid Static/Instance Usage

    [TestMethod]
    public void InstanceMethodCall_OnClassName_ReportsDiagnostic()
    {
        var code = @"
class_name TestScript

class MyHelper:
    func instance_method() -> void:
        pass

func test():
    MyHelper.instance_method()
";
        var diagnostics = ValidateCode(code);

        // Should report either MethodNotFound or a specific static method diagnostic
        var relevantDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.InstanceMethodCalledAsStatic).ToList();

        relevantDiagnostics.Should().NotBeEmpty(
            "Calling instance method on class name should produce a diagnostic");
    }

    [TestMethod]
    public void InstanceMethodCall_OnClassName_WithArgs_ReportsDiagnostic()
    {
        var code = @"
class_name TestScript

class MyHelper:
    func process_data(value: int) -> int:
        return value * 2

func test():
    var result = MyHelper.process_data(42)
";
        var diagnostics = ValidateCode(code);

        var relevantDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.InstanceMethodCalledAsStatic).ToList();

        relevantDiagnostics.Should().NotBeEmpty(
            "Calling instance method with args on class name should produce a diagnostic");
    }

    #endregion

    #region Static Method on Instance - GDScript Allows This

    [TestMethod]
    public void StaticMethodCall_OnInstance_NoDiagnostic()
    {
        // GDScript allows calling static methods on instances
        var code = @"
class_name TestScript

class MyHelper:
    static func compute(x: int) -> int:
        return x * 2

func test():
    var helper = MyHelper.new()
    var result = helper.compute(10)
";
        var diagnostics = ValidateCode(code);
        var staticDiagnostics = FilterStaticMethodDiagnostics(diagnostics);

        // GDScript allows this pattern
        staticDiagnostics.Should().BeEmpty(
            "GDScript allows calling static methods on instances");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void StaticMethodCall_OnSelfInnerClass_NoDiagnostic()
    {
        var code = @"
class_name TestScript

class Inner:
    static func helper() -> int:
        return 42

func test():
    var x = Inner.helper()
";
        var diagnostics = ValidateCode(code);
        var staticDiagnostics = FilterStaticMethodDiagnostics(diagnostics);

        staticDiagnostics.Should().BeEmpty(
            "Static method on inner class should work");
    }

    [TestMethod]
    public void NewCall_OnClassName_NoDiagnostic()
    {
        // new() is a special static-like constructor
        var code = @"
func test():
    var node = Node.new()
    var node2d = Node2D.new()
";
        var diagnostics = ValidateCode(code);
        var memberDiagnostics = FilterMemberAccessDiagnostics(diagnostics);

        memberDiagnostics.Should().BeEmpty(
            "new() on class name should not produce warnings");
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

    private static List<GDDiagnostic> FilterStaticMethodDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.InstanceMethodCalledAsStatic ||
            d.Code == GDDiagnosticCode.StaticMethodCalledOnInstance).ToList();
    }

    private static List<GDDiagnostic> FilterMemberAccessDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PropertyNotFound ||
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.MemberNotAccessible ||
            d.Code == GDDiagnosticCode.NotCallable).ToList();
    }

    #endregion
}
