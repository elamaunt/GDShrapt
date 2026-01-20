using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Base class for traversing GDScript statements recursively.
/// Override <see cref="ProcessStatement"/> to handle each statement.
/// Override Before*/After* methods for hooks at branch entry/exit points.
/// </summary>
internal abstract class GDStatementTraverser
{
    /// <summary>
    /// Called for each statement before traversing nested statements.
    /// </summary>
    protected abstract void ProcessStatement(GDStatement stmt);

    #region Branch Hooks

    /// <summary>Called before entering the if branch.</summary>
    protected virtual void BeforeIfBranch(GDIfBranch branch) { }
    /// <summary>Called after leaving the if branch.</summary>
    protected virtual void AfterIfBranch(GDIfBranch branch) { }

    /// <summary>Called before entering an elif branch.</summary>
    protected virtual void BeforeElifBranch(GDElifBranch branch) { }
    /// <summary>Called after leaving an elif branch.</summary>
    protected virtual void AfterElifBranch(GDElifBranch branch) { }

    /// <summary>Called before entering the else branch.</summary>
    protected virtual void BeforeElseBranch(GDElseBranch branch) { }
    /// <summary>Called after leaving the else branch.</summary>
    protected virtual void AfterElseBranch(GDElseBranch branch) { }

    /// <summary>Called before entering a for loop body.</summary>
    protected virtual void BeforeForLoop(GDForStatement forStmt) { }
    /// <summary>Called after leaving a for loop body.</summary>
    protected virtual void AfterForLoop(GDForStatement forStmt) { }

    /// <summary>Called before entering a while loop body.</summary>
    protected virtual void BeforeWhileLoop(GDWhileStatement whileStmt) { }
    /// <summary>Called after leaving a while loop body.</summary>
    protected virtual void AfterWhileLoop(GDWhileStatement whileStmt) { }

    /// <summary>Called before entering a match case block.</summary>
    protected virtual void BeforeMatchCase(GDMatchCaseDeclaration matchCase) { }
    /// <summary>Called after leaving a match case block.</summary>
    protected virtual void AfterMatchCase(GDMatchCaseDeclaration matchCase) { }

    #endregion

    /// <summary>
    /// Traverses all statements in the list, calling <see cref="ProcessStatement"/>
    /// for each and recursively traversing nested statements.
    /// </summary>
    public void TraverseStatements(IEnumerable<GDStatement>? statements)
    {
        if (statements == null)
            return;

        foreach (var stmt in statements)
        {
            ProcessStatement(stmt);
            TraverseNestedStatements(stmt);
        }
    }

    private void TraverseNestedStatements(GDStatement stmt)
    {
        switch (stmt)
        {
            case GDIfStatement ifStmt:
                TraverseIfStatement(ifStmt);
                break;
            case GDForStatement forStmt:
                TraverseForStatement(forStmt);
                break;
            case GDWhileStatement whileStmt:
                TraverseWhileStatement(whileStmt);
                break;
            case GDMatchStatement matchStmt:
                TraverseMatchStatement(matchStmt);
                break;
        }
    }

    private void TraverseIfStatement(GDIfStatement ifStmt)
    {
        if (ifStmt.IfBranch?.Statements != null)
        {
            BeforeIfBranch(ifStmt.IfBranch);
            TraverseStatements(ifStmt.IfBranch.Statements);
            AfterIfBranch(ifStmt.IfBranch);
        }

        if (ifStmt.ElifBranchesList != null)
        {
            foreach (var elif in ifStmt.ElifBranchesList)
            {
                if (elif?.Statements != null)
                {
                    BeforeElifBranch(elif);
                    TraverseStatements(elif.Statements);
                    AfterElifBranch(elif);
                }
            }
        }

        if (ifStmt.ElseBranch?.Statements != null)
        {
            BeforeElseBranch(ifStmt.ElseBranch);
            TraverseStatements(ifStmt.ElseBranch.Statements);
            AfterElseBranch(ifStmt.ElseBranch);
        }
    }

    private void TraverseForStatement(GDForStatement forStmt)
    {
        if (forStmt.Statements == null)
            return;

        BeforeForLoop(forStmt);
        TraverseStatements(forStmt.Statements);
        AfterForLoop(forStmt);
    }

    private void TraverseWhileStatement(GDWhileStatement whileStmt)
    {
        if (whileStmt.Statements == null)
            return;

        BeforeWhileLoop(whileStmt);
        TraverseStatements(whileStmt.Statements);
        AfterWhileLoop(whileStmt);
    }

    private void TraverseMatchStatement(GDMatchStatement matchStmt)
    {
        if (matchStmt.Cases == null)
            return;

        foreach (var matchCase in matchStmt.Cases)
        {
            if (matchCase?.Statements != null)
            {
                BeforeMatchCase(matchCase);
                TraverseStatements(matchCase.Statements);
                AfterMatchCase(matchCase);
            }
        }
    }
}
