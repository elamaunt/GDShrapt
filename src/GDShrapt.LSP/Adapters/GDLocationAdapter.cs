using GDShrapt.Reader;

namespace GDShrapt.LSP;

/// <summary>
/// Adapts GDShrapt locations to LSP locations.
/// </summary>
public static class GDLocationAdapter
{
    /// <summary>
    /// Creates an LSP location from an AST node.
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
                Start = new GDLspPosition(node.StartLine - 1, node.StartColumn - 1),
                End = new GDLspPosition(node.EndLine - 1, node.EndColumn)
            }
        };
    }

    /// <summary>
    /// Creates an LSP location from a syntax token.
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
                Start = new GDLspPosition(token.StartLine - 1, token.StartColumn - 1),
                End = new GDLspPosition(token.EndLine - 1, token.EndColumn)
            }
        };
    }

    /// <summary>
    /// Creates an LSP range from a syntax token.
    /// </summary>
    public static GDLspRange? RangeFromToken(GDSyntaxToken? token)
    {
        if (token == null)
            return null;

        return new GDLspRange(
            token.StartLine - 1,
            token.StartColumn - 1,
            token.EndLine - 1,
            token.EndColumn);
    }

    /// <summary>
    /// Creates an LSP position from line/column (1-based to 0-based).
    /// </summary>
    public static GDLspPosition ToLspPosition(int line, int column)
    {
        return new GDLspPosition(line - 1, column - 1);
    }

    /// <summary>
    /// Creates an LSP range from start/end line/column (1-based to 0-based).
    /// </summary>
    public static GDLspRange ToLspRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        return new GDLspRange(startLine - 1, startColumn - 1, endLine - 1, endColumn);
    }
}
