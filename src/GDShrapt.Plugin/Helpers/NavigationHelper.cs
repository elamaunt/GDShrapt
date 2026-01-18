namespace GDShrapt.Plugin;

using Godot;
using GDShrapt.Reader;

/// <summary>
/// Helper class for navigating to tokens with precise selection.
/// Replaces simple SetCaretLine calls with full token selection.
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Navigates to a specific position and selects the range.
    /// </summary>
    /// <param name="editor">The CodeEdit control</param>
    /// <param name="line">Line number (0-based)</param>
    /// <param name="startCol">Start column (0-based)</param>
    /// <param name="endCol">End column (0-based)</param>
    /// <param name="centerViewport">Whether to center the viewport on the caret</param>
    public static void NavigateToToken(CodeEdit editor, int line, int startCol, int endCol, bool centerViewport = true)
    {
        if (editor == null)
            return;

        // Validate line bounds
        if (line < 0 || line >= editor.GetLineCount())
            return;

        // Set caret position
        editor.SetCaretLine(line);
        editor.SetCaretColumn(startCol);

        // Select the token (not just the line!)
        if (endCol > startCol)
        {
            editor.Select(line, startCol, line, endCol);
        }

        if (centerViewport)
        {
            editor.CenterViewportToCaret();
        }
    }

    /// <summary>
    /// Navigates to a syntax token and selects it.
    /// </summary>
    /// <param name="editor">The CodeEdit control</param>
    /// <param name="token">The syntax token to navigate to</param>
    /// <param name="centerViewport">Whether to center the viewport on the caret</param>
    public static void NavigateToToken(CodeEdit editor, GDSyntaxToken token, bool centerViewport = true)
    {
        if (token == null)
            return;

        NavigateToToken(editor, token.StartLine, token.StartColumn, token.EndColumn, centerViewport);
    }

    /// <summary>
    /// Navigates to a node and selects its identifier if available.
    /// </summary>
    /// <param name="editor">The CodeEdit control</param>
    /// <param name="node">The AST node to navigate to</param>
    /// <param name="centerViewport">Whether to center the viewport on the caret</param>
    public static void NavigateToNode(CodeEdit editor, GDNode node, bool centerViewport = true)
    {
        if (node == null)
            return;

        // Try to get the identifier for more precise selection
        var identifier = node switch
        {
            GDMethodDeclaration method => method.Identifier,
            GDVariableDeclaration variable => variable.Identifier,
            GDVariableDeclarationStatement localVar => localVar.Identifier,
            GDSignalDeclaration signal => signal.Identifier,
            GDParameterDeclaration param => param.Identifier,
            GDEnumDeclaration enumDecl => enumDecl.Identifier,
            GDInnerClassDeclaration innerClass => innerClass.Identifier,
            GDIdentifierExpression idExpr => idExpr.Identifier,
            _ => null
        };

        if (identifier != null)
        {
            NavigateToToken(editor, identifier, centerViewport);
        }
        else
        {
            // Fallback to node position
            NavigateToToken(editor, node.StartLine, node.StartColumn, node.EndColumn, centerViewport);
        }
    }

    /// <summary>
    /// Navigates to a location with optional column range.
    /// </summary>
    /// <param name="editor">The CodeEdit control</param>
    /// <param name="line">Line number (0-based)</param>
    /// <param name="startCol">Start column (0-based), defaults to 0</param>
    /// <param name="endCol">End column (0-based), defaults to start column</param>
    /// <param name="centerViewport">Whether to center the viewport on the caret</param>
    public static void NavigateToLocation(
        CodeEdit editor,
        int line,
        int startCol = 0,
        int? endCol = null,
        bool centerViewport = true)
    {
        NavigateToToken(editor, line, startCol, endCol ?? startCol, centerViewport);
    }

    /// <summary>
    /// Navigates to a line without selection (legacy behavior).
    /// Use NavigateToToken for precise selection.
    /// </summary>
    /// <param name="editor">The CodeEdit control</param>
    /// <param name="line">Line number (0-based)</param>
    /// <param name="centerViewport">Whether to center the viewport on the caret</param>
    public static void NavigateToLine(CodeEdit editor, int line, bool centerViewport = true)
    {
        if (editor == null)
            return;

        if (line < 0 || line >= editor.GetLineCount())
            return;

        editor.SetCaretLine(line);
        editor.SetCaretColumn(0);

        if (centerViewport)
        {
            editor.CenterViewportToCaret();
        }
    }
}
