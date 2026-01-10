using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Context;

[TestClass]
public class GDRefactoringContextTests
{
    private readonly GDScriptReader _reader = new();

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    [TestMethod]
    public void Constructor_SetsBasicProperties()
    {
        var code = @"extends Node
var x = 10
";
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(1, 5);
        var selection = GDSelectionInfo.None;

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            selection);

        Assert.AreEqual(script, context.Script);
        Assert.AreEqual(cursor, context.Cursor);
        Assert.AreEqual(selection, context.Selection);
        Assert.IsNotNull(context.ClassDeclaration);
    }

    [TestMethod]
    public void IsOnIdentifier_OnIdentifier_ReturnsTrue()
    {
        var code = @"extends Node
var player = 10
";
        var (script, classDecl) = CreateScript(code);
        // "player" is at line 1, column 4
        var cursor = new GDCursorPosition(1, 5);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsTrue(context.IsOnIdentifier);
        Assert.IsNotNull(context.IdentifierAtCursor);
        Assert.AreEqual("player", context.IdentifierAtCursor.Sequence);
    }

    [TestMethod]
    public void IsOnNumber_OnNumberLiteral_ReturnsTrue()
    {
        var code = @"extends Node
var x = 42
";
        var (script, classDecl) = CreateScript(code);
        // "42" is at line 1, column 8
        var cursor = new GDCursorPosition(1, 8);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsTrue(context.IsOnNumber);
        Assert.IsTrue(context.IsOnLiteral);
    }

    [TestMethod]
    public void ContainingMethod_InsideMethod_ReturnsMethod()
    {
        var code = @"extends Node
func my_method():
    var x = 10
";
        var (script, classDecl) = CreateScript(code);
        // Inside method body
        var cursor = new GDCursorPosition(2, 8);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsNotNull(context.ContainingMethod);
        Assert.AreEqual("my_method", context.ContainingMethod.Identifier?.Sequence);
    }

    [TestMethod]
    public void ContainingMethod_OutsideMethod_ReturnsNull()
    {
        var code = @"extends Node
var x = 10
func my_method():
    pass
";
        var (script, classDecl) = CreateScript(code);
        // On class variable, not inside method
        var cursor = new GDCursorPosition(1, 5);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsNull(context.ContainingMethod);
    }

    [TestMethod]
    public void IsOnClassVariable_ClassLevel_ReturnsTrue()
    {
        var code = @"extends Node
var class_var = 10
func test():
    pass
";
        var (script, classDecl) = CreateScript(code);
        // On "class_var" at line 1
        var cursor = new GDCursorPosition(1, 6);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsTrue(context.IsOnClassVariable);
    }

    [TestMethod]
    public void IsOnClassVariable_LocalVariable_ReturnsFalse()
    {
        var code = @"extends Node
func test():
    var local_var = 10
";
        var (script, classDecl) = CreateScript(code);
        // On "local_var" inside method
        var cursor = new GDCursorPosition(2, 9);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsFalse(context.IsOnClassVariable);
    }

    [TestMethod]
    public void HasSelection_WithSelection_ReturnsTrue()
    {
        var code = @"extends Node
var x = 10 + 20
";
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(1, 8);
        var selection = new GDSelectionInfo(1, 8, 1, 15, "10 + 20");

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            selection);

        Assert.IsTrue(context.HasSelection);
    }

    [TestMethod]
    public void IsOnIfStatement_OnIfKeyword_ReturnsTrue()
    {
        var code = @"extends Node
func test():
    if x > 10:
        pass
";
        var (script, classDecl) = CreateScript(code);
        // On "if" at line 2
        var cursor = new GDCursorPosition(2, 6);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsTrue(context.IsOnIfStatement);
    }

    [TestMethod]
    public void IsOnForStatement_OnForKeyword_ReturnsTrue()
    {
        var code = @"extends Node
func test():
    for i in range(10):
        pass
";
        var (script, classDecl) = CreateScript(code);
        // On "for" at line 2
        var cursor = new GDCursorPosition(2, 6);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        Assert.IsTrue(context.IsOnForStatement);
    }

    [TestMethod]
    public void GetVariableDeclaration_OnVariable_ReturnsDeclaration()
    {
        var code = @"extends Node
var x = 10
";
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(1, 5);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        var varDecl = context.GetVariableDeclaration();
        Assert.IsNotNull(varDecl);
        Assert.AreEqual("x", varDecl.Identifier?.Sequence);
    }

    [TestMethod]
    public void FindParent_FindsParentOfType()
    {
        var code = @"extends Node
func test():
    if x > 10:
        var y = 20
";
        var (script, classDecl) = CreateScript(code);
        // On "y" inside if statement
        var cursor = new GDCursorPosition(3, 13);

        var context = new GDRefactoringContext(
            script,
            classDecl,
            cursor,
            GDSelectionInfo.None);

        var ifStmt = context.FindParent<GDIfStatement>();
        Assert.IsNotNull(ifStmt);
    }
}
