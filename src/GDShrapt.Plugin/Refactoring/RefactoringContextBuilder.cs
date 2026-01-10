using GDShrapt.Reader;
using GDShrapt.Semantics;
using Godot;

namespace GDShrapt.Plugin.Refactoring;

/// <summary>
/// Builds RefactoringContext from editor state.
/// Uses GDPositionFinder for optimized AST traversal.
/// </summary>
internal class RefactoringContextBuilder
{
    private readonly GDProjectMap _projectMap;
    private readonly Node _dialogParent;

    public RefactoringContextBuilder(GDProjectMap projectMap, Node dialogParent = null)
    {
        _projectMap = projectMap;
        _dialogParent = dialogParent;
    }

    /// <summary>
    /// Builds a refactoring context from the current editor state.
    /// </summary>
    public RefactoringContext Build(IScriptEditor editor)
    {
        if (editor == null)
            return null;

        var scriptMap = editor.ScriptMap;
        var @class = editor.GetClass();

        if (@class == null)
            return null;

        var cursorLine = editor.CursorLine;
        var cursorColumn = editor.CursorColumn;

        var hasSelection = editor.HasSelection;
        var selectedText = hasSelection ? GetSelectedText(editor) : string.Empty;

        // Use optimized GDPositionFinder for all position-based lookups
        var finder = new GDPositionFinder(@class);

        // Find node and token at cursor position using TryGetTokenByPosition (with early exit)
        var tokenAtCursor = finder.FindTokenAtPosition(cursorLine, cursorColumn);
        var nodeAtCursor = tokenAtCursor?.Parent;

        // Find containing method efficiently using ClassMember property
        var containingMethod = FindContainingMethod(tokenAtCursor);

        // Use GDPositionFinder for condition checks (optimized with parent walk instead of full tree scan)
        var isOnGetNodeCall = finder.IsOnGetNodeCall(cursorLine, cursorColumn);
        var isOnNodePath = nodeAtCursor is GDNodePathExpression
                        || GDPositionFinder.FindParent<GDNodePathExpression>(nodeAtCursor) != null;
        var isInIfCondition = finder.IsInIfCondition(cursorLine, cursorColumn);
        var isOnClassVariable = finder.IsOnClassVariable(cursorLine, cursorColumn);

        // Check for selected statements/expressions using optimized methods
        var startLine = editor.SelectionStartLine;
        var startCol = editor.SelectionStartColumn;
        var endLine = editor.SelectionEndLine;
        var endCol = editor.SelectionEndColumn;

        var hasStatementsSelected = hasSelection && finder.HasStatementsSelected(startLine, startCol, endLine, endCol);
        var hasExpressionSelected = hasSelection && finder.HasExpressionSelected(startLine, startCol, endLine, endCol);
        var selectedExpression = hasExpressionSelected ? finder.FindExpressionAtSelection(startLine, startCol, endLine, endCol) : null;

        return new RefactoringContext
        {
            Editor = editor,
            ScriptMap = scriptMap,
            ProjectMap = _projectMap,
            DialogParent = _dialogParent,
            CursorLine = cursorLine,
            CursorColumn = cursorColumn,
            SelectedText = selectedText,
            SelectionStartLine = startLine,
            SelectionStartColumn = startCol,
            SelectionEndLine = endLine,
            SelectionEndColumn = endCol,
            NodeAtCursor = nodeAtCursor,
            TokenAtCursor = tokenAtCursor,
            ContainingMethod = containingMethod,
            ContainingClass = @class,
            IsInIfCondition = isInIfCondition,
            IsOnGetNodeCall = isOnGetNodeCall,
            IsOnNodePath = isOnNodePath,
            IsOnClassVariable = isOnClassVariable,
            HasStatementsSelected = hasStatementsSelected,
            HasExpressionSelected = hasExpressionSelected,
            SelectedExpression = selectedExpression
        };
    }

    private string GetSelectedText(IScriptEditor editor)
    {
        var startLine = editor.SelectionStartLine;
        var startCol = editor.SelectionStartColumn;
        var endLine = editor.SelectionEndLine;
        var endCol = editor.SelectionEndColumn;

        if (startLine == endLine)
        {
            var line = editor.GetLine(startLine);
            if (startCol < line.Length && endCol <= line.Length)
                return line.Substring(startCol, endCol - startCol);
            return string.Empty;
        }

        // Multi-line selection
        var result = new System.Text.StringBuilder();
        for (int i = startLine; i <= endLine; i++)
        {
            var line = editor.GetLine(i);
            if (i == startLine)
                result.AppendLine(line.Substring(startCol));
            else if (i == endLine)
                result.Append(line.Substring(0, System.Math.Min(endCol, line.Length)));
            else
                result.AppendLine(line);
        }
        return result.ToString();
    }

    private GDMethodDeclaration FindContainingMethod(GDSyntaxToken token)
    {
        if (token == null)
            return null;

        // Use ClassMember property for efficiency (avoids full parent walk)
        var classMember = token.ClassMember;
        if (classMember is GDMethodDeclaration method)
            return method;

        // Fallback to parent walk
        return GDPositionFinder.FindParent<GDMethodDeclaration>(token);
    }

    /// <summary>
    /// Creates a GDRefactoringContext from the Plugin's RefactoringContext.
    /// This bridges the Plugin context to the Semantics context for using
    /// refactoring services from GDShrapt.Semantics.
    /// </summary>
    public GDRefactoringContext ToSemanticsContext(RefactoringContext pluginContext)
    {
        if (pluginContext?.ContainingClass == null)
            return null;

        // Create a GDScriptFile wrapper for the context
        var reference = new GDScriptReference(pluginContext.ScriptMap?.Reference?.FullPath ?? "unknown.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(pluginContext.ContainingClass.ToString());

        // Build selection info
        var selection = pluginContext.HasSelection
            ? new GDSelectionInfo(
                pluginContext.SelectionStartLine,
                pluginContext.SelectionStartColumn,
                pluginContext.SelectionEndLine,
                pluginContext.SelectionEndColumn,
                pluginContext.SelectedText)
            : GDSelectionInfo.None;

        // Build cursor position
        var cursor = new GDCursorPosition(pluginContext.CursorLine, pluginContext.CursorColumn);

        return new GDRefactoringContext(
            scriptFile,
            pluginContext.ContainingClass,
            cursor,
            selection);
    }

    /// <summary>
    /// Creates a GDRefactoringContext directly from an IScriptEditor.
    /// This is a convenience method that combines Build and ToSemanticsContext.
    /// </summary>
    public GDRefactoringContext BuildSemanticsContext(IScriptEditor editor)
    {
        var pluginContext = Build(editor);
        if (pluginContext == null)
            return null;

        return ToSemanticsContext(pluginContext);
    }
}
