using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level3_Methods;

/// <summary>
/// Tests for signal type inference with full parameter signatures.
/// When a signal is referenced without emit/await (e.g., var s = my_signal),
/// InferSemanticType returns GDSignalSemanticType with parameter signature.
/// InferTypeNode returns "Signal" (GDTypeNode can't represent signatures).
/// </summary>
[TestClass]
public class SignalTypeInferenceTests
{
    #region Typed Signal Parameters (via InferSemanticType)

    [TestMethod]
    public void Signal_WithTypedParams_InfersSignalWithSignature()
    {
        var code = @"
class_name Test
extends Node

signal my_signal(value: int, name: String)

func test():
    var s = my_signal
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(value: int, name: String)");
        type.Should().BeOfType<GDSignalSemanticType>();
    }

    [TestMethod]
    public void Signal_WithoutParams_InfersSimpleSignal()
    {
        var code = @"
class_name Test
extends Node

signal no_args

func test():
    var s = no_args
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal");
    }

    [TestMethod]
    public void Signal_SingleTypedParam_InfersSignalWithSignature()
    {
        var code = @"
class_name Test
extends Node

signal health_changed(new_health: float)

func test():
    var s = health_changed
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(new_health: float)");
    }

    [TestMethod]
    public void Signal_InferTypeNode_ReturnsSimpleSignal()
    {
        var code = @"
class_name Test
extends Node

signal my_signal(value: int, name: String)

func test():
    var s = my_signal
";
        var type = InferVariableTypeNode(code, "s");
        type.Should().Be("Signal",
            "InferTypeNode returns base type name; signature is in InferSemanticType");
    }

    #endregion

    #region Emit Call Site Inference

    [TestMethod]
    public void Signal_UntypedParams_InfersFromEmitCallSite()
    {
        var code = @"
class_name Test
extends Node

signal data_ready(value, label)

func test():
    data_ready.emit(42, ""hello"")
    var s = data_ready
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(value: int, label: String)");
    }

    [TestMethod]
    public void Signal_MixedParams_TypedAndUntypedFromEmit()
    {
        var code = @"
class_name Test
extends Node

signal mixed(id: int, data)

func test():
    mixed.emit(1, 3.14)
    var s = mixed
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(id: int, data: float)");
    }

    [TestMethod]
    public void Signal_NoEmitCallSites_UntypedParamsAreVariant()
    {
        var code = @"
class_name Test
extends Node

signal unknown(x, y)

func test():
    var s = unknown
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(x: Variant, y: Variant)");
    }

    [TestMethod]
    public void Signal_EmitInDifferentMethod_InfersType()
    {
        var code = @"
class_name Test
extends Node

signal progress(percent)

func update_progress():
    progress.emit(0.75)

func test():
    var s = progress
";
        var type = InferVariableSemanticType(code, "s");
        type.Should().NotBeNull();
        type!.DisplayName.Should().Be("Signal(percent: float)");
    }

    #endregion

    #region GDSignalSemanticType Unit Tests

    [TestMethod]
    public void SignalSemanticType_NoParams_DisplayNameIsSignal()
    {
        var signalType = new GDSignalSemanticType();
        signalType.DisplayName.Should().Be("Signal");
    }

    [TestMethod]
    public void SignalSemanticType_WithParams_DisplayNameIncludesSignature()
    {
        var paramTypes = new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        };
        var paramNames = new string[] { "value", "name" };
        var signalType = new GDSignalSemanticType(paramTypes, paramNames);

        signalType.DisplayName.Should().Be("Signal(value: int, name: String)");
    }

    [TestMethod]
    public void SignalSemanticType_WithParamsNoNames_DisplayNameOmitsNames()
    {
        var paramTypes = new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("float")
        };
        var signalType = new GDSignalSemanticType(paramTypes);

        signalType.DisplayName.Should().Be("Signal(int, float)");
    }

    [TestMethod]
    public void SignalSemanticType_IsAssignableTo_UntypedSignal()
    {
        var paramTypes = new GDSemanticType[] { new GDSimpleSemanticType("int") };
        var typed = new GDSignalSemanticType(paramTypes, new string[] { "x" });
        var untyped = new GDSimpleSemanticType("Signal");

        typed.IsAssignableTo(untyped, null).Should().BeTrue();
    }

    [TestMethod]
    public void SignalSemanticType_IsAssignableTo_Variant()
    {
        var paramTypes = new GDSemanticType[] { new GDSimpleSemanticType("int") };
        var typed = new GDSignalSemanticType(paramTypes, new string[] { "x" });

        typed.IsAssignableTo(GDVariantSemanticType.Instance, null).Should().BeTrue();
    }

    [TestMethod]
    public void SignalSemanticType_IsAssignableTo_UntypedSignalSemanticType()
    {
        var paramTypes = new GDSemanticType[] { new GDSimpleSemanticType("int") };
        var typed = new GDSignalSemanticType(paramTypes);
        var untyped = new GDSignalSemanticType();

        typed.IsAssignableTo(untyped, null).Should().BeTrue();
    }

    [TestMethod]
    public void SignalSemanticType_Equals_SameParams()
    {
        var a = new GDSignalSemanticType(
            new GDSemanticType[] { new GDSimpleSemanticType("int") },
            new string[] { "x" });
        var b = new GDSignalSemanticType(
            new GDSemanticType[] { new GDSimpleSemanticType("int") },
            new string[] { "y" });

        a.Equals(b).Should().BeTrue("Equality checks parameter types, not names");
    }

    [TestMethod]
    public void SignalSemanticType_Equals_DifferentParams()
    {
        var a = new GDSignalSemanticType(
            new GDSemanticType[] { new GDSimpleSemanticType("int") });
        var b = new GDSignalSemanticType(
            new GDSemanticType[] { new GDSimpleSemanticType("String") });

        a.Equals(b).Should().BeFalse();
    }

    [TestMethod]
    public void SignalSemanticType_IsSignal_True()
    {
        var signalType = new GDSignalSemanticType();
        signalType.IsSignal.Should().BeTrue();
    }

    [TestMethod]
    public void SignalSemanticType_IsValueType_True()
    {
        var signalType = new GDSignalSemanticType();
        signalType.IsValueType.Should().BeTrue();
    }

    #endregion

    #region GetSemanticTypeForNode

    [TestMethod]
    public void GetSemanticTypeForNode_SignalDecl_ReturnsGDSignalSemanticType()
    {
        var code = @"
class_name Test
extends Node

signal my_signal(value: int, name: String)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        var signalDecl = classDecl.AllNodes
            .OfType<GDSignalDeclaration>()
            .First();

        var semanticType = engine.GetSemanticTypeForNode(signalDecl);
        semanticType.Should().BeOfType<GDSignalSemanticType>();

        var signalType = (GDSignalSemanticType)semanticType;
        signalType.ParameterTypes.Should().HaveCount(2);
        signalType.ParameterNames.Should().HaveCount(2);
        signalType.DisplayName.Should().Be("Signal(value: int, name: String)");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticType? InferVariableSemanticType(string code, string variableName)
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

        var varDecl = classDecl.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName);

        if (varDecl?.Initializer == null)
            return null;

        return engine.InferSemanticType(varDecl.Initializer);
    }

    private static string? InferVariableTypeNode(string code, string variableName)
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

        var varDecl = classDecl.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName);

        if (varDecl?.Initializer == null)
            return null;

        var typeNode = engine.InferTypeNode(varDecl.Initializer);
        return typeNode?.BuildName();
    }

    #endregion
}
