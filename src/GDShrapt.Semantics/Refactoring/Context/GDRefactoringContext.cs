using System;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Godot-independent context for refactoring operations.
/// Contains cursor position, selection info, and AST context.
/// </summary>
public class GDRefactoringContext
{
    private readonly GDPositionFinder _finder;

    /// <summary>
    /// The script file being refactored.
    /// </summary>
    public GDScriptFile Script { get; }

    /// <summary>
    /// The project containing the script (for cross-file operations).
    /// </summary>
    public GDScriptProject? Project { get; }

    /// <summary>
    /// Cursor position in the source file.
    /// </summary>
    public GDCursorPosition Cursor { get; }

    /// <summary>
    /// Selection info (start, end, text).
    /// </summary>
    public GDSelectionInfo Selection { get; }

    /// <summary>
    /// The class declaration (root AST node).
    /// </summary>
    public GDClassDeclaration ClassDeclaration { get; }

    /// <summary>
    /// Token at the cursor position (may be null).
    /// </summary>
    public GDSyntaxToken? TokenAtCursor { get; }

    /// <summary>
    /// Node at the cursor position (parent of token).
    /// </summary>
    public GDNode? NodeAtCursor { get; }

    /// <summary>
    /// The method containing the cursor (null if at class level).
    /// </summary>
    public GDMethodDeclaration? ContainingMethod { get; }

    /// <summary>
    /// The expression selected (if selection matches an expression).
    /// </summary>
    public GDExpression? SelectedExpression { get; }

    #region Computed Properties

    /// <summary>
    /// Whether there is an active selection.
    /// </summary>
    public bool HasSelection => Selection.HasSelection;

    /// <summary>
    /// Whether the cursor is on an identifier.
    /// </summary>
    public bool IsOnIdentifier => TokenAtCursor is GDIdentifier;

    /// <summary>
    /// Gets the identifier at cursor (if any).
    /// </summary>
    public GDIdentifier? IdentifierAtCursor => TokenAtCursor as GDIdentifier;

    /// <summary>
    /// Whether the cursor is on a literal expression (number, string, bool).
    /// </summary>
    public bool IsOnLiteral => NodeAtCursor is GDNumberExpression
                              or GDStringExpression
                              or GDBoolExpression;

    /// <summary>
    /// Whether the cursor is on a number literal.
    /// </summary>
    public bool IsOnNumber => NodeAtCursor is GDNumberExpression;

    /// <summary>
    /// Whether the cursor is on a string literal.
    /// </summary>
    public bool IsOnString => NodeAtCursor is GDStringExpression;

    /// <summary>
    /// Whether the cursor is on a boolean literal.
    /// </summary>
    public bool IsOnBool => NodeAtCursor is GDBoolExpression;

    /// <summary>
    /// Whether the cursor is in an if condition (not the body).
    /// </summary>
    public bool IsInIfCondition { get; }

    /// <summary>
    /// Whether the cursor is on a get_node() call or $NodePath.
    /// </summary>
    public bool IsOnGetNodeCall { get; }

    /// <summary>
    /// Whether the cursor is on a $NodePath or @"NodePath" expression.
    /// </summary>
    public bool IsOnNodePath { get; }

    /// <summary>
    /// Whether the cursor is on a class-level variable (not local).
    /// </summary>
    public bool IsOnClassVariable { get; }

    /// <summary>
    /// Whether statements are selected (for extract method, surround).
    /// </summary>
    public bool HasStatementsSelected { get; }

    /// <summary>
    /// Whether an expression is selected (for extract variable/constant).
    /// </summary>
    public bool HasExpressionSelected { get; }

    #endregion

    #region Statement Detection

    /// <summary>
    /// Whether the cursor is on an if statement.
    /// </summary>
    public bool IsOnIfStatement => NodeAtCursor is GDIfStatement
                                  || FindParent<GDIfStatement>() != null;

    /// <summary>
    /// Whether the cursor is on a while statement.
    /// </summary>
    public bool IsOnWhileStatement => NodeAtCursor is GDWhileStatement
                                     || FindParent<GDWhileStatement>() != null;

    /// <summary>
    /// Whether the cursor is on a for statement.
    /// </summary>
    public bool IsOnForStatement => NodeAtCursor is GDForStatement
                                   || FindParent<GDForStatement>() != null;

    /// <summary>
    /// Whether the cursor is on a match statement.
    /// </summary>
    public bool IsOnMatchStatement => NodeAtCursor is GDMatchStatement
                                     || FindParent<GDMatchStatement>() != null;

    /// <summary>
    /// Whether the cursor is on a variable declaration.
    /// </summary>
    public bool IsOnVariableDeclaration => NodeAtCursor is GDVariableDeclaration
                                          || FindParent<GDVariableDeclaration>() != null;

    /// <summary>
    /// Whether the cursor is on a method declaration.
    /// </summary>
    public bool IsOnMethodDeclaration => NodeAtCursor is GDMethodDeclaration
                                        || FindParent<GDMethodDeclaration>() != null;

    #endregion

    public GDRefactoringContext(
        GDScriptFile script,
        GDClassDeclaration classDeclaration,
        GDCursorPosition cursor,
        GDSelectionInfo selection,
        GDScriptProject? project = null)
    {
        Script = script ?? throw new ArgumentNullException(nameof(script));
        ClassDeclaration = classDeclaration ?? throw new ArgumentNullException(nameof(classDeclaration));
        Cursor = cursor;
        Selection = selection ?? GDSelectionInfo.None;
        Project = project;

        _finder = new GDPositionFinder(classDeclaration);

        // Resolve token and node at cursor
        TokenAtCursor = _finder.FindTokenAtPosition(cursor.Line, cursor.Column);
        NodeAtCursor = TokenAtCursor?.Parent;

        // Find containing method
        ContainingMethod = _finder.FindContainingMethod(cursor.Line, cursor.Column);

        // Compute condition-based properties
        IsInIfCondition = _finder.IsInIfCondition(cursor.Line, cursor.Column);
        IsOnGetNodeCall = _finder.IsOnGetNodeCall(cursor.Line, cursor.Column);
        IsOnNodePath = NodeAtCursor is GDNodePathExpression
                      || GDPositionFinder.FindParent<GDNodePathExpression>(NodeAtCursor) != null
                      || NodeAtCursor is GDGetNodeExpression
                      || GDPositionFinder.FindParent<GDGetNodeExpression>(NodeAtCursor) != null;
        IsOnClassVariable = _finder.IsOnClassVariable(cursor.Line, cursor.Column);

        // Compute selection-based properties
        if (selection.HasSelection)
        {
            HasStatementsSelected = _finder.HasStatementsSelected(
                selection.StartLine, selection.StartColumn,
                selection.EndLine, selection.EndColumn);
            HasExpressionSelected = _finder.HasExpressionSelected(
                selection.StartLine, selection.StartColumn,
                selection.EndLine, selection.EndColumn);
            SelectedExpression = HasExpressionSelected
                ? _finder.FindExpressionAtSelection(
                    selection.StartLine, selection.StartColumn,
                    selection.EndLine, selection.EndColumn)
                : null;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Finds a parent node of the specified type from the node at cursor.
    /// </summary>
    public T? FindParent<T>() where T : GDNode
    {
        return GDPositionFinder.FindParent<T>(NodeAtCursor);
    }

    /// <summary>
    /// Gets the variable declaration at cursor if any.
    /// </summary>
    public GDVariableDeclaration? GetVariableDeclaration()
    {
        if (NodeAtCursor is GDVariableDeclaration varDecl)
            return varDecl;
        return FindParent<GDVariableDeclaration>();
    }

    /// <summary>
    /// Gets the if statement at cursor if any.
    /// </summary>
    public GDIfStatement? GetIfStatement()
    {
        if (NodeAtCursor is GDIfStatement ifStmt)
            return ifStmt;
        return FindParent<GDIfStatement>();
    }

    /// <summary>
    /// Gets the match statement at cursor if any.
    /// </summary>
    public GDMatchStatement? GetMatchStatement()
    {
        if (NodeAtCursor is GDMatchStatement matchStmt)
            return matchStmt;
        return FindParent<GDMatchStatement>();
    }

    /// <summary>
    /// Gets the for statement at cursor if any.
    /// </summary>
    public GDForStatement? GetForStatement()
    {
        if (NodeAtCursor is GDForStatement forStmt)
            return forStmt;
        return FindParent<GDForStatement>();
    }

    /// <summary>
    /// Gets the while statement at cursor if any.
    /// </summary>
    public GDWhileStatement? GetWhileStatement()
    {
        if (NodeAtCursor is GDWhileStatement whileStmt)
            return whileStmt;
        return FindParent<GDWhileStatement>();
    }

    /// <summary>
    /// Gets the semantic model for the script.
    /// </summary>
    public GDSemanticModel? GetSemanticModel() => Script.SemanticModel;

    #endregion
}
