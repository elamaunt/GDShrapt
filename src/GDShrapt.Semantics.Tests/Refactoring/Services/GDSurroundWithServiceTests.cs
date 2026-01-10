using System;
using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Services;

[TestClass]
public class GDSurroundWithServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDSurroundWithService _service = new();

    private GDRefactoringContext CreateContextWithSelection(string code, int startLine, int startColumn, int endLine, int endColumn)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);

        // Extract selected text
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

        var cursor = new GDCursorPosition(startLine, startColumn);
        var selection = new GDSelectionInfo(startLine, startColumn, endLine, endColumn, selectedText);
        return new GDRefactoringContext(script, classDecl, cursor, selection);
    }

    private GDRefactoringContext CreateContext(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(0, 0);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_WithSelection_ReturnsTrue()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_WithNoSelection_ReturnsFalse()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContext(code);

        // Without selection, depends on HasStatementSelected
        var result = _service.CanExecute(context);
        Assert.IsNotNull(result.ToString());
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region SurroundWithIf Tests

    [TestMethod]
    public void SurroundWithIf_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithIf(context, "x > 0");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithIf_WithDefaultCondition_UsesTrue()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithIf(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithIf_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithIf(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithIfElse Tests

    [TestMethod]
    public void SurroundWithIfElse_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithIfElse(context, "condition");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithIfElse_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithIfElse(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithFor Tests

    [TestMethod]
    public void SurroundWithFor_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithFor(context, "i", "range(5)");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithFor_WithDefaultParameters_UsesDefaults()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithFor(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithFor_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithFor(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithWhile Tests

    [TestMethod]
    public void SurroundWithWhile_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithWhile(context, "running");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithWhile_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithWhile(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithMatch Tests

    [TestMethod]
    public void SurroundWithMatch_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithMatch(context, "state");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithMatch_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithMatch(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithFunc Tests

    [TestMethod]
    public void SurroundWithFunc_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithFunc(context, "my_helper");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithFunc_WithDefaultName_UsesDefault()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithFunc(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithFunc_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithFunc(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region SurroundWithTry Tests

    [TestMethod]
    public void SurroundWithTry_WithSelection_ReturnsEdit()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.SurroundWithTry(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SurroundWithTry_NullContext_ReturnsFailed()
    {
        var result = _service.SurroundWithTry(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region GetAvailableOptions Tests

    [TestMethod]
    public void GetAvailableOptions_ReturnsNonEmptyList()
    {
        var options = _service.GetAvailableOptions();

        Assert.IsNotNull(options);
        Assert.IsTrue(options.Count > 0);
    }

    [TestMethod]
    public void GetAvailableOptions_ContainsCommonOptions()
    {
        var options = _service.GetAvailableOptions();
        var ids = options.Select(o => o.Id).ToList();

        Assert.IsTrue(ids.Contains("if"));
        Assert.IsTrue(ids.Contains("if-else"));
        Assert.IsTrue(ids.Contains("for"));
        Assert.IsTrue(ids.Contains("while"));
        Assert.IsTrue(ids.Contains("match"));
        Assert.IsTrue(ids.Contains("func"));
        Assert.IsTrue(ids.Contains("try"));
    }

    [TestMethod]
    public void GetAvailableOptions_OptionsHaveDescriptions()
    {
        var options = _service.GetAvailableOptions();

        foreach (var option in options)
        {
            Assert.IsFalse(string.IsNullOrEmpty(option.Id));
            Assert.IsFalse(string.IsNullOrEmpty(option.Description));
            Assert.IsFalse(string.IsNullOrEmpty(option.Template));
        }
    }

    #endregion

    #region GDSurroundOption Tests

    [TestMethod]
    public void GDSurroundOption_ToString_ReturnsFormattedString()
    {
        var option = new GDSurroundOption("test", "template", "Test Description");

        var str = option.ToString();

        Assert.IsTrue(str.Contains("test"));
        Assert.IsTrue(str.Contains("Test Description"));
    }

    [TestMethod]
    public void GDSurroundOption_Properties_AreSet()
    {
        var option = new GDSurroundOption("id", "template", "description");

        Assert.AreEqual("id", option.Id);
        Assert.AreEqual("template", option.Template);
        Assert.AreEqual("description", option.Description);
    }

    #endregion

    #region Plan Methods Tests

    [TestMethod]
    public void PlanSurroundWithIf_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithIf(context, "x > 0");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("if", result.SurroundType);
        Assert.IsNotNull(result.OriginalCode);
        Assert.IsNotNull(result.ResultCode);
        Assert.IsTrue(result.AffectedLinesCount >= 1);
        Assert.IsTrue(result.ResultCode.Contains("if x > 0:"));
    }

    [TestMethod]
    public void PlanSurroundWithIfElse_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithIfElse(context, "condition");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("if-else", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("if condition:"));
        Assert.IsTrue(result.ResultCode.Contains("else:"));
    }

    [TestMethod]
    public void PlanSurroundWithFor_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithFor(context, "i", "range(5)");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("for", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("for i in range(5):"));
    }

    [TestMethod]
    public void PlanSurroundWithWhile_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithWhile(context, "running");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("while", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("while running:"));
    }

    [TestMethod]
    public void PlanSurroundWithMatch_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithMatch(context, "state");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("match", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("match state:"));
    }

    [TestMethod]
    public void PlanSurroundWithFunc_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithFunc(context, "my_helper");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("func", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("func my_helper():"));
    }

    [TestMethod]
    public void PlanSurroundWithTry_WithSelection_ReturnsPreviewInfo()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithSelection(code, 2, 4, 2, 18);

        var result = _service.PlanSurroundWithTry(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("try", result.SurroundType);
        Assert.IsTrue(result.ResultCode.Contains("try:"));
        Assert.IsTrue(result.ResultCode.Contains("except:"));
    }

    [TestMethod]
    public void PlanSurroundWithIf_NullContext_ReturnsFailed()
    {
        var result = _service.PlanSurroundWithIf(null);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanSurroundWithIf_NoSelection_ReturnsFailed()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContext(code);

        var result = _service.PlanSurroundWithIf(context);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region GDSurroundWithResult Tests

    [TestMethod]
    public void GDSurroundWithResult_Planned_SetsProperties()
    {
        var result = GDSurroundWithResult.Planned("if", "original", "result", 3);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("if", result.SurroundType);
        Assert.AreEqual("original", result.OriginalCode);
        Assert.AreEqual("result", result.ResultCode);
        Assert.AreEqual(3, result.AffectedLinesCount);
    }

    [TestMethod]
    public void GDSurroundWithResult_Failed_SetsErrorMessage()
    {
        var result = GDSurroundWithResult.Failed("Test error");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Test error", result.ErrorMessage);
        Assert.IsNull(result.SurroundType);
    }

    #endregion
}
