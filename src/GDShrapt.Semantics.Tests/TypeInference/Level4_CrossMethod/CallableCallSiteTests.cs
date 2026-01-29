using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference.Level4_CrossMethod;

/// <summary>
/// Tests for Callable call site tracking.
/// Verifies that lambda expressions and method references passed as arguments
/// are properly tracked as call sites for type inference.
/// </summary>
[TestClass]
public class CallableCallSiteTests
{
    #region Lambda Passed as Argument

    [TestMethod]
    public void Lambda_PassedAsArgument_IsCallSite()
    {
        var code = @"
class_name TestScript

func process(callback: Callable):
    callback.call(42)

func test():
    process(func(x): print(x))
";
        var project = CreateProject(
            ("res://test.gd", code));

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("TestScript", "process");

        // Assert
        callSites.Should().HaveCount(1);
        var callSite = callSites[0];
        callSite.Arguments.Should().HaveCount(1);

        // The argument should be recognized as Callable (lambda)
        var argExpr = callSite.Arguments[0];
        argExpr.Should().NotBeNull();
    }

    [TestMethod]
    public void Lambda_PassedToSignalConnect_NoErrors()
    {
        var code = @"
class_name TestScript
extends Node

signal button_pressed

func test():
    button_pressed.connect(func(): print(""Attack!""))
";
        // Verify no errors for the lambda usage
        var diagnostics = ValidateCode(code);
        var callableDiagnostics = diagnostics.Where(d =>
            d.Message.Contains("Callable") ||
            d.Message.Contains("callback")).ToList();

        callableDiagnostics.Should().BeEmpty(
            "Lambda passed to connect() should not produce errors");
    }

    #endregion

    #region Method Reference Passed as Argument

    [TestMethod]
    public void MethodReference_PassedToConnect_NoErrors()
    {
        var code = @"
class_name TestScript
extends Node

signal my_signal(value: int)

func _on_signal(value: int):
    print(value)

func test():
    my_signal.connect(_on_signal)
";
        // Method reference should be recognized as Callable
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        methodNotFound.Should().BeEmpty(
            "Method reference passed to connect should be valid");
    }

    [TestMethod]
    public void MethodReference_PassedAsParameter_IsCallSite()
    {
        var code = @"
class_name TestScript

func execute(callback: Callable):
    callback.call()

func _handler():
    print(""handled"")

func test():
    execute(_handler)
";
        var project = CreateProject(
            ("res://test.gd", code));

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("TestScript", "execute");

        // Assert
        callSites.Should().HaveCount(1);
        callSites[0].Arguments.Should().HaveCount(1);
    }

    #endregion

    #region Callable Constructor Call Sites

    [TestMethod]
    public void CallableConstructor_NoErrors()
    {
        var code = @"
class_name TestScript

var dictionary = {}

func test():
    var tween = create_tween()
    tween.tween_callback(Callable(dictionary, ""clear""))
";
        // Callable() constructor should be valid
        var diagnostics = ValidateCode(code);
        var callableErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("Callable")).ToList();

        // Should not have errors about Callable constructor
        callableErrors.Should().BeEmpty(
            "Callable constructor should be recognized");
    }

    #endregion

    #region Lambda Stored and Called

    [TestMethod]
    public void Lambda_StoredInVariable_CallIsValid()
    {
        var code = @"
class_name TestScript

func test():
    var my_lambda = func(message):
        print(message)
    my_lambda.call(""Hello!"")
";
        // my_lambda.call() should be recognized as valid
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("call")).ToList();

        methodNotFound.Should().BeEmpty(
            "call() on stored lambda variable should be valid");
    }

    [TestMethod]
    public void Lambda_ImmediateCall_Works()
    {
        var code = @"
class_name TestScript

func test():
    (func(x): print(x)).call(42)
";
        // Immediate call on lambda should work
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("call")).ToList();

        methodNotFound.Should().BeEmpty(
            "Immediate call() on lambda should be valid");
    }

    #endregion

    #region Signal Handler Type Compatibility

    [TestMethod]
    public void Lambda_PassedToSignal_NoTypeMismatch()
    {
        var code = @"
class_name TestScript
extends Node

signal data_received(data: Dictionary)

func test():
    data_received.connect(func(d): print(d))
";
        // Lambda connected to signal should be compatible
        var diagnostics = ValidateCode(code);
        var typeMismatch = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();

        // Untyped lambda parameter should accept Dictionary
        typeMismatch.Should().BeEmpty(
            "Lambda with untyped param should be compatible with signal signature");
    }

    [TestMethod]
    public void MethodReference_WithMatchingSignature_NoTypeMismatch()
    {
        var code = @"
class_name TestScript
extends Node

signal value_changed(old_value: int, new_value: int)

func _on_value_changed(old_val: int, new_val: int):
    print(""Changed from "", old_val, "" to "", new_val)

func test():
    value_changed.connect(_on_value_changed)
";
        // Method with matching signature should connect without errors
        var diagnostics = ValidateCode(code);
        var signatureDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.ArgumentTypeMismatch ||
            d.Code == GDDiagnosticCode.WrongArgumentCount).ToList();

        signatureDiagnostics.Should().BeEmpty(
            "Method with matching signature should connect without errors");
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params (string path, string content)[] scripts)
    {
        var project = new GDScriptProject(scripts.Select(s => s.content).ToArray());
        project.AnalyzeAll();
        return project;
    }

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
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

    #endregion
}
