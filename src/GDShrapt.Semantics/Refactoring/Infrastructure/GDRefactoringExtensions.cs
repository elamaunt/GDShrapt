using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Extension methods for common refactoring operations.
/// </summary>
public static class GDRefactoringExtensions
{
    /// <summary>
    /// Finds all nodes of type T matching an optional predicate.
    /// </summary>
    public static IEnumerable<T> FindNodes<T>(this GDNode root, Func<T, bool>? predicate = null) where T : GDNode
    {
        if (root == null) return Enumerable.Empty<T>();
        var nodes = root.AllNodes.OfType<T>();
        return predicate != null ? nodes.Where(predicate) : nodes;
    }

    /// <summary>
    /// Finds all tokens of type T matching an optional predicate.
    /// </summary>
    public static IEnumerable<T> FindTokens<T>(this GDNode root, Func<T, bool>? predicate = null) where T : GDSyntaxToken
    {
        if (root == null) return Enumerable.Empty<T>();
        var tokens = root.AllTokens.OfType<T>();
        return predicate != null ? tokens.Where(predicate) : tokens;
    }

    /// <summary>
    /// Finds all identifiers with a specific name.
    /// </summary>
    public static IEnumerable<GDIdentifier> FindIdentifiers(this GDNode root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return Enumerable.Empty<GDIdentifier>();
        return root.AllTokens.OfType<GDIdentifier>()
            .Where(id => string.Equals(id.Sequence, name, StringComparison.Ordinal));
    }

    /// <summary>
    /// Safely gets the identifier sequence.
    /// </summary>
    public static string? GetName(this GDIdentifier? identifier)
        => identifier?.Sequence;

    /// <summary>
    /// Gets the indentation string for a node.
    /// </summary>
    public static string GetIndentation(this GDNode node)
        => GDIndentationUtilities.GetIndentation(node);

    /// <summary>
    /// Gets the indentation string for a statement.
    /// </summary>
    public static string GetIndentation(this GDStatement statement)
        => GDIndentationUtilities.GetIndentation(statement);

    /// <summary>
    /// Checks if an expression is a literal (number, string, bool).
    /// </summary>
    public static bool IsLiteral(this GDExpression? expression)
        => expression is GDNumberExpression or GDStringExpression or GDBoolExpression;

    /// <summary>
    /// Checks if a node is inside a method.
    /// </summary>
    public static bool IsInsideMethod(this GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration)
                return true;
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Gets the containing method for a node.
    /// </summary>
    public static GDMethodDeclaration? GetContainingMethod(this GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent as GDNode;
        }
        return null;
    }

    /// <summary>
    /// Gets the containing statement for a node.
    /// </summary>
    public static GDStatement? GetContainingStatement(this GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDStatement stmt)
                return stmt;
            current = current.Parent as GDNode;
        }
        return null;
    }

    /// <summary>
    /// Gets the containing class member for a node.
    /// </summary>
    public static GDClassMember? GetContainingMember(this GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDClassMember member)
                return member;
            current = current.Parent as GDNode;
        }
        return null;
    }

    /// <summary>
    /// Creates a text edit builder for this context.
    /// </summary>
    internal static GDTextEditBuilder CreateEditBuilder(this GDRefactoringContext context)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDTextEditBuilder.ForFile(filePath);
    }
}
