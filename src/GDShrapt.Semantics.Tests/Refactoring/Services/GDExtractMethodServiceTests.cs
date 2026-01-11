using System;
using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDExtractMethodServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDExtractMethodService _service = new();

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    private GDRefactoringContext CreateContext(string code, int startLine, int startColumn, int endLine, int endColumn)
    {
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(startLine, startColumn);
        // Extract the selected text from code for GDSelectionInfo
        var lines = code.Split('\n');
        var selectedText = "";
        if (startLine < lines.Length)
        {
            if (startLine == endLine && startColumn < lines[startLine].Length)
            {
                var endCol = Math.Min(endColumn, lines[startLine].Length);
                selectedText = lines[startLine].Substring(startColumn, endCol - startColumn);
            }
            else
            {
                selectedText = "selected";
            }
        }
        var selection = new GDSelectionInfo(startLine, startColumn, endLine, endColumn, selectedText);
        return new GDRefactoringContext(script, classDecl, cursor, selection);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_WithStatementSelection_ReturnsTrue()
    {
        var code = @"extends Node
func test():
	var x = 10
	print(x)
	x += 1
";
        // Select lines 3-4 (print and x += 1)
        var context = CreateContext(code, 3, 1, 4, 7);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_WithNoSelection_ReturnsFalse()
    {
        var code = @"extends Node
func test():
	var x = 10
";
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(2, 5);
        var context = new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_WithValidSelection_ReturnsMethodInfo()
    {
        var code = @"extends Node
func test():
	var x = 10
	print(x)
";
        // Select line 3 (print(x))
        var context = CreateContext(code, 3, 1, 3, 9);

        var result = _service.Plan(context, "print_value");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("print_value", result.MethodName);
        Assert.IsTrue(result.DetectedParameters.Contains("x"));
        Assert.IsFalse(result.IsStatic);
        Assert.IsNotNull(result.GeneratedMethodCode);
        Assert.IsNotNull(result.GeneratedCallCode);
    }

    [TestMethod]
    public void Plan_WithEmptyMethodName_UsesDefault()
    {
        var code = @"extends Node
func test():
	var x = 10
	print(x)
";
        var context = CreateContext(code, 3, 1, 3, 9);

        var result = _service.Plan(context, "");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("_new_method", result.MethodName);
    }

    [TestMethod]
    public void Plan_InStaticMethod_ResultIsStatic()
    {
        var code = @"extends Node
static func test():
	var x = 10
	print(x)
";
        var context = CreateContext(code, 3, 1, 3, 9);

        var result = _service.Plan(context, "helper");

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsStatic);
    }

    [TestMethod]
    public void Plan_WithNoDependencies_EmptyParameters()
    {
        var code = @"extends Node
func test():
	print(42)
";
        var context = CreateContext(code, 2, 1, 2, 10);

        var result = _service.Plan(context, "my_method");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.DetectedParameters.Count);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_WithValidSelection_ReturnsEdits()
    {
        var code = @"extends Node
func test():
	var x = 10
	print(x)
";
        var context = CreateContext(code, 3, 1, 3, 9);

        var result = _service.Execute(context, "print_value");

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.TotalEditsCount >= 1);
    }

    [TestMethod]
    public void Execute_WithInvalidContext_ReturnsFailed()
    {
        var code = @"extends Node
var x = 10
";
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(1, 0);
        var context = new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);

        var result = _service.Execute(context, "test_method");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_GeneratedMethodContainsBody()
    {
        var code = @"extends Node
func test():
	var x = 10
	print(x)
	print(x + 1)
";
        // Select both print statements (lines 3-4)
        var context = CreateContext(code, 3, 1, 4, 13);

        var planResult = _service.Plan(context, "log_values");

        Assert.IsTrue(planResult.Success);
        Assert.IsTrue(planResult.GeneratedMethodCode.Contains("print"));
        Assert.IsTrue(planResult.GeneratedCallCode.Contains("log_values"));
    }

    #endregion

    #region Method Name Normalization Tests

    [TestMethod]
    public void Plan_WithSpacesInName_ConvertsToSnakeCase()
    {
        var code = @"extends Node
func test():
	print(42)
";
        var context = CreateContext(code, 2, 1, 2, 10);

        var result = _service.Plan(context, "my method name");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("my_method_name", result.MethodName);
    }

    [TestMethod]
    public void Plan_WithUpperCase_ConvertsToLowerCase()
    {
        var code = @"extends Node
func test():
	print(42)
";
        var context = CreateContext(code, 2, 1, 2, 10);

        var result = _service.Plan(context, "MyMethod");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("mymethod", result.MethodName);
    }

    #endregion
}
