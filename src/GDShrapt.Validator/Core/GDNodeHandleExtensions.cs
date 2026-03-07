using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Validator;

/// <summary>
/// Extensions for converting Reader AST nodes to handle types.
/// </summary>
internal static class GDNodeHandleExtensions
{
    public static GDNodeHandle ToHandle(this GDNode? node, string? filePath = null)
    {
        if (node == null)
            return GDNodeHandle.Empty;

        GDSyntaxToken? firstToken = null;
        GDSyntaxToken? lastToken = null;
        foreach (var token in node.AllTokens)
        {
            firstToken ??= token;
            lastToken = token;
        }

        var startLine = firstToken?.StartLine ?? 0;
        var startColumn = firstToken?.StartColumn ?? 0;
        var endLine = lastToken?.StartLine ?? startLine;
        var endColumn = lastToken != null ? lastToken.StartColumn + (lastToken.Length > 0 ? lastToken.Length : 0) : startColumn;

        return new GDNodeHandle(startLine, startColumn, endLine, endColumn, filePath, 0);
    }

    public static GDTokenHandle ToHandle(this GDSyntaxToken? token)
    {
        if (token == null)
            return GDTokenHandle.Empty;

        return new GDTokenHandle(
            token.StartLine,
            token.StartColumn,
            token.StartLine,
            token.StartColumn + (token.Length > 0 ? token.Length : 0),
            token.ToString(),
            0);
    }
}
