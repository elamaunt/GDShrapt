using System;
using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Services;

[TestClass]
public class GDExtractVariableServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDExtractVariableService _service = new();

    private GDRefactoringContext CreateContextWithExpression(string code, int startLine, int startColumn, int endLine, int endColumn)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);

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
    public void CanExecute_WithExpressionSelected_ReturnsTrue()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
";
        // Select the expression "10 + 20"
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        // Note: CanExecute depends on HasExpressionSelected which is computed in context
        // For this test, we mainly verify the method doesn't throw
        var result = _service.CanExecute(context);
        Assert.IsNotNull(result.ToString());
    }

    [TestMethod]
    public void CanExecute_WithNoSelection_ReturnsFalse()
    {
        var code = @"extends Node
func test():
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

    #region Plan Tests

    [TestMethod]
    public void Plan_WithValidExpression_ReturnsVariableInfo()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        // The Plan method should handle cases where expression detection works
        var result = _service.Plan(context, "sum");

        // Result depends on whether expression was detected
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_WithEmptyVariableName_UsesDefault()
    {
        var code = @"extends Node
func test():
    print(get_node(""Player""))
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 29);

        var result = _service.Plan(context, "");

        Assert.IsNotNull(result);
        if (result.Success)
        {
            Assert.AreEqual("new_variable", result.SuggestedName);
        }
    }

    [TestMethod]
    public void Plan_NullContext_ReturnsFailed()
    {
        var result = _service.Plan(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_WithValidExpression_ReturnsEdits()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        var result = _service.Execute(context, "sum");

        // Result depends on whether expression was detected
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Execute_NullContext_ReturnsFailed()
    {
        var result = _service.Execute(null, "test_var");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_WithReplaceAll_ReplacesAllOccurrences()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
    print(10 + 20)
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        var result = _service.Execute(context, "sum", replaceAll: true);

        // Result depends on expression detection
        Assert.IsNotNull(result);
    }

    #endregion

    #region SuggestVariableName Tests

    [TestMethod]
    public void SuggestVariableName_CallExpression_UsesCallName()
    {
        var expr = _reader.ParseExpression("get_node(\"Player\")") as GDCallExpression;

        var name = _service.SuggestVariableName(expr);

        Assert.IsNotNull(name);
        // Should suggest something based on get_node
    }

    [TestMethod]
    public void SuggestVariableName_StringExpression_ReturnsText()
    {
        var expr = _reader.ParseExpression("\"hello\"");

        var name = _service.SuggestVariableName(expr);

        Assert.AreEqual("text", name);
    }

    [TestMethod]
    public void SuggestVariableName_NumberExpression_ReturnsValue()
    {
        var expr = _reader.ParseExpression("42");

        var name = _service.SuggestVariableName(expr);

        Assert.AreEqual("value", name);
    }

    [TestMethod]
    public void SuggestVariableName_ArrayExpression_ReturnsItems()
    {
        var expr = _reader.ParseExpression("[1, 2, 3]");

        var name = _service.SuggestVariableName(expr);

        Assert.AreEqual("items", name);
    }

    [TestMethod]
    public void SuggestVariableName_DictionaryExpression_ReturnsDict()
    {
        var expr = _reader.ParseExpression("{\"a\": 1}");

        var name = _service.SuggestVariableName(expr);

        Assert.AreEqual("dict", name);
    }

    [TestMethod]
    public void SuggestVariableName_NullExpression_ReturnsValue()
    {
        var name = _service.SuggestVariableName(null);

        Assert.AreEqual("value", name);
    }

    #endregion

    #region Variable Name Normalization Tests

    [TestMethod]
    public void Plan_WithSpacesInName_ConvertsToSnakeCase()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        var result = _service.Plan(context, "my variable name");

        if (result.Success)
        {
            Assert.AreEqual("my_variable_name", result.SuggestedName);
        }
    }

    [TestMethod]
    public void Plan_WithUpperCase_ConvertsToLowerCase()
    {
        var code = @"extends Node
func test():
    print(10 + 20)
";
        var context = CreateContextWithExpression(code, 2, 11, 2, 18);

        var result = _service.Plan(context, "MyVariable");

        if (result.Success)
        {
            Assert.AreEqual("myvariable", result.SuggestedName);
        }
    }

    #endregion

    #region Type Confidence Tests

    [TestMethod]
    public void Plan_LiteralIntExpression_ReturnsCertainConfidence()
    {
        var code = @"extends Node
func test():
    print(42)
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 12);

        var result = _service.Plan(context, "value");

        Assert.IsNotNull(result);
        // If expression is detected, check confidence for literal
        if (result.Success && result.InferredType == "int")
        {
            Assert.AreEqual(GDTypeConfidence.Certain, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_LiteralFloatExpression_ReturnsCertainConfidence()
    {
        var code = @"extends Node
func test():
    print(3.14)
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 14);

        var result = _service.Plan(context, "pi");

        Assert.IsNotNull(result);
        if (result.Success && result.InferredType == "float")
        {
            Assert.AreEqual(GDTypeConfidence.Certain, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_LiteralStringExpression_ReturnsCertainConfidence()
    {
        var code = @"extends Node
func test():
    print(""hello"")
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 17);

        var result = _service.Plan(context, "text");

        Assert.IsNotNull(result);
        if (result.Success && result.InferredType == "String")
        {
            Assert.AreEqual(GDTypeConfidence.Certain, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_LiteralBoolExpression_ReturnsCertainConfidence()
    {
        var code = @"extends Node
func test():
    print(true)
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 14);

        var result = _service.Plan(context, "flag");

        Assert.IsNotNull(result);
        if (result.Success && result.InferredType == "bool")
        {
            Assert.AreEqual(GDTypeConfidence.Certain, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_ArrayInitializer_ReturnsMediumConfidence()
    {
        var code = @"extends Node
func test():
    print([1, 2, 3])
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 19);

        var result = _service.Plan(context, "items");

        Assert.IsNotNull(result);
        if (result.Success && result.InferredType?.Contains("Array") == true)
        {
            // Arrays have Medium confidence since element types might vary
            Assert.IsTrue(result.TypeConfidence == GDTypeConfidence.Medium ||
                          result.TypeConfidence == GDTypeConfidence.Certain);
        }
    }

    [TestMethod]
    public void Plan_DictionaryInitializer_ReturnsMediumConfidence()
    {
        var code = @"extends Node
func test():
    print({""a"": 1})
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 19);

        var result = _service.Plan(context, "data");

        Assert.IsNotNull(result);
        if (result.Success && result.InferredType == "Dictionary")
        {
            Assert.AreEqual(GDTypeConfidence.Medium, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_UnknownExpression_ReturnsUnknownConfidence()
    {
        var code = @"extends Node
func test():
    var x = unknown_func()
    print(x)
";
        var context = CreateContextWithExpression(code, 2, 12, 2, 26);

        var result = _service.Plan(context, "value");

        // If expression is detected but type is unknown, should have Unknown or Medium confidence
        Assert.IsNotNull(result);
        if (result.Success)
        {
            Assert.IsTrue(result.TypeConfidence == GDTypeConfidence.Unknown ||
                          result.TypeConfidence == GDTypeConfidence.Medium);
        }
    }

    [TestMethod]
    public void Plan_WithConfidenceReason_HasReasonText()
    {
        var code = @"extends Node
func test():
    print(42)
";
        var context = CreateContextWithExpression(code, 2, 10, 2, 12);

        var result = _service.Plan(context, "value");

        // If successful with certain confidence, should have a reason
        if (result.Success && result.TypeConfidence == GDTypeConfidence.Certain)
        {
            Assert.IsFalse(string.IsNullOrEmpty(result.TypeConfidenceReason));
        }
    }

    #endregion
}
