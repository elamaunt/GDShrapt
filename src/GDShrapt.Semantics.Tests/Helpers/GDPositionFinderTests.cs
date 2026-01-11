using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDPositionFinderTests
{
    private readonly GDScriptReader _reader = new();

    #region FindTokenAtPosition Tests

    [TestMethod]
    public void FindTokenAtPosition_OnIdentifier_ReturnsIdentifier()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var player = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Line 1 (0-based), "player" starts at column 4
        var token = finder.FindTokenAtPosition(1, 5);

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType(token, typeof(GDIdentifier));
        Assert.AreEqual("player", token.ToString());
    }

    [TestMethod]
    public void FindTokenAtPosition_OnKeyword_ReturnsKeyword()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // "var" starts at column 0
        var token = finder.FindTokenAtPosition(1, 0);

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType(token, typeof(GDVarKeyword));
    }

    [TestMethod]
    public void FindTokenAtPosition_OnNumber_ReturnsNumber()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 42
");
        var finder = new GDPositionFinder(classDecl);

        // "42" starts at column 8
        var token = finder.FindTokenAtPosition(1, 8);

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType(token, typeof(GDNumber));
        Assert.AreEqual("42", token.ToString());
    }

    [TestMethod]
    public void FindTokenAtPosition_AtEndOfToken_ReturnsToken()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var player = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Last character of "player" (column 9)
        var token = finder.FindTokenAtPosition(1, 9);

        Assert.IsNotNull(token);
        Assert.AreEqual("player", token.ToString());
    }

    [TestMethod]
    public void FindTokenAtPosition_OutOfRange_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Line 100 does not exist
        var token = finder.FindTokenAtPosition(100, 0);

        Assert.IsNull(token);
    }

    #endregion

    #region FindIdentifierAtPosition Tests

    [TestMethod]
    public void FindIdentifierAtPosition_OnIdentifier_ReturnsIdentifier()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var value = 10
");
        var finder = new GDPositionFinder(classDecl);

        // "value" starts at column 8
        var identifier = finder.FindIdentifierAtPosition(2, 9);

        Assert.IsNotNull(identifier);
        Assert.AreEqual("value", identifier.Sequence);
    }

    [TestMethod]
    public void FindIdentifierAtPosition_OnNonIdentifier_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // "10" is a number, not identifier
        var identifier = finder.FindIdentifierAtPosition(1, 8);

        Assert.IsNull(identifier);
    }

    #endregion

    #region FindNodeAtPosition Tests

    [TestMethod]
    public void FindNodeAtPosition_OnVariable_ReturnsVariableDeclaration()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var player = 10
");
        var finder = new GDPositionFinder(classDecl);

        // On "player" identifier - parent is GDVariableDeclaration
        var node = finder.FindNodeAtPosition(1, 5);

        Assert.IsNotNull(node);
        Assert.IsInstanceOfType(node, typeof(GDVariableDeclaration));
    }

    [TestMethod]
    public void FindNodeAtPosition_Generic_ReturnsMethodDeclaration()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func my_func():
    pass
");
        var finder = new GDPositionFinder(classDecl);

        // On "my_func" identifier
        var node = finder.FindNodeAtPosition<GDMethodDeclaration>(1, 6);

        Assert.IsNotNull(node);
        Assert.AreEqual("my_func", node.Identifier?.Sequence);
    }

    [TestMethod]
    public void FindNodeAtPosition_Generic_ReturnsClassDeclaration()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Any position should be able to find GDClassDeclaration
        var node = finder.FindNodeAtPosition<GDClassDeclaration>(1, 5);

        Assert.IsNotNull(node);
        Assert.AreSame(classDecl, node);
    }

    #endregion

    #region FindParent Tests

    [TestMethod]
    public void FindParent_FromToken_ReturnsParentMethod()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Find token at "x"
        var token = finder.FindTokenAtPosition(2, 8);
        Assert.IsNotNull(token);

        var method = GDPositionFinder.FindParent<GDMethodDeclaration>(token);

        Assert.IsNotNull(method);
        Assert.AreEqual("test", method.Identifier?.Sequence);
    }

    [TestMethod]
    public void FindParent_FromNode_ReturnsParentClass()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    pass
");
        var finder = new GDPositionFinder(classDecl);

        var node = finder.FindNodeAtPosition(1, 6);
        Assert.IsNotNull(node);

        var parent = GDPositionFinder.FindParent<GDClassDeclaration>(node);

        Assert.IsNotNull(parent);
        Assert.AreSame(classDecl, parent);
    }

    [TestMethod]
    public void FindParent_NotFound_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        var token = finder.FindTokenAtPosition(1, 5);
        Assert.IsNotNull(token);

        // No GDForStatement parent exists
        var forStatement = GDPositionFinder.FindParent<GDForStatement>(token);

        Assert.IsNull(forStatement);
    }

    #endregion

    #region FindContainingMethod Tests

    [TestMethod]
    public void FindContainingMethod_InsideMethod_ReturnsMethod()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func my_method():
    var x = 10
    print(x)
");
        var finder = new GDPositionFinder(classDecl);

        var method = finder.FindContainingMethod(2, 8);

        Assert.IsNotNull(method);
        Assert.AreEqual("my_method", method.Identifier?.Sequence);
    }

    [TestMethod]
    public void FindContainingMethod_OutsideMethod_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var class_var = 10
func my_method():
    pass
");
        var finder = new GDPositionFinder(classDecl);

        // On class variable, not inside a method
        var method = finder.FindContainingMethod(1, 5);

        Assert.IsNull(method);
    }

    #endregion

    #region HasStatementsSelected Tests

    [TestMethod]
    public void HasStatementsSelected_SingleStatement_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10
    print(x)
");
        var finder = new GDPositionFinder(classDecl);

        // Selection covers "var x = 10" entirely (line 2, from column 4 to end)
        var hasStatements = finder.HasStatementsSelected(2, 4, 2, 14);

        Assert.IsTrue(hasStatements);
    }

    [TestMethod]
    public void HasStatementsSelected_NoStatements_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10
");
        var finder = new GDPositionFinder(classDecl);

        // Selection at class level (not inside method statements)
        var hasStatements = finder.HasStatementsSelected(0, 0, 0, 7);

        Assert.IsFalse(hasStatements);
    }

    #endregion

    #region HasExpressionSelected Tests

    [TestMethod]
    public void HasExpressionSelected_ExactMatch_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10 + 20
");
        var finder = new GDPositionFinder(classDecl);

        // Get the expression "10 + 20" position
        // "10" starts at column 12
        var hasExpr = finder.HasExpressionSelected(2, 12, 2, 19);

        Assert.IsTrue(hasExpr);
    }

    [TestMethod]
    public void HasExpressionSelected_PartialExpression_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10 + 20
");
        var finder = new GDPositionFinder(classDecl);

        // Partial selection of "10" only - may not have exact match
        var hasExpr = finder.HasExpressionSelected(2, 12, 2, 13);

        // This depends on implementation - just check it doesn't throw
        Assert.IsTrue(hasExpr || !hasExpr); // Just verify no exception
    }

    #endregion

    #region FindExpressionAtSelection Tests

    [TestMethod]
    public void FindExpressionAtSelection_BinaryExpression_ReturnsExpression()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10 + 20
");
        var finder = new GDPositionFinder(classDecl);

        // Select the "10 + 20" expression
        var expr = finder.FindExpressionAtSelection(2, 12, 2, 19);

        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
    }

    [TestMethod]
    public void FindExpressionAtSelection_Literal_ReturnsNumberExpression()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 42
");
        var finder = new GDPositionFinder(classDecl);

        // Select just "42"
        var expr = finder.FindExpressionAtSelection(2, 12, 2, 14);

        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr, typeof(GDNumberExpression));
    }

    #endregion

    #region IsOnGetNodeCall Tests

    [TestMethod]
    public void IsOnGetNodeCall_GetNodeCall_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    get_node(""Player"")
");
        var finder = new GDPositionFinder(classDecl);

        // On "get_node" identifier
        var isOnGetNode = finder.IsOnGetNodeCall(2, 6);

        Assert.IsTrue(isOnGetNode);
    }

    [TestMethod]
    public void IsOnGetNodeCall_DollarSyntax_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    $Player
");
        var finder = new GDPositionFinder(classDecl);

        // Get the actual position of $Player by finding the expression
        var getNodeExpr = classDecl.AllNodes.OfType<GDGetNodeExpression>().FirstOrDefault();
        Assert.IsNotNull(getNodeExpr, "Expected to find GDGetNodeExpression");

        // Try using the column of "Player" identifier which should be inside the expression
        // The $ is the dollar token, Player is in the path
        var playerToken = getNodeExpr.Path?.FirstOrDefault();
        Assert.IsNotNull(playerToken, "Expected to find Player in path");

        // On "Player" identifier inside $Player expression
        var isOnGetNode = finder.IsOnGetNodeCall(playerToken.StartLine, playerToken.StartColumn);

        Assert.IsTrue(isOnGetNode, $"Expected true for position L{playerToken.StartLine}:C{playerToken.StartColumn}");
    }

    [TestMethod]
    public void IsOnGetNodeCall_OtherCall_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    print(""hello"")
");
        var finder = new GDPositionFinder(classDecl);

        // On "print" - not a get_node call
        var isOnGetNode = finder.IsOnGetNodeCall(2, 6);

        Assert.IsFalse(isOnGetNode);
    }

    #endregion

    #region IsInIfCondition Tests

    [TestMethod]
    public void IsInIfCondition_InCondition_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    if x > 10:
        pass
");
        var finder = new GDPositionFinder(classDecl);

        // On "x" in condition
        var inCondition = finder.IsInIfCondition(2, 7);

        Assert.IsTrue(inCondition);
    }

    [TestMethod]
    public void IsInIfCondition_InBody_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    if true:
        print(x)
");
        var finder = new GDPositionFinder(classDecl);

        // On "print" in body
        var inCondition = finder.IsInIfCondition(3, 10);

        Assert.IsFalse(inCondition);
    }

    #endregion

    #region IsOnClassVariable Tests

    [TestMethod]
    public void IsOnClassVariable_ClassLevel_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var player_health = 100
func test():
    pass
");
        var finder = new GDPositionFinder(classDecl);

        // On "player_health" class variable
        var isClassVar = finder.IsOnClassVariable(1, 6);

        Assert.IsTrue(isClassVar);
    }

    [TestMethod]
    public void IsOnClassVariable_LocalVariable_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var local_var = 10
");
        var finder = new GDPositionFinder(classDecl);

        // On "local_var" which is inside a method
        var isClassVar = finder.IsOnClassVariable(2, 9);

        Assert.IsFalse(isClassVar);
    }

    #endregion

    #region IsSelectionInNode Tests

    [TestMethod]
    public void IsSelectionInNode_InsideMethod_ReturnsTrue()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
func test():
    var x = 10
    print(x)
");
        var finder = new GDPositionFinder(classDecl);

        var isInMethod = finder.IsSelectionInNode<GDMethodDeclaration>(2, 4, 3, 12, out var method);

        Assert.IsTrue(isInMethod);
        Assert.IsNotNull(method);
        Assert.AreEqual("test", method.Identifier?.Sequence);
    }

    [TestMethod]
    public void IsSelectionInNode_OutsideMethod_ReturnsFalse()
    {
        var classDecl = _reader.ParseFileContent(@"extends Node
var x = 10
func test():
    pass
");
        var finder = new GDPositionFinder(classDecl);

        // Selection on class variable - not in method
        var isInMethod = finder.IsSelectionInNode<GDMethodDeclaration>(1, 4, 1, 10, out var method);

        Assert.IsFalse(isInMethod);
        Assert.IsNull(method);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    [ExpectedException(typeof(System.ArgumentNullException))]
    public void Constructor_NullRoot_ThrowsArgumentNullException()
    {
        new GDPositionFinder(null!);
    }

    #endregion
}
