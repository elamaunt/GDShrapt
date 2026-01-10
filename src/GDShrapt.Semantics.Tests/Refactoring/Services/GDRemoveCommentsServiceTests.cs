using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Services;

[TestClass]
public class GDRemoveCommentsServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDRemoveCommentsService _service = new();

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    private GDRefactoringContext CreateContext(string code)
    {
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(0, 0);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_WithComments_ReturnsTrue()
    {
        var code = @"extends Node
# This is a comment
var x = 10
";
        var context = CreateContext(code);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_WithoutComments_ReturnsFalse()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code);

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region GetCommentCount Tests

    [TestMethod]
    public void GetCommentCount_MultipleComments_ReturnsCorrectCount()
    {
        var code = @"extends Node
# Comment 1
var x = 10
# Comment 2
func test():
	# Comment 3
	pass
";
        var context = CreateContext(code);

        var count = _service.GetCommentCount(context);

        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public void GetCommentCount_NoComments_ReturnsZero()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code);

        var count = _service.GetCommentCount(context);

        Assert.AreEqual(0, count);
    }

    #endregion

    #region GetCommentCountInRange Tests

    [TestMethod]
    public void GetCommentCountInRange_CommentsInRange_ReturnsCorrectCount()
    {
        var code = @"extends Node
# Comment 1
var x = 10
# Comment 2
# Comment 3
func test():
	pass
";
        var context = CreateContext(code);

        // Range from line 3 to 4 (0-based)
        var count = _service.GetCommentCountInRange(context, 3, 4);

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void GetCommentCountInRange_NoCommentsInRange_ReturnsZero()
    {
        var code = @"extends Node
# Comment 1
var x = 10
func test():
	pass
";
        var context = CreateContext(code);

        // Range from line 3 to 4 (no comments)
        var count = _service.GetCommentCountInRange(context, 3, 4);

        Assert.AreEqual(0, count);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_WithComments_RemovesAllComments()
    {
        var code = @"extends Node
# Comment to remove
var x = 10
";
        var context = CreateContext(code);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);

        var newCode = result.Edits[0].NewText;
        Assert.IsFalse(newCode.Contains("#"));
        Assert.IsTrue(newCode.Contains("var x = 10"));
    }

    [TestMethod]
    public void Execute_WithoutComments_ReturnsFailed()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code);

        var result = _service.Execute(context);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_PreservesCodeStructure()
    {
        var code = @"extends Node

# Header comment
var x = 10

func test():
	# Inner comment
	print(x)
";
        var context = CreateContext(code);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);

        var newCode = result.Edits[0].NewText;
        Assert.IsTrue(newCode.Contains("extends Node"));
        Assert.IsTrue(newCode.Contains("var x = 10"));
        Assert.IsTrue(newCode.Contains("func test():"));
        Assert.IsTrue(newCode.Contains("print(x)"));
    }

    #endregion

    #region ExecuteInRange Tests

    [TestMethod]
    public void ExecuteInRange_WithCommentsInRange_RemovesOnlyRangeComments()
    {
        var code = @"extends Node
# Keep this comment
var x = 10
# Remove this
# And this
func test():
	pass
";
        var context = CreateContext(code);

        // Remove only comments on lines 3-4
        var result = _service.ExecuteInRange(context, 3, 4);

        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void ExecuteInRange_NoCommentsInRange_ReturnsFailed()
    {
        var code = @"extends Node
# Comment here
var x = 10
func test():
	pass
";
        var context = CreateContext(code);

        // No comments in lines 3-4
        var result = _service.ExecuteInRange(context, 3, 4);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_WithComments_ReturnsCommentInfo()
    {
        var code = @"extends Node
# First comment
var x = 10
# Second comment
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.CommentCount);
        Assert.AreEqual(2, result.Comments.Count);
    }

    [TestMethod]
    public void Plan_CommentsContainText()
    {
        var code = @"extends Node
# My comment text
var x = 10
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.CommentCount);
        Assert.IsTrue(result.Comments[0].Text.Contains("My comment text"));
    }

    [TestMethod]
    public void Plan_NoComments_ReturnsFailed()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsFalse(result.Success);
    }

    #endregion
}
