using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.LSP.Adapters;

/// <summary>
/// Utility for finding AST nodes at specific positions.
/// Delegates to GDPositionFinder from Semantics for optimized lookups.
/// </summary>
public static class GDNodeFinder
{
    /// <summary>
    /// Finds the most specific token at the given position.
    /// Uses TryGetTokenByPosition for optimized lookup with early exit.
    /// </summary>
    public static GDSyntaxToken? FindTokenAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        var finder = new GDPositionFinder(root);
        return finder.FindTokenAtPosition(line, column);
    }

    /// <summary>
    /// Finds the identifier token at the given position.
    /// </summary>
    public static GDIdentifier? FindIdentifierAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        var finder = new GDPositionFinder(root);
        return finder.FindIdentifierAtPosition(line, column);
    }

    /// <summary>
    /// Finds the identifier expression at the given position.
    /// </summary>
    public static GDIdentifierExpression? FindIdentifierExpressionAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        var finder = new GDPositionFinder(root);
        return finder.FindNodeAtPosition<GDIdentifierExpression>(line, column);
    }

    /// <summary>
    /// Finds the node at the given position.
    /// </summary>
    public static GDNode? FindNodeAtPosition(GDNode root, int line, int column)
    {
        if (root == null)
            return null;

        var finder = new GDPositionFinder(root);
        return finder.FindNodeAtPosition(line, column);
    }

    /// <summary>
    /// Finds all identifiers with the given name.
    /// Note: This still requires full tree traversal as it's a search by name, not position.
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
}
