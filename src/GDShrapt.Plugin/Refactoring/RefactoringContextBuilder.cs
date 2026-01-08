using GDShrapt.Reader;
using Godot;
using System.Linq;

namespace GDShrapt.Plugin.Refactoring;

/// <summary>
/// Builds RefactoringContext from editor state.
/// </summary>
internal class RefactoringContextBuilder
{
    private readonly GDProjectMap _projectMap;
    private readonly Node _dialogParent;

    public RefactoringContextBuilder(GDProjectMap projectMap, Node dialogParent = null)
    {
        _projectMap = projectMap;
        _dialogParent = dialogParent;
    }

    /// <summary>
    /// Builds a refactoring context from the current editor state.
    /// </summary>
    public RefactoringContext Build(IScriptEditor editor)
    {
        if (editor == null)
            return null;

        var scriptMap = editor.ScriptMap;
        var @class = editor.GetClass();

        var cursorLine = editor.CursorLine;
        var cursorColumn = editor.CursorColumn;

        var hasSelection = editor.HasSelection;
        var selectedText = hasSelection ? GetSelectedText(editor) : string.Empty;

        // Find node and token at cursor position
        var nodeAtCursor = FindNodeAtPosition(@class, cursorLine, cursorColumn);
        var tokenAtCursor = FindTokenAtPosition(@class, cursorLine, cursorColumn);

        // Find containing method
        var containingMethod = FindContainingMethod(nodeAtCursor ?? tokenAtCursor as GDNode);

        // Determine if we're on a get_node call or $NodePath
        var isOnGetNodeCall = IsGetNodeCall(nodeAtCursor);
        var isOnNodePath = nodeAtCursor is GDNodePathExpression
                        || FindParent<GDNodePathExpression>(nodeAtCursor) != null;

        // Determine if cursor is in if condition
        var isInIfCondition = IsInIfCondition(nodeAtCursor);

        // Determine if on class variable
        var isOnClassVariable = IsClassVariable(nodeAtCursor);

        // Check for selected statements/expressions
        var hasStatementsSelected = hasSelection && HasStatementsSelected(editor, @class);
        var hasExpressionSelected = hasSelection && HasExpressionSelected(editor, @class);
        var selectedExpression = hasExpressionSelected ? FindSelectedExpression(editor, @class) : null;

        return new RefactoringContext
        {
            Editor = editor,
            ScriptMap = scriptMap,
            ProjectMap = _projectMap,
            DialogParent = _dialogParent,
            CursorLine = cursorLine,
            CursorColumn = cursorColumn,
            SelectedText = selectedText,
            SelectionStartLine = editor.SelectionStartLine,
            SelectionStartColumn = editor.SelectionStartColumn,
            SelectionEndLine = editor.SelectionEndLine,
            SelectionEndColumn = editor.SelectionEndColumn,
            NodeAtCursor = nodeAtCursor,
            TokenAtCursor = tokenAtCursor,
            ContainingMethod = containingMethod,
            ContainingClass = @class,
            IsInIfCondition = isInIfCondition,
            IsOnGetNodeCall = isOnGetNodeCall,
            IsOnNodePath = isOnNodePath,
            IsOnClassVariable = isOnClassVariable,
            HasStatementsSelected = hasStatementsSelected,
            HasExpressionSelected = hasExpressionSelected,
            SelectedExpression = selectedExpression
        };
    }

    private string GetSelectedText(IScriptEditor editor)
    {
        var startLine = editor.SelectionStartLine;
        var startCol = editor.SelectionStartColumn;
        var endLine = editor.SelectionEndLine;
        var endCol = editor.SelectionEndColumn;

        if (startLine == endLine)
        {
            var line = editor.GetLine(startLine);
            if (startCol < line.Length && endCol <= line.Length)
                return line.Substring(startCol, endCol - startCol);
            return string.Empty;
        }

        // Multi-line selection
        var result = new System.Text.StringBuilder();
        for (int i = startLine; i <= endLine; i++)
        {
            var line = editor.GetLine(i);
            if (i == startLine)
                result.AppendLine(line.Substring(startCol));
            else if (i == endLine)
                result.Append(line.Substring(0, System.Math.Min(endCol, line.Length)));
            else
                result.AppendLine(line);
        }
        return result.ToString();
    }

    private GDNode FindNodeAtPosition(GDClassDeclaration @class, int line, int column)
    {
        if (@class == null)
            return null;

        GDNode bestMatch = null;
        int bestLength = int.MaxValue;

        foreach (var node in @class.AllNodes)
        {
            if (node.StartLine <= line && node.EndLine >= line)
            {
                // Check if cursor is within this node's column range
                if (node.StartLine == line && node.StartColumn > column)
                    continue;
                if (node.EndLine == line && node.EndColumn < column)
                    continue;

                // Prefer smaller (more specific) nodes
                var length = CalculateNodeLength(node);
                if (length < bestLength)
                {
                    bestLength = length;
                    bestMatch = node;
                }
            }
        }

        return bestMatch;
    }

    private GDSyntaxToken FindTokenAtPosition(GDClassDeclaration @class, int line, int column)
    {
        if (@class == null)
            return null;

        foreach (var token in @class.AllTokens)
        {
            if (token.StartLine == line && token.StartColumn <= column && token.EndColumn >= column)
                return token;
            if (token.StartLine < line && token.EndLine > line)
                return token;
            if (token.StartLine < line && token.EndLine == line && token.EndColumn >= column)
                return token;
            if (token.StartLine == line && token.EndLine > line && token.StartColumn <= column)
                return token;
        }

        return null;
    }

    private int CalculateNodeLength(GDNode node)
    {
        if (node.StartLine == node.EndLine)
            return node.EndColumn - node.StartColumn;

        // Approximate length for multi-line nodes
        return (node.EndLine - node.StartLine) * 100 + node.EndColumn;
    }

    private GDMethodDeclaration FindContainingMethod(GDNode node)
    {
        while (node != null)
        {
            if (node is GDMethodDeclaration method)
                return method;
            node = node.Parent as GDNode;
        }
        return null;
    }

    private T FindParent<T>(GDNode node) where T : GDNode
    {
        while (node != null)
        {
            if (node is T result)
                return result;
            node = node.Parent as GDNode;
        }
        return null;
    }

    private bool IsGetNodeCall(GDNode node)
    {
        if (node is GDCallExpression call)
        {
            var calledExpr = call.CallerExpression?.ToString();
            return calledExpr == "get_node" || calledExpr == "get_node_or_null";
        }

        // Check if inside a get_node call
        var callParent = FindParent<GDCallExpression>(node);
        if (callParent != null)
        {
            var calledExpr = callParent.CallerExpression?.ToString();
            return calledExpr == "get_node" || calledExpr == "get_node_or_null";
        }

        return false;
    }

    private bool IsInIfCondition(GDNode node)
    {
        var ifStmt = FindParent<GDIfStatement>(node);
        if (ifStmt == null)
            return false;

        // Check if node is part of the condition (not the body)
        if (ifStmt.IfBranch?.Condition != null)
        {
            var condition = ifStmt.IfBranch.Condition;
            return IsNodeInsideExpression(node, condition);
        }

        return false;
    }

    private bool IsNodeInsideExpression(GDNode node, GDExpression expression)
    {
        if (node == expression)
            return true;

        foreach (var child in expression.AllNodes)
        {
            if (child == node)
                return true;
        }

        return false;
    }

    private bool IsClassVariable(GDNode node)
    {
        var varDecl = node as GDVariableDeclaration ?? FindParent<GDVariableDeclaration>(node);
        if (varDecl == null)
            return false;

        // Check if parent is class members (not inside a method)
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

    private bool HasStatementsSelected(IScriptEditor editor, GDClassDeclaration @class)
    {
        if (@class == null)
            return false;

        var startLine = editor.SelectionStartLine;
        var endLine = editor.SelectionEndLine;

        // Check if selection spans at least one full statement
        foreach (var statementsList in @class.AllNodes.OfType<GDStatementsList>())
        {
            foreach (var statement in statementsList.OfType<GDStatement>())
            {
                if (statement.StartLine >= startLine && statement.EndLine <= endLine)
                    return true;
            }
        }

        return false;
    }

    private bool HasExpressionSelected(IScriptEditor editor, GDClassDeclaration @class)
    {
        if (@class == null)
            return false;

        var startLine = editor.SelectionStartLine;
        var startCol = editor.SelectionStartColumn;
        var endLine = editor.SelectionEndLine;
        var endCol = editor.SelectionEndColumn;

        // Check if selection matches an expression
        foreach (var expr in @class.AllNodes.OfType<GDExpression>())
        {
            if (expr.StartLine == startLine && expr.StartColumn == startCol &&
                expr.EndLine == endLine && expr.EndColumn == endCol)
                return true;

            // Also check for partial match within single line
            if (expr.StartLine == startLine && expr.EndLine == endLine &&
                expr.StartLine == endLine &&
                expr.StartColumn >= startCol && expr.EndColumn <= endCol)
                return true;
        }

        return false;
    }

    private GDExpression FindSelectedExpression(IScriptEditor editor, GDClassDeclaration @class)
    {
        if (@class == null)
            return null;

        var startLine = editor.SelectionStartLine;
        var startCol = editor.SelectionStartColumn;
        var endLine = editor.SelectionEndLine;
        var endCol = editor.SelectionEndColumn;

        GDExpression bestMatch = null;
        int bestLength = int.MaxValue;

        foreach (var expr in @class.AllNodes.OfType<GDExpression>())
        {
            // Check if expression is within selection
            if (expr.StartLine >= startLine && expr.EndLine <= endLine)
            {
                if (expr.StartLine == startLine && expr.StartColumn < startCol)
                    continue;
                if (expr.EndLine == endLine && expr.EndColumn > endCol)
                    continue;

                var length = CalculateNodeLength(expr);
                if (length < bestLength)
                {
                    bestLength = length;
                    bestMatch = expr;
                }
            }
        }

        return bestMatch;
    }
}
