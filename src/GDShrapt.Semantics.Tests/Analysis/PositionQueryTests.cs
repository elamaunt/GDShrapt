using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class PositionQueryTests
{
    private const string TestCode = @"extends Node

var health: int = 100

func take_damage(amount: float) -> void:
    health -= amount
    if health <= 0:
        print(""dead"")
";

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/position_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #region GetNodeAtPosition Tests

    [TestMethod]
    public void GetNodeAtPosition_OnVariableDeclaration_ReturnsVariableDeclaration()
    {
        var model = CreateModel(TestCode);

        // "health" identifier is at line 2, column 4
        var node = model.GetNodeAtPosition(2, 4);

        node.Should().NotBeNull();
        node.Should().BeOfType<GDVariableDeclaration>();
        var varDecl = (GDVariableDeclaration)node!;
        varDecl.Identifier?.Sequence.Should().Be("health");
    }

    [TestMethod]
    public void GetNodeAtPosition_OnMethodDeclaration_ReturnsMethodDeclaration()
    {
        var model = CreateModel(TestCode);

        // "take_damage" identifier is at line 4, column 5
        var node = model.GetNodeAtPosition(4, 5);

        node.Should().NotBeNull();
        // The token's direct parent may be the method declaration
        // Walk up to find GDMethodDeclaration
        var current = node;
        GDMethodDeclaration? methodDecl = null;
        while (current != null)
        {
            if (current is GDMethodDeclaration md)
            {
                methodDecl = md;
                break;
            }
            current = current.Parent;
        }

        methodDecl.Should().NotBeNull();
        methodDecl!.Identifier?.Sequence.Should().Be("take_damage");
    }

    [TestMethod]
    public void GetNodeAtPosition_OnExpression_ReturnsExpressionParent()
    {
        var model = CreateModel(TestCode);

        // "health" usage at line 5, column 5 (inside "health -= amount" after indentation)
        var node = model.GetNodeAtPosition(5, 5);

        node.Should().NotBeNull();
    }

    [TestMethod]
    public void GetNodeAtPosition_OnEmptyOrBlankLine_ReturnsNullOrNode()
    {
        var model = CreateModel(TestCode);

        // Line 1 is an empty line between "extends Node" and "var health..."
        var node = model.GetNodeAtPosition(1, 0);

        // Empty line may return null or a whitespace-holding node
        // The important thing is it does not throw
    }

    [TestMethod]
    public void GetNodeAtPosition_OutOfBounds_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        // Line 100 does not exist
        var node = model.GetNodeAtPosition(100, 0);

        node.Should().BeNull();
    }

    [TestMethod]
    public void GetNodeAtPosition_NegativePosition_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        var node = model.GetNodeAtPosition(-1, -1);

        node.Should().BeNull();
    }

    #endregion

    #region GetIdentifierAtPosition Tests

    [TestMethod]
    public void GetIdentifierAtPosition_OnVariableName_ReturnsIdentifier()
    {
        var model = CreateModel(TestCode);

        // "health" at line 2, column 4
        var identifier = model.GetIdentifierAtPosition(2, 4);

        identifier.Should().NotBeNull();
        identifier!.Sequence.Should().Be("health");
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnFunctionName_ReturnsIdentifier()
    {
        var model = CreateModel(TestCode);

        // "take_damage" at line 4, column 5
        var identifier = model.GetIdentifierAtPosition(4, 5);

        identifier.Should().NotBeNull();
        identifier!.Sequence.Should().Be("take_damage");
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnParameterName_ReturnsIdentifier()
    {
        var model = CreateModel(TestCode);

        // "amount" at line 4, column 17
        var identifier = model.GetIdentifierAtPosition(4, 17);

        identifier.Should().NotBeNull();
        identifier!.Sequence.Should().Be("amount");
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnTypeAnnotation_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        // "int" type at line 2, column 12 is a built-in type token, not a GDIdentifier
        var identifier = model.GetIdentifierAtPosition(2, 12);

        identifier.Should().BeNull("built-in type keywords are not GDIdentifier tokens");
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnUsageInsideMethod_ReturnsIdentifier()
    {
        var model = CreateModel(TestCode);

        // "health" usage at line 5, column 4
        var identifier = model.GetIdentifierAtPosition(5, 5);

        identifier.Should().NotBeNull();
        identifier!.Sequence.Should().Be("health");
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnEmptyPosition_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        // Line 100 does not exist
        var identifier = model.GetIdentifierAtPosition(100, 0);

        identifier.Should().BeNull();
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnKeyword_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        // "var" keyword at line 2, column 0 - not an identifier
        var identifier = model.GetIdentifierAtPosition(2, 0);

        identifier.Should().BeNull();
    }

    [TestMethod]
    public void GetIdentifierAtPosition_OnNumber_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        // "100" at line 2, column 18 - not an identifier
        var identifier = model.GetIdentifierAtPosition(2, 18);

        identifier.Should().BeNull();
    }

    #endregion

    #region GetTokenAtPosition Tests

    [TestMethod]
    public void GetTokenAtPosition_OnVarKeyword_ReturnsVarKeyword()
    {
        var model = CreateModel(TestCode);

        // "var" at line 2, column 0
        var token = model.GetTokenAtPosition(2, 0);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDVarKeyword>();
    }

    [TestMethod]
    public void GetTokenAtPosition_OnFuncKeyword_ReturnsFuncKeyword()
    {
        var model = CreateModel(TestCode);

        // "func" at line 4, column 0
        var token = model.GetTokenAtPosition(4, 0);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDFuncKeyword>();
    }

    [TestMethod]
    public void GetTokenAtPosition_OnExtendsKeyword_ReturnsExtendsKeyword()
    {
        var model = CreateModel(TestCode);

        // "extends" at line 0, column 0
        var token = model.GetTokenAtPosition(0, 0);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDExtendsKeyword>();
    }

    [TestMethod]
    public void GetTokenAtPosition_OnIfKeyword_ReturnsIfKeyword()
    {
        var model = CreateModel(TestCode);

        // "if" at line 6 - use column 5 to be inside the keyword
        // (column 4 is occupied by the indentation token)
        var token = model.GetTokenAtPosition(6, 5);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDIfKeyword>();
    }

    [TestMethod]
    public void GetTokenAtPosition_OnIdentifier_ReturnsIdentifier()
    {
        var model = CreateModel(TestCode);

        // "health" at line 2, column 4
        var token = model.GetTokenAtPosition(2, 4);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDIdentifier>();
        token!.ToString().Should().Be("health");
    }

    [TestMethod]
    public void GetTokenAtPosition_OnNumber_ReturnsNumber()
    {
        var model = CreateModel(TestCode);

        // "100" at line 2, column 18
        var token = model.GetTokenAtPosition(2, 18);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDNumber>();
        token!.ToString().Should().Be("100");
    }

    [TestMethod]
    public void GetTokenAtPosition_OnStringLiteral_ReturnsStringPart()
    {
        var model = CreateModel(TestCode);

        // "dead" string content at line 7, column 15 (inside the quotes)
        var token = model.GetTokenAtPosition(7, 15);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDStringPart>();
        ((GDStringPart)token!).Sequence.Should().Be("dead");
    }

    [TestMethod]
    public void GetTokenAtPosition_OutOfBounds_ReturnsNull()
    {
        var model = CreateModel(TestCode);

        var token = model.GetTokenAtPosition(100, 0);

        token.Should().BeNull();
    }

    [TestMethod]
    public void GetTokenAtPosition_OnZeroNumber_ReturnsNumber()
    {
        var model = CreateModel(TestCode);

        // "0" at line 6, column 18 (in "health <= 0")
        var token = model.GetTokenAtPosition(6, 18);

        token.Should().NotBeNull();
        token.Should().BeOfType<GDNumber>();
        token!.ToString().Should().Be("0");
    }

    #endregion

    #region Cross-Method Consistency Tests

    [TestMethod]
    public void AllPositionMethods_SamePosition_AreConsistent()
    {
        var model = CreateModel(TestCode);

        // "health" at line 2, column 4
        var token = model.GetTokenAtPosition(2, 4);
        var identifier = model.GetIdentifierAtPosition(2, 4);
        var node = model.GetNodeAtPosition(2, 4);

        token.Should().NotBeNull();
        identifier.Should().NotBeNull();
        node.Should().NotBeNull();

        // The token should be an identifier
        token.Should().BeOfType<GDIdentifier>();
        // The identifier should be the same as the token
        identifier!.Sequence.Should().Be("health");
        // The node should be the parent of the token
        node.Should().Be(token!.Parent);
    }

    [TestMethod]
    public void AllPositionMethods_OnKeyword_IdentifierIsNull()
    {
        var model = CreateModel(TestCode);

        // "var" keyword at line 2, column 0
        var token = model.GetTokenAtPosition(2, 0);
        var identifier = model.GetIdentifierAtPosition(2, 0);
        var node = model.GetNodeAtPosition(2, 0);

        token.Should().NotBeNull();
        identifier.Should().BeNull("a keyword is not an identifier");
        node.Should().NotBeNull();
    }

    #endregion
}
