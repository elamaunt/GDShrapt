using GDShrapt.Reader;

namespace GDShrapt.LSP;

/// <summary>
/// Adapts GDShrapt locations to LSP locations.
/// </summary>
public static class GDLocationAdapter
{
    /// <summary>
    /// Creates an LSP location from an AST node.
    /// AST nodes have 0-based line/column, LSP also uses 0-based.
    /// </summary>
    public static GDLspLocation? FromNode(GDNode? node, string filePath)
    {
        if (node == null)
            return null;

        return new GDLspLocation
        {
            Uri = GDDocumentManager.PathToUri(filePath),
            Range = new GDLspRange
            {
                // AST is already 0-based, LSP is 0-based - no conversion needed
                Start = new GDLspPosition(node.StartLine, node.StartColumn),
                End = new GDLspPosition(node.EndLine, node.EndColumn)
            }
        };
    }

    /// <summary>
    /// Creates an LSP location from a syntax token.
    /// AST tokens have 0-based line/column, LSP also uses 0-based.
    /// </summary>
    public static GDLspLocation? FromToken(GDSyntaxToken? token, string filePath)
    {
        if (token == null)
            return null;

        return new GDLspLocation
        {
            Uri = GDDocumentManager.PathToUri(filePath),
            Range = new GDLspRange
            {
                // AST is already 0-based, LSP is 0-based - no conversion needed
                Start = new GDLspPosition(token.StartLine, token.StartColumn),
                End = new GDLspPosition(token.EndLine, token.EndColumn)
            }
        };
    }

    /// <summary>
    /// Creates an LSP range from a syntax token.
    /// AST tokens have 0-based line/column, LSP also uses 0-based.
    /// </summary>
    public static GDLspRange? RangeFromToken(GDSyntaxToken? token)
    {
        if (token == null)
            return null;

        // AST is already 0-based, LSP is 0-based - no conversion needed
        return new GDLspRange(
            token.StartLine,
            token.StartColumn,
            token.EndLine,
            token.EndColumn);
    }

    /// <summary>
    /// Creates an LSP position from line/column.
    /// Line is 1-based (from diagnostics), Column is 0-based.
    /// LSP uses 0-based for both.
    /// </summary>
    public static GDLspPosition ToLspPosition(int line, int column)
    {
        return new GDLspPosition(line - 1, column);  // Line: 1-based → 0-based, Column: already 0-based
    }

    /// <summary>
    /// Creates an LSP range from start/end line/column.
    /// Line is 1-based (from diagnostics), Column is 0-based.
    /// LSP uses 0-based for both.
    /// </summary>
    public static GDLspRange ToLspRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        return new GDLspRange(startLine - 1, startColumn, endLine - 1, endColumn);  // Line: 1-based → 0-based
    }
}
