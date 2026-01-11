using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDSnippetServiceTests
{
    private readonly GDSnippetService _service = new();
    private readonly GDScriptReader _reader = new();

    #region GetSnippetForKeyword Tests

    [TestMethod]
    public void GetSnippetForKeyword_ForKeyword_ReturnsSnippet()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        Assert.IsNotNull(snippet);
        Assert.AreEqual("for", snippet.Keyword);
        Assert.AreEqual(GDSnippetCategory.ControlFlow, snippet.Category);
    }

    [TestMethod]
    public void GetSnippetForKeyword_WhileKeyword_ReturnsSnippet()
    {
        var snippet = _service.GetSnippetForKeyword("while");

        Assert.IsNotNull(snippet);
        Assert.AreEqual("while", snippet.Keyword);
    }

    [TestMethod]
    public void GetSnippetForKeyword_IfKeyword_ReturnsSnippet()
    {
        var snippet = _service.GetSnippetForKeyword("if");

        Assert.IsNotNull(snippet);
        Assert.AreEqual("if", snippet.Keyword);
    }

    [TestMethod]
    public void GetSnippetForKeyword_FuncKeyword_ReturnsSnippet()
    {
        var snippet = _service.GetSnippetForKeyword("func");

        Assert.IsNotNull(snippet);
        Assert.AreEqual("func", snippet.Keyword);
        Assert.AreEqual(GDSnippetCategory.Declaration, snippet.Category);
    }

    [TestMethod]
    public void GetSnippetForKeyword_VarKeyword_ReturnsSnippet()
    {
        var snippet = _service.GetSnippetForKeyword("var");

        Assert.IsNotNull(snippet);
        Assert.AreEqual("var", snippet.Keyword);
    }

    [TestMethod]
    public void GetSnippetForKeyword_UnknownKeyword_ReturnsNull()
    {
        var snippet = _service.GetSnippetForKeyword("unknown_keyword");

        Assert.IsNull(snippet);
    }

    [TestMethod]
    public void GetSnippetForKeyword_EmptyString_ReturnsNull()
    {
        var snippet = _service.GetSnippetForKeyword("");

        Assert.IsNull(snippet);
    }

    [TestMethod]
    public void GetSnippetForKeyword_NullString_ReturnsNull()
    {
        var snippet = _service.GetSnippetForKeyword(null);

        Assert.IsNull(snippet);
    }

    #endregion

    #region GetAllSnippets Tests

    [TestMethod]
    public void GetAllSnippets_ReturnsNonEmptyList()
    {
        var snippets = _service.GetAllSnippets();

        Assert.IsNotNull(snippets);
        Assert.IsTrue(snippets.Count > 0);
    }

    [TestMethod]
    public void GetAllSnippets_ContainsCommonKeywords()
    {
        var snippets = _service.GetAllSnippets();
        var keywords = snippets.Select(s => s.Keyword).ToList();

        Assert.IsTrue(keywords.Contains("for"));
        Assert.IsTrue(keywords.Contains("while"));
        Assert.IsTrue(keywords.Contains("if"));
        Assert.IsTrue(keywords.Contains("func"));
        Assert.IsTrue(keywords.Contains("var"));
        Assert.IsTrue(keywords.Contains("const"));
    }

    #endregion

    #region GetSnippetsByCategory Tests

    [TestMethod]
    public void GetSnippetsByCategory_ControlFlow_ReturnsControlFlowSnippets()
    {
        var snippets = _service.GetSnippetsByCategory(GDSnippetCategory.ControlFlow);

        Assert.IsTrue(snippets.Count > 0);
        Assert.IsTrue(snippets.All(s => s.Category == GDSnippetCategory.ControlFlow));
    }

    [TestMethod]
    public void GetSnippetsByCategory_Declaration_ReturnsDeclarationSnippets()
    {
        var snippets = _service.GetSnippetsByCategory(GDSnippetCategory.Declaration);

        Assert.IsTrue(snippets.Count > 0);
        Assert.IsTrue(snippets.All(s => s.Category == GDSnippetCategory.Declaration));
    }

    [TestMethod]
    public void GetSnippetsByCategory_Annotation_ReturnsAnnotationSnippets()
    {
        var snippets = _service.GetSnippetsByCategory(GDSnippetCategory.Annotation);

        Assert.IsTrue(snippets.Count > 0);
        Assert.IsTrue(snippets.All(s => s.Category == GDSnippetCategory.Annotation));
    }

    #endregion

    #region HasSnippet Tests

    [TestMethod]
    public void HasSnippet_ExistingKeyword_ReturnsTrue()
    {
        Assert.IsTrue(_service.HasSnippet("for"));
        Assert.IsTrue(_service.HasSnippet("while"));
        Assert.IsTrue(_service.HasSnippet("func"));
    }

    [TestMethod]
    public void HasSnippet_NonExistingKeyword_ReturnsFalse()
    {
        Assert.IsFalse(_service.HasSnippet("nonexistent"));
        Assert.IsFalse(_service.HasSnippet(""));
        Assert.IsFalse(_service.HasSnippet(null));
    }

    #endregion

    #region TryMatchKeyword Tests

    [TestMethod]
    public void TryMatchKeyword_LineEndingWithFor_ReturnsTrue()
    {
        var lineText = "\tfor";

        var result = _service.TryMatchKeyword(lineText, out var keyword);

        Assert.IsTrue(result);
        Assert.AreEqual("for", keyword);
    }

    [TestMethod]
    public void TryMatchKeyword_LineWithSpaceBeforeKeyword_ReturnsTrue()
    {
        var lineText = " if";

        var result = _service.TryMatchKeyword(lineText, out var keyword);

        Assert.IsTrue(result);
        Assert.AreEqual("if", keyword);
    }

    [TestMethod]
    public void TryMatchKeyword_JustKeyword_ReturnsTrue()
    {
        var lineText = "while";

        var result = _service.TryMatchKeyword(lineText, out var keyword);

        Assert.IsTrue(result);
        Assert.AreEqual("while", keyword);
    }

    [TestMethod]
    public void TryMatchKeyword_NoMatch_ReturnsFalse()
    {
        var lineText = "some random text";

        var result = _service.TryMatchKeyword(lineText, out var keyword);

        Assert.IsFalse(result);
        Assert.IsNull(keyword);
    }

    [TestMethod]
    public void TryMatchKeyword_KeywordInMiddle_ReturnsFalse()
    {
        var lineText = "for x in range";

        var result = _service.TryMatchKeyword(lineText, out var keyword);

        Assert.IsFalse(result);
    }

    #endregion

    #region GetInsertionText Tests

    [TestMethod]
    public void GetInsertionText_ForSnippet_ReturnsExpandedTemplate()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        var text = _service.GetInsertionText(snippet);

        Assert.IsTrue(text.Contains("i"));
        Assert.IsTrue(text.Contains("range"));
        Assert.IsTrue(text.Contains("10"));
    }

    [TestMethod]
    public void GetInsertionText_IfSnippet_ReturnsExpandedTemplate()
    {
        var snippet = _service.GetSnippetForKeyword("if");

        var text = _service.GetInsertionText(snippet);

        Assert.IsTrue(text.Contains("true"));
        Assert.IsTrue(text.Contains(":"));
    }

    [TestMethod]
    public void GetInsertionText_FuncSnippet_ReturnsExpandedTemplate()
    {
        var snippet = _service.GetSnippetForKeyword("func");

        var text = _service.GetInsertionText(snippet);

        Assert.IsTrue(text.Contains("_new_function"));
        Assert.IsTrue(text.Contains("()"));
    }

    [TestMethod]
    public void GetInsertionText_NullSnippet_ReturnsEmpty()
    {
        var text = _service.GetInsertionText(null);

        Assert.AreEqual("", text);
    }

    #endregion

    #region GetFirstPlaceholderSelection Tests

    [TestMethod]
    public void GetFirstPlaceholderSelection_ForSnippet_ReturnsPlaceholderPosition()
    {
        var snippet = _service.GetSnippetForKeyword("for");
        var baseColumn = 0;

        var result = _service.GetFirstPlaceholderSelection(snippet, baseColumn, out var start, out var end);

        Assert.IsTrue(result);
        Assert.IsTrue(end > start);
    }

    [TestMethod]
    public void GetFirstPlaceholderSelection_PassSnippet_ReturnsFalse()
    {
        var snippet = _service.GetSnippetForKeyword("pass");

        var result = _service.GetFirstPlaceholderSelection(snippet, 0, out var start, out var end);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetFirstPlaceholderSelection_NullSnippet_ReturnsFalse()
    {
        var result = _service.GetFirstPlaceholderSelection(null, 0, out var start, out var end);

        Assert.IsFalse(result);
    }

    #endregion

    #region Snippet Properties Tests

    [TestMethod]
    public void Snippet_HasAllRequiredProperties()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        Assert.IsNotNull(snippet.Keyword);
        Assert.IsNotNull(snippet.Description);
        Assert.IsNotNull(snippet.Template);
        Assert.IsNotNull(snippet.Placeholders);
    }

    [TestMethod]
    public void Snippet_ForLoop_HasCorrectPlaceholders()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        Assert.IsTrue(snippet.Placeholders.Count >= 2);
        Assert.IsTrue(snippet.Placeholders.Contains("i"));
        Assert.IsTrue(snippet.Placeholders.Contains("10"));
    }

    [TestMethod]
    public void Snippet_ToString_ReturnsDescription()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        var str = snippet.ToString();

        Assert.IsTrue(str.Contains("for"));
    }

    #endregion

    #region PlanApplySnippet Tests

    private GDRefactoringContext CreateSnippetContext(string code, int line, int column)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(line, column);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    [TestMethod]
    public void PlanApplySnippet_ValidContext_ReturnsPreviewInfo()
    {
        var code = "extends Node\n";
        var context = CreateSnippetContext(code, 1, 0);
        var snippet = _service.GetSnippetForKeyword("for");

        var result = _service.PlanApplySnippet(context, snippet);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(snippet, result.Snippet);
        Assert.IsNotNull(result.ExpandedText);
        Assert.IsNotNull(result.PlaceholderPositions);
        Assert.IsTrue(result.PlaceholderPositions.Count > 0);
    }

    [TestMethod]
    public void PlanApplySnippet_NullContext_ReturnsFailed()
    {
        var snippet = _service.GetSnippetForKeyword("for");

        var result = _service.PlanApplySnippet(null, snippet);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanApplySnippet_NullSnippet_ReturnsFailed()
    {
        var code = "extends Node\n";
        var context = CreateSnippetContext(code, 1, 0);

        var result = _service.PlanApplySnippet(context, null);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanApplySnippetByKeyword_ValidKeyword_ReturnsPreviewInfo()
    {
        var code = "extends Node\n";
        var context = CreateSnippetContext(code, 1, 0);

        var result = _service.PlanApplySnippetByKeyword(context, "if");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("if", result.Snippet.Keyword);
        Assert.IsNotNull(result.ExpandedText);
    }

    [TestMethod]
    public void PlanApplySnippetByKeyword_InvalidKeyword_ReturnsFailed()
    {
        var code = "extends Node\n";
        var context = CreateSnippetContext(code, 1, 0);

        var result = _service.PlanApplySnippetByKeyword(context, "unknown_keyword");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("unknown_keyword"));
    }

    [TestMethod]
    public void PlanApplySnippet_ForSnippet_HasCorrectPlaceholderPositions()
    {
        var code = "extends Node\n";
        var context = CreateSnippetContext(code, 1, 4);
        var snippet = _service.GetSnippetForKeyword("for");

        var result = _service.PlanApplySnippet(context, snippet);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.PlaceholderPositions.Count >= 2);
        // First placeholder should be "i"
        Assert.AreEqual("i", result.PlaceholderPositions[0].DefaultValue);
        Assert.AreEqual(1, result.PlaceholderPositions[0].Length);
    }

    #endregion

    #region GDPlaceholderPosition Tests

    [TestMethod]
    public void GDPlaceholderPosition_Properties_AreSet()
    {
        var position = new GDPlaceholderPosition(5, 10, 3, "foo");

        Assert.AreEqual(5, position.Line);
        Assert.AreEqual(10, position.Column);
        Assert.AreEqual(3, position.Length);
        Assert.AreEqual("foo", position.DefaultValue);
    }

    [TestMethod]
    public void GDPlaceholderPosition_ToString_ContainsInfo()
    {
        var position = new GDPlaceholderPosition(5, 10, 3, "foo");

        var str = position.ToString();

        Assert.IsTrue(str.Contains("5"));
        Assert.IsTrue(str.Contains("10"));
        Assert.IsTrue(str.Contains("foo"));
    }

    #endregion

    #region GDSnippetResult Tests

    [TestMethod]
    public void GDSnippetResult_Planned_SetsProperties()
    {
        var snippet = _service.GetSnippetForKeyword("for");
        var placeholders = new System.Collections.Generic.List<GDPlaceholderPosition>
        {
            new GDPlaceholderPosition(0, 0, 1, "i")
        };

        var result = GDSnippetResult.Planned(snippet, "expanded text", placeholders);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(snippet, result.Snippet);
        Assert.AreEqual("expanded text", result.ExpandedText);
        Assert.AreEqual(1, result.PlaceholderPositions.Count);
    }

    [TestMethod]
    public void GDSnippetResult_Failed_SetsErrorMessage()
    {
        var result = GDSnippetResult.Failed("Test error");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Test error", result.ErrorMessage);
        Assert.IsNull(result.Snippet);
        Assert.IsNull(result.ExpandedText);
    }

    #endregion
}
