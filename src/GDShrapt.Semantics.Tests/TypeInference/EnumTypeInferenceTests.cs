using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference;

[TestClass]
public class EnumTypeInferenceTests
{
    private const string TestCode = @"extends Node

enum AIState { IDLE, PATROL, CHASE, ATTACK }
enum Priority { LOW = 0, MEDIUM = 5, HIGH = 10 }

var current_state: AIState = AIState.IDLE

func test():
    var state = AIState.PATROL
    var p = Priority.HIGH
";

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/enum_type_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #region Symbol-Level Type

    [TestMethod]
    public void EnumValue_Symbol_HasEnumTypeName()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("IDLE");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.EnumValue);
        symbol.TypeName.Should().Be("AIState");
    }

    [TestMethod]
    public void EnumValue_ExplicitValue_HasEnumTypeName()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("HIGH");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.EnumValue);
        symbol.TypeName.Should().Be("Priority");
    }

    [TestMethod]
    public void Enum_Symbol_HasEnumKind()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("AIState");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Enum);
    }

    #endregion

    #region TypeSystem.GetTypeInfo

    [TestMethod]
    public void EnumValue_GetTypeInfo_ReturnsEnumType()
    {
        var model = CreateModel(TestCode);

        var typeInfo = model.TypeSystem.GetTypeInfo("IDLE");

        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("AIState");
        typeInfo.Confidence.Should().Be(GDTypeConfidence.Certain);
    }

    [TestMethod]
    public void EnumValue_ExplicitValue_GetTypeInfo_ReturnsEnumType()
    {
        var model = CreateModel(TestCode);

        var typeInfo = model.TypeSystem.GetTypeInfo("HIGH");

        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be("Priority");
        typeInfo.Confidence.Should().Be(GDTypeConfidence.Certain);
    }

    #endregion

    #region TypeSystem.GetType (Inference Engine)

    [TestMethod]
    public void EnumValueDeclaration_InferredType_IsEnumName()
    {
        var model = CreateModel(TestCode);

        var enumValueDecl = model.ScriptFile.Class!.AllNodes
            .OfType<GDEnumValueDeclaration>()
            .First(v => v.Identifier?.Sequence == "IDLE");

        var type = model.TypeSystem.GetType(enumValueDecl);

        type.Should().NotBeNull();
        type.DisplayName.Should().Be("AIState");
    }

    [TestMethod]
    public void EnumMemberAccess_InferredType_IsEnumName()
    {
        var model = CreateModel(TestCode);

        var memberAccess = model.ScriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .First(m => m.Identifier?.Sequence == "PATROL");

        var type = model.TypeSystem.GetType(memberAccess);

        type.Should().NotBeNull();
        type.DisplayName.Should().Be("AIState");
    }

    #endregion

    #region Both Paths Agree

    [TestMethod]
    public void EnumValue_TypeInfo_And_TypeInference_Agree()
    {
        var model = CreateModel(TestCode);

        var typeInfo = model.TypeSystem.GetTypeInfo("IDLE");
        var enumValueDecl = model.ScriptFile.Class!.AllNodes
            .OfType<GDEnumValueDeclaration>()
            .First(v => v.Identifier?.Sequence == "IDLE");
        var inferredType = model.TypeSystem.GetType(enumValueDecl);

        typeInfo.Should().NotBeNull();
        typeInfo!.InferredType.Should().NotBeNull();
        typeInfo.InferredType!.DisplayName.Should().Be(inferredType.DisplayName,
            "TYPE_INFO and TYPE_INFERENCE must agree on enum value type");
    }

    #endregion

}
