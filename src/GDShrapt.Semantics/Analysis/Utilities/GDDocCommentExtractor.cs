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
        int consecutiveNewLines = 0;
        while (currentToken != null)
        {
            if (currentToken is GDComment comment)
            {
                var text = comment.ToString().Trim();
                if (text.StartsWith("##"))
                {
                    var docText = text.Substring(2).TrimStart();
                    docLines.Insert(0, docText);
                    consecutiveNewLines = 0;
                }
                else
                {
                    break;
                }
            }
            else if (currentToken is GDNewLine)
            {
                consecutiveNewLines++;
                if (consecutiveNewLines >= 2)
                    break;
            }
            else if (currentToken is GDSpace
                     || currentToken is GDIntendation || currentToken is GDCarriageReturnToken)
            {
                // Non-newline whitespace is allowed between doc comments
            }
            else if (IsInsideAttribute(currentToken))
            {
                consecutiveNewLines = 0;
            }
            else
            {
                break;
            }

            currentToken = currentToken.GlobalPreviousToken;
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }

    private static bool IsInsideAttribute(GDSyntaxToken token)
    {
        var parent = token.Parent;
        while (parent != null)
        {
            if (parent is GDClassAttribute)
                return true;
            parent = parent.Parent;
        }
        return false;
    }
}
