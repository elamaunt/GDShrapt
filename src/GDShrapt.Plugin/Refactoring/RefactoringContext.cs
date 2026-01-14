using GDShrapt.Reader;
using GDShrapt.Semantics;
using Godot;

namespace GDShrapt.Plugin;

/// <summary>
/// Context for determining refactoring availability and execution.
/// Contains information about cursor position, selection, and AST nodes.
/// </summary>
internal class RefactoringContext
{
    /// <summary>
    /// The script editor instance.
    /// </summary>
    public IScriptEditor Editor { get; init; }

    /// <summary>
    /// The script map containing AST and type information.
    /// </summary>
    public GDScriptFile ScriptFile { get; init; }

    /// <summary>
    /// The project map for cross-file operations.
    /// </summary>
    public GDScriptProject ScriptProject { get; init; }

    /// <summary>
    /// Parent node for dialogs (typically the plugin node).
    /// </summary>
    public Node DialogParent { get; init; }

    /// <summary>
    /// Cursor line (0-based).
    /// </summary>
    public int CursorLine { get; init; }

    /// <summary>
    /// Cursor column (0-based).
    /// </summary>
    public int CursorColumn { get; init; }

    /// <summary>
    /// Selected text (empty string if no selection).
    /// </summary>
    public string SelectedText { get; init; } = string.Empty;

    /// <summary>
    /// Selection start line (0-based).
    /// </summary>
    public int SelectionStartLine { get; init; }

    /// <summary>
    /// Selection start column (0-based).
    /// </summary>
    public int SelectionStartColumn { get; init; }

    /// <summary>
    /// Selection end line (0-based).
    /// </summary>
    public int SelectionEndLine { get; init; }

    /// <summary>
    /// Selection end column (0-based).
    /// </summary>
    public int SelectionEndColumn { get; init; }

    /// <summary>
    /// AST node at cursor position (may be null).
    /// </summary>
    public GDNode NodeAtCursor { get; init; }

    /// <summary>
    /// Token at cursor position (may be null).
    /// </summary>
    public GDSyntaxToken TokenAtCursor { get; init; }

    /// <summary>
    /// Parent method containing the cursor (may be null if at class level).
    /// </summary>
    public GDMethodDeclaration ContainingMethod { get; init; }

    /// <summary>
    /// Parent class declaration.
    /// </summary>
    public GDClassDeclaration ContainingClass { get; init; }

    /// <summary>
    /// Whether there is an active selection.
    /// </summary>
    public bool HasSelection => !string.IsNullOrEmpty(SelectedText);

    /// <summary>
    /// Whether the cursor is on an identifier token.
    /// </summary>
    public bool IsOnIdentifier => TokenAtCursor is GDIdentifier;

    /// <summary>
    /// Whether the cursor is on a literal expression.
    /// </summary>
    public bool IsOnLiteral => NodeAtCursor is GDNumberExpression
                             || NodeAtCursor is GDStringExpression
                             || NodeAtCursor is GDBoolExpression;

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
    /// Whether the cursor is in an if condition.
    /// </summary>
    public bool IsInIfCondition { get; init; }

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
    /// Whether the cursor is on a get_node() call.
    /// </summary>
    public bool IsOnGetNodeCall { get; init; }

    /// <summary>
    /// Whether the cursor is on a $NodePath expression.
    /// </summary>
    public bool IsOnNodePath { get; init; }

    /// <summary>
    /// Whether the cursor is on a variable declaration.
    /// </summary>
    public bool IsOnVariableDeclaration => NodeAtCursor is GDVariableDeclaration
                                         || FindParent<GDVariableDeclaration>() != null;

    /// <summary>
    /// Whether the cursor is on a class member variable (not local).
    /// </summary>
    public bool IsOnClassVariable { get; init; }

    /// <summary>
    /// Whether the cursor is on a method declaration.
    /// </summary>
    public bool IsOnMethodDeclaration => NodeAtCursor is GDMethodDeclaration
                                       || FindParent<GDMethodDeclaration>() != null;

    /// <summary>
    /// Whether statements are selected (for extract method, surround).
    /// </summary>
    public bool HasStatementsSelected { get; init; }

    /// <summary>
    /// Whether an expression is selected (for extract variable/constant).
    /// </summary>
    public bool HasExpressionSelected { get; init; }

    /// <summary>
    /// The selected expression node (if any).
    /// </summary>
    public GDExpression SelectedExpression { get; init; }

    /// <summary>
    /// Finds a parent node of the specified type.
    /// </summary>
    public T FindParent<T>() where T : GDNode
    {
        var node = NodeAtCursor;
        while (node != null)
        {
            if (node is T result)
                return result;
            node = node.Parent as GDNode;
        }
        return null;
    }

    /// <summary>
    /// Gets the variable declaration at cursor if any.
    /// </summary>
    public GDVariableDeclaration GetVariableDeclaration()
    {
        if (NodeAtCursor is GDVariableDeclaration varDecl)
            return varDecl;
        return FindParent<GDVariableDeclaration>();
    }

    /// <summary>
    /// Gets the if statement at cursor if any.
    /// </summary>
    public GDIfStatement GetIfStatement()
    {
        if (NodeAtCursor is GDIfStatement ifStmt)
            return ifStmt;
        return FindParent<GDIfStatement>();
    }

    /// <summary>
    /// Gets the match statement at cursor if any.
    /// </summary>
    public GDMatchStatement GetMatchStatement()
    {
        if (NodeAtCursor is GDMatchStatement matchStmt)
            return matchStmt;
        return FindParent<GDMatchStatement>();
    }

    /// <summary>
    /// Gets the for statement at cursor if any.
    /// </summary>
    public GDForStatement GetForStatement()
    {
        if (NodeAtCursor is GDForStatement forStmt)
            return forStmt;
        return FindParent<GDForStatement>();
    }

    /// <summary>
    /// Gets the while statement at cursor if any.
    /// </summary>
    public GDWhileStatement GetWhileStatement()
    {
        if (NodeAtCursor is GDWhileStatement whileStmt)
            return whileStmt;
        return FindParent<GDWhileStatement>();
    }

    /// <summary>
    /// Creates a GDRefactoringContext for use with Semantics services.
    /// </summary>
    public GDRefactoringContext BuildSemanticsContext()
    {
        if (ContainingClass == null)
            return null;

        // Create a GDScriptFile wrapper for the context
        var reference = new GDScriptReference(ScriptFile?.FullPath ?? "unknown.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(ContainingClass.ToString());

        // Build selection info
        var selection = HasSelection
            ? new GDSelectionInfo(
                SelectionStartLine,
                SelectionStartColumn,
                SelectionEndLine,
                SelectionEndColumn,
                SelectedText)
            : GDSelectionInfo.None;

        // Build cursor position
        var cursor = new GDCursorPosition(CursorLine, CursorColumn);

        return new GDRefactoringContext(
            scriptFile,
            ContainingClass,
            cursor,
            selection);
    }
}
