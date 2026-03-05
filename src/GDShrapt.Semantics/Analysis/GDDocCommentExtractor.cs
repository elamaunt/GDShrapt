using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Extracts ## doc comments from GDScript AST nodes.
/// </summary>
internal static class GDDocCommentExtractor
{
    /// <summary>
    /// Extracts ## doc comment text from above the given declaration node.
    /// Walks GlobalPreviousToken chain for consecutive ## comments.
    /// </summary>
    public static string? Extract(GDNode? declaration)
    {
        if (declaration == null)
            return null;

        var docLines = new List<string>();

        GDSyntaxToken? firstToken = null;
        foreach (var token in declaration.AllTokens)
        {
            firstToken = token;
            break;
        }

        if (firstToken == null)
            return null;

        var currentToken = firstToken.GlobalPreviousToken;
        while (currentToken != null)
        {
            if (currentToken is GDComment comment)
            {
                var text = comment.ToString().Trim();
                if (text.StartsWith("##"))
                {
                    var docText = text.Substring(2).TrimStart();
                    docLines.Insert(0, docText);
                }
                else
                {
                    break;
                }
            }
            else if (currentToken is GDNewLine || currentToken is GDSpace)
            {
                // Whitespace is allowed between doc comments
            }
            else
            {
                break;
            }

            currentToken = currentToken.GlobalPreviousToken;
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }
}
