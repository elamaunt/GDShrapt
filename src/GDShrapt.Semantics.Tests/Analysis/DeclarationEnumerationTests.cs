using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class DeclarationEnumerationTests
{
    private const string TestCode = @"
class_name MyClass
extends Node

enum Direction { UP, DOWN, LEFT, RIGHT }

enum State {
    IDLE,
    RUNNING,
    JUMPING,
}

class InnerHelper:
    var value: int = 0
    func helper_method():
        pass

class AnotherInner:
    var name: String = """"

var counter: int = 0
signal health_changed(new_health: int)

func process():
    pass
";

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/decl_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #region GetEnums

    [TestMethod]
    public void GetEnums_ReturnsAllEnumSymbols()
    {
        var model = CreateModel(TestCode);

        var enums = model.GetEnums().ToList();

        enums.Should().HaveCount(2);
    }

    [TestMethod]
    public void GetEnums_ContainsDirectionEnum()
    {
        var model = CreateModel(TestCode);

        var enums = model.GetEnums().ToList();

        enums.Should().Contain(e => e.Name == "Direction");
    }

    [TestMethod]
    public void GetEnums_ContainsStateEnum()
    {
        var model = CreateModel(TestCode);

        var enums = model.GetEnums().ToList();

        enums.Should().Contain(e => e.Name == "State");
    }

    [TestMethod]
    public void GetEnums_AllHaveEnumKind()
    {
        var model = CreateModel(TestCode);

        var enums = model.GetEnums().ToList();

        enums.Should().OnlyContain(e => e.Kind == GDSymbolKind.Enum);
    }

    [TestMethod]
    public void GetEnums_EmptyScript_ReturnsEmpty()
    {
        var model = CreateModel(@"
extends Node

func process():
    pass
");

        var enums = model.GetEnums().ToList();

        enums.Should().BeEmpty();
    }

    #endregion

    #region EnumValues

    [TestMethod]
    public void FindSymbol_EnumValue_ReturnsByName()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("UP");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.EnumValue);
        symbol.Name.Should().Be("UP");
    }

    [TestMethod]
    public void FindSymbol_AllEnumValues_Found()
    {
        var model = CreateModel(TestCode);

        model.FindSymbol("UP").Should().NotBeNull();
        model.FindSymbol("DOWN").Should().NotBeNull();
        model.FindSymbol("LEFT").Should().NotBeNull();
        model.FindSymbol("RIGHT").Should().NotBeNull();
        model.FindSymbol("IDLE").Should().NotBeNull();
        model.FindSymbol("RUNNING").Should().NotBeNull();
        model.FindSymbol("JUMPING").Should().NotBeNull();
    }

    [TestMethod]
    public void GetSymbolsOfKind_EnumValue_ReturnsAll()
    {
        var model = CreateModel(TestCode);

        var values = model.GetSymbolsOfKind(GDSymbolKind.EnumValue);

        values.Should().HaveCount(7);
    }

    [TestMethod]
    public void EnumValue_DeclaringScopeNode_IsEnumDeclaration()
    {
        var model = CreateModel(TestCode);

        var upSymbol = model.FindSymbol("UP");
        var directionSymbol = model.FindSymbol("Direction");

        upSymbol.Should().NotBeNull();
        directionSymbol.Should().NotBeNull();
        upSymbol!.DeclaringScopeNode.Should().Be(directionSymbol!.DeclarationNode);
    }

    [TestMethod]
    public void EnumValues_FilterByEnum_WorksCorrectly()
    {
        var model = CreateModel(TestCode);
        var directionSymbol = model.FindSymbol("Direction");

        var directionValues = model.GetSymbolsOfKind(GDSymbolKind.EnumValue)
            .Where(ev => ev.DeclaringScopeNode == directionSymbol!.DeclarationNode)
            .Select(ev => ev.Name)
            .ToList();

        directionValues.Should().BeEquivalentTo("UP", "DOWN", "LEFT", "RIGHT");
    }

    [TestMethod]
    public void EnumValue_HasCorrectDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var upSymbol = model.FindSymbol("UP");

        upSymbol.Should().NotBeNull();
        upSymbol!.DeclarationNode.Should().NotBeNull();
        upSymbol.DeclarationNode.Should().BeOfType<GDEnumValueDeclaration>();
    }

    [TestMethod]
    public void GetSymbolsOfKind_EnumValue_EmptyScript_ReturnsEmpty()
    {
        var model = CreateModel(@"
extends Node

func process():
    pass
");

        model.GetSymbolsOfKind(GDSymbolKind.EnumValue).Should().BeEmpty();
    }

    #endregion

    #region GetInnerClasses

    [TestMethod]
    public void GetInnerClasses_ReturnsAllInnerClassSymbols()
    {
        var model = CreateModel(TestCode);

        var innerClasses = model.GetInnerClasses().ToList();

        innerClasses.Should().HaveCount(2);
    }

    [TestMethod]
    public void GetInnerClasses_ContainsInnerHelper()
    {
        var model = CreateModel(TestCode);

        var innerClasses = model.GetInnerClasses().ToList();

        innerClasses.Should().Contain(c => c.Name == "InnerHelper");
    }

    [TestMethod]
    public void GetInnerClasses_ContainsAnotherInner()
    {
        var model = CreateModel(TestCode);

        var innerClasses = model.GetInnerClasses().ToList();

        innerClasses.Should().Contain(c => c.Name == "AnotherInner");
    }

    [TestMethod]
    public void GetInnerClasses_AllHaveClassKind()
    {
        var model = CreateModel(TestCode);

        var innerClasses = model.GetInnerClasses().ToList();

        innerClasses.Should().OnlyContain(c => c.Kind == GDSymbolKind.Class);
    }

    [TestMethod]
    public void GetInnerClasses_EmptyScript_ReturnsEmpty()
    {
        var model = CreateModel(@"
extends Node

func process():
    pass
");

        var innerClasses = model.GetInnerClasses().ToList();

        innerClasses.Should().BeEmpty();
    }

    #endregion

    #region GetDeclaration

    [TestMethod]
    public void GetDeclaration_ForVariable_ReturnsDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("counter");

        declNode.Should().NotBeNull();
        declNode.Should().BeOfType<GDVariableDeclaration>();
    }

    [TestMethod]
    public void GetDeclaration_ForMethod_ReturnsDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("process");

        declNode.Should().NotBeNull();
        declNode.Should().BeOfType<GDMethodDeclaration>();
    }

    [TestMethod]
    public void GetDeclaration_ForSignal_ReturnsDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("health_changed");

        declNode.Should().NotBeNull();
        declNode.Should().BeOfType<GDSignalDeclaration>();
    }

    [TestMethod]
    public void GetDeclaration_ForEnum_ReturnsDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("Direction");

        declNode.Should().NotBeNull();
        declNode.Should().BeOfType<GDEnumDeclaration>();
    }

    [TestMethod]
    public void GetDeclaration_ForInnerClass_ReturnsDeclarationNode()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("InnerHelper");

        declNode.Should().NotBeNull();
        declNode.Should().BeOfType<GDInnerClassDeclaration>();
    }

    [TestMethod]
    public void GetDeclaration_ForNonExistentSymbol_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        var declNode = model.GetDeclaration("nonexistent_symbol");

        declNode.Should().BeNull();
    }

    #endregion

    #region ResolveStandaloneExpression

    [TestMethod]
    public void ResolveStandaloneExpression_KnownIdentifier_ResolvesToSymbolType()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("counter");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.IsResolved.Should().BeTrue();
        result.TypeName.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void ResolveStandaloneExpression_UnknownIdentifier_ReturnsUnresolved()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("unknown_var");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.IsResolved.Should().BeFalse();
        result.TypeName.DisplayName.Should().Be("Variant");
    }

    [TestMethod]
    public void ResolveStandaloneExpression_LiteralExpression_ReturnsResult()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("42");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
    }

    [TestMethod]
    public void ResolveStandaloneExpression_BoolLiteral_ReturnsResult()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("true");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
    }

    #endregion
}
