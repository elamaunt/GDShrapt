using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.LSP.Adapters;

/// <summary>
/// Utility for finding AST nodes at specific positions.
/// </summary>
public static class GDNodeFinder
{
    /// <summary>
    /// Finds the most specific token at the given position.
    /// </summary>
    public static GDSyntaxToken? FindTokenAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        GDSyntaxToken? bestMatch = null;

        foreach (var token in root.AllTokens)
        {
            if (ContainsPosition(token, line, column))
            {
                // Find the most specific (smallest) token
                if (bestMatch == null || IsMoreSpecific(token, bestMatch))
                {
                    bestMatch = token;
                }
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Finds the identifier token at the given position.
    /// </summary>
    public static GDIdentifier? FindIdentifierAtPosition(GDNode root, int line, int column)
    {
        var token = FindTokenAtPosition(root, line, column);

        // If the token itself is an identifier, return it
        if (token is GDIdentifier identifier)
            return identifier;

        // Walk up to find identifier in parent context
        if (token != null)
        {
            var parent = token.Parent;
            while (parent != null)
            {
                // Check direct child identifiers
                foreach (var childToken in parent.Tokens)
                {
                    if (childToken is GDIdentifier id && ContainsPosition(id, line, column))
                        return id;
                }
                parent = parent.Parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the identifier expression at the given position.
    /// </summary>
    public static GDIdentifierExpression? FindIdentifierExpressionAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        foreach (var node in root.AllNodes)
        {
            if (node is GDIdentifierExpression idExpr && ContainsPosition(idExpr, line, column))
            {
                return idExpr;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the node at the given position.
    /// </summary>
    public static GDNode? FindNodeAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        GDNode? bestMatch = null;

        foreach (var node in root.AllNodes)
        {
            if (ContainsPosition(node, line, column))
            {
                // Find the most specific (smallest) node
                if (bestMatch == null || IsMoreSpecific(node, bestMatch))
                {
                    bestMatch = node;
                }
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Finds all identifiers with the given name.
    /// </summary>
    public static IEnumerable<GDIdentifier> FindIdentifiersByName(GDNode root, string name)
    {
        foreach (var token in root.AllTokens)
        {
            if (token is GDIdentifier identifier && identifier.ToString() == name)
            {
                yield return identifier;
            }
        }
    }

    private static bool ContainsPosition(GDSyntaxToken token, int line, int column)
    {
        // Check if position is within token's range
        if (line < token.StartLine || line > token.EndLine)
            return false;

        if (line == token.StartLine && column < token.StartColumn)
            return false;

        if (line == token.EndLine && column > token.EndColumn)
            return false;

        return true;
    }

    private static bool IsMoreSpecific(GDSyntaxToken candidate, GDSyntaxToken current)
    {
        // A token is more specific if it has a smaller range
        var candidateLines = candidate.EndLine - candidate.StartLine;
        var currentLines = current.EndLine - current.StartLine;

        if (candidateLines < currentLines)
            return true;

        if (candidateLines > currentLines)
            return false;

        // Same number of lines, compare columns
        var candidateCols = (candidate.EndColumn - candidate.StartColumn);
        var currentCols = (current.EndColumn - current.StartColumn);

        return candidateCols < currentCols;
    }
}
