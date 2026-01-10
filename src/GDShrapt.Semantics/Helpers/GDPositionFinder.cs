using GDShrapt.Reader;

namespace GDShrapt.Reader;

/// <summary>
/// Utility class for finding AST nodes and tokens at specific positions.
/// Uses optimized TryGetTokenByPosition with early exit instead of full AllTokens enumeration.
/// </summary>
public class GDPositionFinder
{
    private readonly GDNode _root;

    public GDPositionFinder(GDNode root)
    {
        _root = root ?? throw new System.ArgumentNullException(nameof(root));
    }

    /// <summary>
    /// Finds the token at the specified position using optimized TryGetTokenByPosition.
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>The token at the position or null if not found</returns>
    public GDSyntaxToken FindTokenAtPosition(int line, int column)
    {
        if (_root.TryGetTokenByPosition(line, column, out var token))
            return token;
        return null;
    }

    /// <summary>
    /// Finds the identifier at the specified position.
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>The identifier at the position or null if the token is not an identifier</returns>
    public GDIdentifier FindIdentifierAtPosition(int line, int column)
    {
        var token = FindTokenAtPosition(line, column);
        return token as GDIdentifier;
    }

    /// <summary>
    /// Finds the node at the specified position by navigating to the parent of the token.
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>The parent node of the token at the position or null if not found</returns>
    public GDNode FindNodeAtPosition(int line, int column)
    {
        var token = FindTokenAtPosition(line, column);
        return token?.Parent;
    }

    /// <summary>
    /// Finds a node of the specified type at the position by walking up the parent chain.
    /// </summary>
    /// <typeparam name="T">The type of node to find</typeparam>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>The node of the specified type or null if not found</returns>
    public T FindNodeAtPosition<T>(int line, int column) where T : GDNode
    {
        var node = FindNodeAtPosition(line, column);
        while (node != null)
        {
            if (node is T result)
                return result;
            node = node.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds a parent node of the specified type starting from the given node.
    /// </summary>
    /// <typeparam name="T">The type of parent node to find</typeparam>
    /// <param name="node">The starting node</param>
    /// <returns>The parent node of the specified type or null if not found</returns>
    public static T FindParent<T>(GDNode node) where T : GDNode
    {
        while (node != null)
        {
            if (node is T result)
                return result;
            node = node.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds a parent node of the specified type starting from the given token.
    /// </summary>
    /// <typeparam name="T">The type of parent node to find</typeparam>
    /// <param name="token">The starting token</param>
    /// <returns>The parent node of the specified type or null if not found</returns>
    public static T FindParent<T>(GDSyntaxToken token) where T : GDNode
    {
        var parent = token?.Parent;
        while (parent != null)
        {
            if (parent is T result)
                return result;
            parent = parent.Parent;
        }
        return null;
    }

    /// <summary>
    /// Checks if the selection is within a node of the specified type.
    /// Uses TryGetTokenByPosition for efficient position lookup.
    /// </summary>
    /// <typeparam name="T">The type of node to check</typeparam>
    /// <param name="startLine">Selection start line (0-based)</param>
    /// <param name="startColumn">Selection start column (0-based)</param>
    /// <param name="endLine">Selection end line (0-based)</param>
    /// <param name="endColumn">Selection end column (0-based)</param>
    /// <param name="node">The found node if any</param>
    /// <returns>True if the selection is within a node of the specified type</returns>
    public bool IsSelectionInNode<T>(int startLine, int startColumn, int endLine, int endColumn, out T node) where T : GDNode
    {
        node = null;
        var startToken = FindTokenAtPosition(startLine, startColumn);
        if (startToken == null) return false;

        // Walk up the tree looking for a node of type T that contains the whole selection
        var parent = startToken.Parent;
        while (parent != null)
        {
            if (parent is T candidate)
            {
                // Check if the selection is entirely within this node
                if (IsNodeContainingSelection(candidate, startLine, startColumn, endLine, endColumn))
                {
                    node = candidate;
                    return true;
                }
            }
            parent = parent.Parent;
        }
        return false;
    }

    /// <summary>
    /// Finds the smallest expression that contains the given selection.
    /// </summary>
    /// <param name="startLine">Selection start line (0-based)</param>
    /// <param name="startColumn">Selection start column (0-based)</param>
    /// <param name="endLine">Selection end line (0-based)</param>
    /// <param name="endColumn">Selection end column (0-based)</param>
    /// <returns>The expression containing the selection or null if not found</returns>
    public GDExpression FindExpressionAtSelection(int startLine, int startColumn, int endLine, int endColumn)
    {
        var startToken = FindTokenAtPosition(startLine, startColumn);
        if (startToken == null) return null;

        // Walk up the tree looking for the smallest expression that contains the selection
        GDExpression bestMatch = null;
        var parent = startToken.Parent;

        while (parent != null)
        {
            if (parent is GDExpression expr)
            {
                if (IsNodeContainingSelection(expr, startLine, startColumn, endLine, endColumn))
                {
                    // Check if the expression exactly matches the selection
                    if (expr.StartLine == startLine && expr.StartColumn == startColumn &&
                        expr.EndLine == endLine && expr.EndColumn == endColumn)
                    {
                        return expr;
                    }

                    // Keep the smallest containing expression
                    if (bestMatch == null || IsNodeSmaller(expr, bestMatch))
                    {
                        bestMatch = expr;
                    }
                }
            }
            parent = parent.Parent;
        }

        return bestMatch;
    }

    /// <summary>
    /// Checks if the selection contains complete statements.
    /// </summary>
    /// <param name="startLine">Selection start line (0-based)</param>
    /// <param name="startColumn">Selection start column (0-based)</param>
    /// <param name="endLine">Selection end line (0-based)</param>
    /// <param name="endColumn">Selection end column (0-based)</param>
    /// <returns>True if the selection contains at least one complete statement</returns>
    public bool HasStatementsSelected(int startLine, int startColumn, int endLine, int endColumn)
    {
        var startToken = FindTokenAtPosition(startLine, startColumn);
        if (startToken == null) return false;

        // Find the containing statements list
        var statementsListNode = FindParent<GDStatementsList>(startToken);
        if (statementsListNode == null) return false;

        // Check if any statement is completely within the selection
        foreach (var statement in statementsListNode)
        {
            if (statement is GDStatement stmt)
            {
                if (stmt.StartLine >= startLine && stmt.EndLine <= endLine)
                {
                    // Check column bounds for same-line selections
                    if (stmt.StartLine == startLine && stmt.StartColumn < startColumn)
                        continue;
                    if (stmt.EndLine == endLine && stmt.EndColumn > endColumn)
                        continue;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the selection exactly matches an expression.
    /// </summary>
    /// <param name="startLine">Selection start line (0-based)</param>
    /// <param name="startColumn">Selection start column (0-based)</param>
    /// <param name="endLine">Selection end line (0-based)</param>
    /// <param name="endColumn">Selection end column (0-based)</param>
    /// <returns>True if an expression exactly matches the selection bounds</returns>
    public bool HasExpressionSelected(int startLine, int startColumn, int endLine, int endColumn)
    {
        var expr = FindExpressionAtSelection(startLine, startColumn, endLine, endColumn);
        if (expr == null) return false;

        // Check for exact match
        if (expr.StartLine == startLine && expr.StartColumn == startColumn &&
            expr.EndLine == endLine && expr.EndColumn == endColumn)
        {
            return true;
        }

        // Also allow if expression is contained within the selection on a single line
        if (startLine == endLine && expr.StartLine == startLine && expr.EndLine == endLine &&
            expr.StartColumn >= startColumn && expr.EndColumn <= endColumn)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the containing method for the token at the specified position.
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>The containing method declaration or null if not inside a method</returns>
    public GDMethodDeclaration FindContainingMethod(int line, int column)
    {
        var token = FindTokenAtPosition(line, column);
        if (token == null) return null;

        // Use ClassMember property if available for efficiency
        var classMember = token.ClassMember;
        if (classMember is GDMethodDeclaration method)
            return method;

        // Fallback to parent walk
        return FindParent<GDMethodDeclaration>(token);
    }

    /// <summary>
    /// Checks if the token at the specified position is on a get_node() call or $NodePath.
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>True if on get_node() call or $NodePath expression</returns>
    public bool IsOnGetNodeCall(int line, int column)
    {
        var node = FindNodeAtPosition(line, column);
        if (node == null) return false;

        // Check if it's a $NodePath expression (GDGetNodeExpression)
        if (node is GDGetNodeExpression || FindParent<GDGetNodeExpression>(node) != null)
            return true;

        // Check if it's a %UniqueNode expression (GDGetUniqueNodeExpression)
        if (node is GDGetUniqueNodeExpression || FindParent<GDGetUniqueNodeExpression>(node) != null)
            return true;

        // Check if it's a NodePath expression (@"NodePath" syntax)
        if (node is GDNodePathExpression || FindParent<GDNodePathExpression>(node) != null)
            return true;

        // Check if it's a get_node() call
        var call = node as GDCallExpression ?? FindParent<GDCallExpression>(node);
        if (call != null)
        {
            var calledExpr = call.CallerExpression?.ToString();
            return calledExpr == "get_node" || calledExpr == "get_node_or_null";
        }

        return false;
    }

    /// <summary>
    /// Checks if the token at the specified position is in an if condition (not the body).
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>True if in an if condition</returns>
    public bool IsInIfCondition(int line, int column)
    {
        var node = FindNodeAtPosition(line, column);
        if (node == null) return false;

        var ifStmt = node as GDIfStatement ?? FindParent<GDIfStatement>(node);
        if (ifStmt?.IfBranch?.Condition == null) return false;

        // Check if the node is part of the condition expression
        return IsNodeInsideExpression(node, ifStmt.IfBranch.Condition);
    }

    /// <summary>
    /// Checks if the token at the specified position is on a class-level variable (not local).
    /// </summary>
    /// <param name="line">0-based line number</param>
    /// <param name="column">0-based column number</param>
    /// <returns>True if on a class variable declaration</returns>
    public bool IsOnClassVariable(int line, int column)
    {
        var node = FindNodeAtPosition(line, column);
        if (node == null) return false;

        var varDecl = node as GDVariableDeclaration ?? FindParent<GDVariableDeclaration>(node);
        if (varDecl == null) return false;

        // Check if parent is class members list (not inside a method)
        var parent = varDecl.Parent;
        while (parent != null)
        {
            if (parent is GDMethodDeclaration)
                return false;
            if (parent is GDClassMembersList)
                return true;
            parent = (parent as GDNode)?.Parent;
        }

        return false;
    }

    private static bool IsNodeContainingSelection(GDNode node, int startLine, int startColumn, int endLine, int endColumn)
    {
        // Node must start before or at selection start
        if (node.StartLine > startLine) return false;
        if (node.StartLine == startLine && node.StartColumn > startColumn) return false;

        // Node must end after or at selection end
        if (node.EndLine < endLine) return false;
        if (node.EndLine == endLine && node.EndColumn < endColumn) return false;

        return true;
    }

    private static bool IsNodeSmaller(GDNode a, GDNode b)
    {
        var aLines = a.EndLine - a.StartLine;
        var bLines = b.EndLine - b.StartLine;

        if (aLines < bLines) return true;
        if (aLines > bLines) return false;

        // Same number of lines, compare by total length
        var aLength = (a.EndColumn - a.StartColumn);
        var bLength = (b.EndColumn - b.StartColumn);

        return aLength < bLength;
    }

    private static bool IsNodeInsideExpression(GDNode node, GDExpression expression)
    {
        if (node == expression) return true;

        // Walk up from node to see if we reach the expression
        var current = node;
        while (current != null)
        {
            if (current == expression) return true;
            current = current.Parent;
        }

        return false;
    }
}
