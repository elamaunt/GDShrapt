using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDInvertConditionServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDInvertConditionService _service = new();

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    private GDRefactoringContext CreateContext(string code, int line, int column)
    {
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(line, column);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_OnIfStatement_ReturnsTrue()
    {
        // Note: Uses TAB for indentation
        var code = "extends Node\nfunc test():\n\tif x > 10:\n\t\tpass\n";
        // Line 2, column 1 = "if" keyword (tab is column 0)
        var context = CreateContext(code, 2, 1);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_OnWhileStatement_ReturnsTrue()
    {
        var code = "extends Node\nfunc test():\n\twhile running:\n\t\tpass\n";
        // Line 2, column 1 = "while" keyword
        var context = CreateContext(code, 2, 1);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_OnVariableDeclaration_ReturnsFalse()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code, 1, 4); // On variable

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region InvertExpression Tests - Comparison Operators

    [TestMethod]
    public void InvertExpression_EqualOperator_ReturnsNotEqual()
    {
        var expr = _reader.ParseExpression("x == 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x != 10", result);
    }

    [TestMethod]
    public void InvertExpression_NotEqualOperator_ReturnsEqual()
    {
        var expr = _reader.ParseExpression("x != 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x == 10", result);
    }

    [TestMethod]
    public void InvertExpression_GreaterThan_ReturnsLessOrEqual()
    {
        var expr = _reader.ParseExpression("x > 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x <= 10", result);
    }

    [TestMethod]
    public void InvertExpression_LessThan_ReturnsGreaterOrEqual()
    {
        var expr = _reader.ParseExpression("x < 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x >= 10", result);
    }

    [TestMethod]
    public void InvertExpression_GreaterOrEqual_ReturnsLessThan()
    {
        var expr = _reader.ParseExpression("x >= 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x < 10", result);
    }

    [TestMethod]
    public void InvertExpression_LessOrEqual_ReturnsGreaterThan()
    {
        var expr = _reader.ParseExpression("x <= 10");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x > 10", result);
    }

    #endregion

    #region InvertExpression Tests - Logical Operators (De Morgan's Laws)

    [TestMethod]
    public void InvertExpression_AndOperator_AppliesDeMorgan()
    {
        var expr = _reader.ParseExpression("a and b");

        var result = _service.InvertExpression(expr);

        // not (a and b) = (not a) or (not b)
        Assert.AreEqual("(not a) or (not b)", result);
    }

    [TestMethod]
    public void InvertExpression_OrOperator_AppliesDeMorgan()
    {
        var expr = _reader.ParseExpression("a or b");

        var result = _service.InvertExpression(expr);

        // not (a or b) = (not a) and (not b)
        Assert.AreEqual("(not a) and (not b)", result);
    }

    [TestMethod]
    public void InvertExpression_AndWithComparisons_AppliesDeMorgan()
    {
        var expr = _reader.ParseExpression("x > 10 and y < 20");

        var result = _service.InvertExpression(expr);

        // Comparisons don't need extra wrapping
        Assert.AreEqual("(not x > 10) or (not y < 20)", result);
    }

    #endregion

    #region InvertExpression Tests - Single Operators

    [TestMethod]
    public void InvertExpression_NotOperator_RemovesNot()
    {
        var expr = _reader.ParseExpression("not x");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("x", result);
    }

    [TestMethod]
    public void InvertExpression_BoolTrue_ReturnsFalse()
    {
        var expr = _reader.ParseExpression("true");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("false", result);
    }

    [TestMethod]
    public void InvertExpression_BoolFalse_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("false");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("true", result);
    }

    #endregion

    #region InvertExpression Tests - Complex Expressions

    [TestMethod]
    public void InvertExpression_Identifier_AddsNot()
    {
        var expr = _reader.ParseExpression("is_ready");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("not is_ready", result);
    }

    [TestMethod]
    public void InvertExpression_CallExpression_WrapsWithNot()
    {
        var expr = _reader.ParseExpression("is_visible()");

        var result = _service.InvertExpression(expr);

        Assert.AreEqual("not (is_visible())", result);
    }

    [TestMethod]
    public void InvertExpression_BracketExpression_HandlesCorrectly()
    {
        var expr = _reader.ParseExpression("(x > 10)");

        var result = _service.InvertExpression(expr);

        // Should invert the inner expression
        Assert.IsTrue(result.Contains("not"));
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_OnIfWithSimpleCondition_InvertsCondition()
    {
        var code = "extends Node\nfunc test():\n\tif x == 10:\n\t\tpass\n";
        // Line 2, column 1 = "if" keyword
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
        Assert.AreEqual("x != 10", result.Edits[0].NewText);
    }

    [TestMethod]
    public void Execute_OnWhileWithCondition_InvertsCondition()
    {
        var code = "extends Node\nfunc test():\n\twhile x > 0:\n\t\tx -= 1\n";
        // Line 2, column 1 = "while" keyword
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
        Assert.AreEqual("x <= 0", result.Edits[0].NewText);
    }

    [TestMethod]
    public void Execute_OnVariableDeclaration_ReturnsFailure()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code, 1, 4);

        var result = _service.Execute(context);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_OnIfWithElse_SwapsBranches()
    {
        var code = "extends Node\nfunc test():\n\tif x == 10:\n\t\tprint(\"a\")\n\telse:\n\t\tprint(\"b\")\n";
        // Line 2, column 1 = "if" keyword
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
        // Should contain inverted condition and swapped branches
        Assert.IsTrue(result.Edits[0].NewText.Contains("x != 10"));
    }

    [TestMethod]
    public void Execute_OnIfWithElif_OnlyInvertsCondition()
    {
        var code = "extends Node\nfunc test():\n\tif x == 10:\n\t\tpass\n\telif x == 20:\n\t\tpass\n";
        // Line 2, column 1 = "if" keyword
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        // With elif, we don't swap branches, just invert condition
        Assert.AreEqual("x != 10", result.Edits[0].NewText);
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_OnIfStatement_ReturnsPreviewInfo()
    {
        var code = "extends Node\nfunc test():\n\tif x == 10:\n\t\tpass\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x == 10", result.OriginalCondition);
        Assert.AreEqual("x != 10", result.InvertedCondition);
        Assert.IsFalse(result.WillSwapBranches);
        Assert.IsNotNull(result.OriginalCode);
        Assert.IsNotNull(result.ResultCode);
    }

    [TestMethod]
    public void Plan_OnIfWithElse_IndicatesSwapBranches()
    {
        var code = "extends Node\nfunc test():\n\tif x == 10:\n\t\tprint(\"a\")\n\telse:\n\t\tprint(\"b\")\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.WillSwapBranches);
        Assert.IsNotNull(result.OriginalCode);
        Assert.IsNotNull(result.ResultCode);
        Assert.AreNotEqual(result.OriginalCode, result.ResultCode);
    }

    [TestMethod]
    public void Plan_OnWhileStatement_ReturnsPreviewInfo()
    {
        var code = "extends Node\nfunc test():\n\twhile x > 0:\n\t\tx -= 1\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x > 0", result.OriginalCondition);
        Assert.AreEqual("x <= 0", result.InvertedCondition);
        Assert.IsFalse(result.WillSwapBranches);
    }

    [TestMethod]
    public void Plan_OnVariableDeclaration_ReturnsFailure()
    {
        var code = "extends Node\nvar x = 10\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Plan_NullContext_ReturnsFailure()
    {
        var result = _service.Plan(null);

        Assert.IsFalse(result.Success);
    }

    #endregion
}
