using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDConvertForToWhileServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDConvertForToWhileService _service = new();

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
    public void CanExecute_OnForStatement_ReturnsTrue()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(10):\n\t\tprint(i)\n";
        // Line 2, column 1 = "for" keyword
        var context = CreateContext(code, 2, 1);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_OnWhileStatement_ReturnsFalse()
    {
        var code = "extends Node\nfunc test():\n\twhile true:\n\t\tpass\n";
        // Line 2, column 1 = "while" keyword
        var context = CreateContext(code, 2, 1);

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_OutsideMethod_ReturnsFalse()
    {
        var code = "extends Node\nvar x = 10\n";
        var context = CreateContext(code, 1, 4);

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region Plan Tests - Range Conversions

    [TestMethod]
    public void Plan_RangeSingleArg_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(10):\n\t\tprint(i)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.RangeSingleArg, result.ConversionType);
        Assert.IsTrue(result.ConvertedCode.Contains("var i = 0"));
        Assert.IsTrue(result.ConvertedCode.Contains("while i < 10"));
        Assert.IsTrue(result.ConvertedCode.Contains("i += 1"));
    }

    [TestMethod]
    public void Plan_RangeTwoArgs_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(5, 15):\n\t\tprint(i)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.RangeTwoArgs, result.ConversionType);
        Assert.IsTrue(result.ConvertedCode.Contains("var i = 5"));
        Assert.IsTrue(result.ConvertedCode.Contains("while i < 15"));
        Assert.IsTrue(result.ConvertedCode.Contains("i += 1"));
    }

    [TestMethod]
    public void Plan_RangeThreeArgs_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(0, 20, 2):\n\t\tprint(i)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.RangeThreeArgs, result.ConversionType);
        Assert.IsTrue(result.ConvertedCode.Contains("var i = 0"));
        Assert.IsTrue(result.ConvertedCode.Contains("while i < 20"));
        Assert.IsTrue(result.ConvertedCode.Contains("i += 2"));
    }

    [TestMethod]
    public void Plan_RangeNegativeStep_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(10, 0, -1):\n\t\tprint(i)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.RangeThreeArgs, result.ConversionType);
        Assert.IsTrue(result.ConvertedCode.Contains("var i = 10"));
        Assert.IsTrue(result.ConvertedCode.Contains("while i > 0"));
        Assert.IsTrue(result.ConvertedCode.Contains("i += -1"));
    }

    #endregion

    #region Plan Tests - Collection Conversion

    [TestMethod]
    public void Plan_CollectionIteration_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor item in items:\n\t\tprint(item)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.Collection, result.ConversionType);
        Assert.IsTrue(result.ConvertedCode.Contains("var _idx = 0"));
        Assert.IsTrue(result.ConvertedCode.Contains("while _idx < items.size()"));
        Assert.IsTrue(result.ConvertedCode.Contains("var item = items[_idx]"));
        Assert.IsTrue(result.ConvertedCode.Contains("_idx += 1"));
    }

    [TestMethod]
    public void Plan_ArrayLiteral_ConvertsCorrectly()
    {
        var code = "extends Node\nfunc test():\n\tfor x in [1, 2, 3]:\n\t\tprint(x)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ForLoopConversionType.Collection, result.ConversionType);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_OnForStatement_ReturnsSuccessResult()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(10):\n\t\tprint(i)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
        Assert.IsTrue(result.Edits[0].NewText.Contains("while"));
    }

    [TestMethod]
    public void Execute_NotOnForStatement_ReturnsFailure()
    {
        var code = "extends Node\nfunc test():\n\twhile true:\n\t\tpass\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion

    #region Body Preservation Tests

    [TestMethod]
    public void Plan_PreservesLoopBody()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(5):\n\t\tvar x = i * 2\n\t\tprint(x)\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ConvertedCode.Contains("var x = i * 2"));
        Assert.IsTrue(result.ConvertedCode.Contains("print(x)"));
    }

    [TestMethod]
    public void Plan_EmptyBody_AddsPass()
    {
        var code = "extends Node\nfunc test():\n\tfor i in range(5):\n\t\tpass\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ConvertedCode.Contains("pass"));
    }

    #endregion
}
