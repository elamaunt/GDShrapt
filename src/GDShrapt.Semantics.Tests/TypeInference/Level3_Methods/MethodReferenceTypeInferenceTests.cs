using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference.Level3_Methods;

/// <summary>
/// Tests for method reference type inference.
/// When a method is referenced without calling it (e.g., var cb = _on_timeout),
/// it should be typed as Callable, not null.
/// This enables .bind(), .call(), .is_valid() methods to work without GD4002.
/// </summary>
[TestClass]
public class MethodReferenceTypeInferenceTests
{
    #region Method Reference Infers Callable

    [TestMethod]
    public void MethodReference_InfersCallableType()
    {
        var code = @"
class_name Test
extends Node

func _on_timeout():
    pass

func test():
    var cb = _on_timeout
";
        var type = InferVariableType(code, "cb");
        type.Should().Be("Callable",
            "Method reference should be inferred as Callable type");
    }

    [TestMethod]
    public void SemanticModel_FindSymbol_ReturnsMethodWithCorrectKind()
    {
        var code = @"
class_name Test
extends Node

func _on_timeout():
    pass

func test():
    pass
";
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var symbol = semanticModel.FindSymbol("_on_timeout");
        symbol.Should().NotBeNull("Method should be registered as symbol");
        symbol!.Kind.Should().Be(GDSymbolKind.Method,
            "Method symbol should have Kind = Method");
    }

    [TestMethod]
    public void SemanticModel_GetExpressionType_ReturnsCallableForMethodIdentifier()
    {
        var code = @"
class_name Test
extends Node

func _on_timeout():
    pass

func test():
    var timer = Timer.new()
    var bound = _on_timeout.bind(timer)
";
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        // Find the _on_timeout identifier in the expression _on_timeout.bind(timer)
        // Use scriptFile.Class to get the AST that the SemanticModel knows about
        var methodIdentifier = scriptFile.Class!.AllNodes
            .OfType<GDIdentifierExpression>()
            .FirstOrDefault(id => id.Identifier?.Sequence == "_on_timeout" &&
                                  id.Parent is GDMemberOperatorExpression);

        methodIdentifier.Should().NotBeNull("_on_timeout identifier should exist in member expression");

        var type = semanticModel.GetExpressionType(methodIdentifier!);
        type.Should().Be("Callable",
            $"Method reference should return Callable. Actual: {type ?? "null"}");
    }

    [TestMethod]
    public void MethodReference_WithParameters_InfersCallableType()
    {
        var code = @"
class_name Test
extends Node

func _on_data_received(data: Dictionary):
    pass

func test():
    var handler = _on_data_received
";
        var type = InferVariableType(code, "handler");
        type.Should().Be("Callable",
            "Method reference with parameters should be inferred as Callable");
    }

    [TestMethod]
    public void MethodReference_WithReturnType_InfersCallableType()
    {
        var code = @"
class_name Test
extends Node

func _calculate(x: int, y: int) -> int:
    return x + y

func test():
    var calc = _calculate
";
        var type = InferVariableType(code, "calc");
        type.Should().Be("Callable",
            "Method reference with return type should be inferred as Callable");
    }

    #endregion

    #region bind() Works on Method Reference - No GD4002

    [TestMethod]
    public void MethodReference_BindWorks_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _on_timeout(timer: Timer):
    pass

func test():
    var timer = Timer.new()
    var bound = _on_timeout.bind(timer)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("bind")).ToList();

        methodNotFound.Should().BeEmpty(
            "bind() should work on method reference (Callable type)");
    }

    [TestMethod]
    public void MethodReference_BindWithTimer_Connect_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _on_timeout(timer: Timer):
    timer.stop()

func test():
    var timer = Timer.new()
    timer.timeout.connect(_on_timeout.bind(timer))
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("bind")).ToList();

        methodNotFound.Should().BeEmpty(
            "bind() on method reference passed to connect() should work");
    }

    #endregion

    #region call() Works on Method Reference - No GD4002

    [TestMethod]
    public void MethodReference_CallWorks_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _process(data):
    print(data)

func test():
    var processor = _process
    processor.call(42)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("call")).ToList();

        methodNotFound.Should().BeEmpty(
            "call() should work on method reference stored in variable");
    }

    [TestMethod]
    public void MethodReference_CallvWorks_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _handler(a, b, c):
    pass

func test():
    var cb = _handler
    cb.callv([1, 2, 3])
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("callv")).ToList();

        methodNotFound.Should().BeEmpty(
            "callv() should work on method reference");
    }

    #endregion

    #region is_valid() / is_null() Work on Method Reference - No GD4002

    [TestMethod]
    public void MethodReference_IsValidWorks_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _handler():
    pass

func test():
    var cb = _handler
    if cb.is_valid():
        cb.call()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("is_valid")).ToList();

        methodNotFound.Should().BeEmpty(
            "is_valid() should work on method reference");
    }

    [TestMethod]
    public void MethodReference_IsNullWorks_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _handler():
    pass

func test():
    var cb = _handler
    if not cb.is_null():
        cb.call()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("is_null")).ToList();

        methodNotFound.Should().BeEmpty(
            "is_null() should work on method reference");
    }

    #endregion

    #region Method Reference Passed to Connect - No GD4002

    [TestMethod]
    public void MethodReference_PassedToConnect_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

signal my_signal

func _on_signal():
    pass

func test():
    my_signal.connect(_on_signal)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound ||
            d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();

        signalDiagnostics.Should().BeEmpty(
            "Method reference passed to connect should work without errors");
    }

    [TestMethod]
    public void MethodReference_PassedToConnect_WithSignalParams_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

signal data_received(data: Dictionary)

func _on_data(data: Dictionary):
    print(data)

func test():
    data_received.connect(_on_data)
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        methodNotFound.Should().BeEmpty(
            "Method reference with matching signature should work with signal connect");
    }

    #endregion

    #region get_method() Returns Callable

    [TestMethod]
    public void GetMethod_ReturnsCallable_NoGD4002()
    {
        var code = @"
class_name Test
extends Node

func _handler():
    pass

func test():
    var cb = Callable(self, ""_handler"")
    cb.call()
";
        var diagnostics = ValidateCode(code);
        var methodNotFound = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.MethodNotFound &&
            d.Message.Contains("call")).ToList();

        methodNotFound.Should().BeEmpty(
            "Callable constructor should create valid Callable type");
    }

    #endregion

    #region Helper Methods

    private static string? InferVariableType(string code, string variableName)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return null;

        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Find variable declaration statement
        var varDecl = classDecl.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName);

        if (varDecl?.Initializer == null)
            return null;

        var typeNode = engine.InferTypeNode(varDecl.Initializer);
        return typeNode?.BuildName();
    }

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return Enumerable.Empty<GDDiagnostic>();

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
        // Validate scriptFile.Class (the AST the semantic model knows about)
        // NOT a separately parsed classDecl
        var result = validator.Validate(scriptFile.Class);

        return result.Diagnostics;
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
