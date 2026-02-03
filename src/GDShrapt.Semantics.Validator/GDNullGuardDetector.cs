using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Detects null guard patterns in GDScript code.
/// Centralizes all null-check pattern recognition for use by validators.
/// </summary>
public static class GDNullGuardDetector
{
    /// <summary>
    /// Checks if the access node is guarded by a null check.
    /// Handles patterns like:
    /// - In 'and' expression: is_instance_valid(x) and x.visible, x != null and x.method(), x and x.property
    /// - In if-body: if x: x.method(), if x and ...: x.method()
    /// - In while-body: while x is T: x.method()
    /// - In ternary true-branch: x.method() if x is T else 0
    /// </summary>
    public static bool IsGuardedByNullCheck(GDNode accessNode, string varName)
    {
        // Check if we're in the right side of an 'and' expression with a null guard on the left
        if (IsGuardedByAndExpressionLeft(accessNode, varName))
            return true;

        // Check if we're in an if/elif/while body where the condition contains a truthiness guard
        if (IsInIfBodyWithTruthinessGuard(accessNode, varName))
            return true;

        // Check if we're in ternary true-branch with a type guard in condition
        if (IsInTernaryTrueBranch(accessNode, varName))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the access is protected by a preceding guard clause that handles the null case.
    /// Pattern: if not is_instance_valid(x): return   (or x == null: return)
    ///          x.property  # <-- x is guaranteed non-null here
    ///
    /// Also searches inside nested blocks (while, for, if bodies) for guard clauses.
    /// </summary>
    public static bool IsProtectedByGuardClause(GDNode accessNode, string varName)
    {
        // Find the containing statements list (could be method, while, for, if body)
        var (statements, container) = FindContainingStatementsList(accessNode);
        if (statements == null)
            return false;

        var stmtList = statements.ToList();
        if (stmtList.Count == 0)
            return false;

        // Find the statement containing our access
        var accessStatementIndex = -1;
        for (int i = 0; i < stmtList.Count; i++)
        {
            if (IsDescendantOf(accessNode, stmtList[i]))
            {
                accessStatementIndex = i;
                break;
            }
        }

        if (accessStatementIndex < 0)
            return false;

        // Look at preceding statements for guard clauses
        for (int i = 0; i < accessStatementIndex; i++)
        {
            if (stmtList[i] is GDIfStatement ifStmt)
            {
                if (IsGuardClauseForVariable(ifStmt, varName))
                    return true;
            }
        }

        // Also check parent blocks recursively
        if (container != null && container.Parent is GDNode)
        {
            if (IsProtectedByGuardClause(container, varName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the containing statements list for a node.
    /// Returns the statements list of the nearest enclosing block (while, for, if branch, method).
    /// Special case: if node is in if/elif/while condition, returns the containing block of that statement.
    /// </summary>
    private static (System.Collections.Generic.IEnumerable<GDStatement>? Statements, GDNode? Container) FindContainingStatementsList(GDNode? node)
    {
        var current = node?.Parent as GDNode;
        var previousNode = node;

        while (current != null)
        {
            // Special case: if we're in the condition of an if/elif/while, skip to parent block
            // because guard clauses in the same block should protect the condition
            if (current is GDIfBranch ifBranch && IsDescendantOf(previousNode, ifBranch.Condition))
            {
                // Skip this branch, continue to find parent block
                previousNode = current;
                current = current.Parent as GDNode;
                continue;
            }
            if (current is GDElifBranch elifBranch && IsDescendantOf(previousNode, elifBranch.Condition))
            {
                previousNode = current;
                current = current.Parent as GDNode;
                continue;
            }
            if (current is GDWhileStatement whileStmt && IsDescendantOf(previousNode, whileStmt.Condition))
            {
                previousNode = current;
                current = current.Parent as GDNode;
                continue;
            }

            // Check for while body
            if (current is GDWhileStatement ws)
                return (ws.Statements, ws);

            // Check for for body
            if (current is GDForStatement forStmt)
                return (forStmt.Statements, forStmt);

            // Check for if/elif/else bodies
            if (current is GDIfBranch ib)
                return (ib.Statements, ib);
            if (current is GDElifBranch eb)
                return (eb.Statements, eb);
            if (current is GDElseBranch elseBranch)
                return (elseBranch.Statements, elseBranch);

            // Check for method body (last resort)
            if (current is GDMethodDeclaration method)
                return (method.Statements, method);

            previousNode = current;
            current = current.Parent as GDNode;
        }
        return (null, null);
    }

    /// <summary>
    /// Checks if the access node is in the right side of an 'and' expression with a null guard on the left.
    /// Handles patterns like: is_instance_valid(x) and x.visible
    ///                        x != null and x.method()
    ///                        x and x.property
    /// </summary>
    private static bool IsGuardedByAndExpressionLeft(GDNode accessNode, string varName)
    {
        // Walk up the tree to find if we're in the right side of an 'and' expression
        var current = accessNode as GDNode;

        while (current != null)
        {
            if (current is GDDualOperatorExpression dualOp)
            {
                var opType = dualOp.Operator?.OperatorType;
                if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
                {
                    // Check if we're in the right side of the 'and'
                    if (IsDescendantOf(accessNode, dualOp.RightExpression))
                    {
                        // Check if the left side is a null guard for our variable
                        if (IsNullGuardFor(dualOp.LeftExpression, varName))
                            return true;
                    }
                }
            }

            current = current.Parent as GDNode;
        }

        return false;
    }

    /// <summary>
    /// Checks if access is in if/while body where the condition contains a truthiness guard.
    /// Handles: if obj: obj.method()
    ///          if obj and ...: obj.method()
    ///          if is_instance_valid(obj): obj.method()
    ///          while obj is Type: obj.method()
    /// </summary>
    private static bool IsInIfBodyWithTruthinessGuard(GDNode accessNode, string varName)
    {
        // Find containing GDIfBranch, GDElifBranch, or GDWhileStatement
        var current = accessNode?.Parent as GDNode;
        while (current != null)
        {
            if (current is GDIfBranch ifBranch)
            {
                // Check that accessNode is in body (Statements), not in condition
                if (ifBranch.Condition != null && !IsDescendantOf(accessNode, ifBranch.Condition))
                {
                    if (HasTruthinessGuardFor(ifBranch.Condition, varName))
                        return true;
                }
            }
            else if (current is GDElifBranch elifBranch)
            {
                if (elifBranch.Condition != null && !IsDescendantOf(accessNode, elifBranch.Condition))
                {
                    if (HasTruthinessGuardFor(elifBranch.Condition, varName))
                        return true;
                }
            }
            else if (current is GDWhileStatement whileStmt)
            {
                // Check that accessNode is in body (Statements), not in condition
                if (whileStmt.Condition != null && !IsDescendantOf(accessNode, whileStmt.Condition))
                {
                    if (HasTruthinessGuardFor(whileStmt.Condition, varName))
                        return true;
                }
            }
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Checks if access is in ternary true-branch where condition contains a type guard.
    /// Handles: value.length() if value is String else 0
    /// </summary>
    private static bool IsInTernaryTrueBranch(GDNode accessNode, string varName)
    {
        var current = accessNode?.Parent as GDNode;
        while (current != null)
        {
            // GDIfExpression - ternary conditional expression
            // Syntax: TrueExpression if Condition else FalseExpression
            if (current is GDIfExpression ifExpr)
            {
                // Check if accessNode is in TrueExpression (executed when condition is true)
                if (ifExpr.TrueExpression != null &&
                    IsDescendantOf(accessNode, ifExpr.TrueExpression))
                {
                    if (IsNullGuardFor(ifExpr.Condition, varName))
                        return true;
                }
            }
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Checks if condition contains a truthiness guard for the variable.
    /// </summary>
    private static bool HasTruthinessGuardFor(GDExpression? condition, string varName)
    {
        if (condition == null)
            return false;

        // Simple truthiness: if obj
        if (condition is GDIdentifierExpression ident && ident.Identifier?.Sequence == varName)
            return true;

        // Use existing IsNullGuardFor for other patterns
        // (is_instance_valid, != null, truthiness)
        if (IsNullGuardFor(condition, varName))
            return true;

        // and/or with guard on left: if obj and ... or if is_instance_valid(obj) and ...
        if (condition is GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;
            if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
            {
                // Check the left part of and
                if (HasTruthinessGuardFor(dualOp.LeftExpression, varName))
                    return true;
            }

            // or with type guards: if value is String or value is StringName
            // Both branches must guard the same variable - if either is true, var is non-null
            if (opType == GDDualOperatorType.Or || opType == GDDualOperatorType.Or2)
            {
                if (HasTruthinessGuardFor(dualOp.LeftExpression, varName) &&
                    HasTruthinessGuardFor(dualOp.RightExpression, varName))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the expression is a null guard for the specified variable.
    /// Recognizes: is_instance_valid(var), var != null, var (truthiness check)
    /// </summary>
    public static bool IsNullGuardFor(GDExpression? expr, string varName)
    {
        if (expr == null)
            return false;

        // is_instance_valid(var)
        if (expr is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDIdentifierExpression funcIdent &&
                funcIdent.Identifier?.Sequence == "is_instance_valid")
            {
                var args = callExpr.Parameters?.ToList();
                if (args != null && args.Count > 0 && args[0] is GDIdentifierExpression argIdent)
                {
                    if (argIdent.Identifier?.Sequence == varName)
                        return true;
                }
            }
        }

        // var != null
        if (expr is GDDualOperatorExpression eqOp &&
            eqOp.Operator?.OperatorType == GDDualOperatorType.NotEqual)
        {
            if (IsNullLiteral(eqOp.RightExpression) &&
                eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
                leftIdent.Identifier?.Sequence == varName)
                return true;

            if (IsNullLiteral(eqOp.LeftExpression) &&
                eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                rightIdent.Identifier?.Sequence == varName)
                return true;
        }

        // var is Type (type guard implies non-null)
        if (expr is GDDualOperatorExpression isOp &&
            isOp.Operator?.OperatorType == GDDualOperatorType.Is)
        {
            if (isOp.LeftExpression is GDIdentifierExpression leftIsIdent &&
                leftIsIdent.Identifier?.Sequence == varName)
                return true;
        }

        // var == non_null_value (equality with non-null implies var is non-null)
        // If var == "literal" then var cannot be null (equality would fail otherwise)
        if (expr is GDDualOperatorExpression eqOp2 &&
            eqOp2.Operator?.OperatorType == GDDualOperatorType.Equal)
        {
            // Check if left side is our variable
            if (eqOp2.LeftExpression is GDIdentifierExpression leftIdent2 &&
                leftIdent2.Identifier?.Sequence == varName)
            {
                if (IsNonNullExpression(eqOp2.RightExpression))
                    return true;
            }
            // Check if right side is our variable
            if (eqOp2.RightExpression is GDIdentifierExpression rightIdent2 &&
                rightIdent2.Identifier?.Sequence == varName)
            {
                if (IsNonNullExpression(eqOp2.LeftExpression))
                    return true;
            }
        }

        // var (truthiness check)
        if (expr is GDIdentifierExpression ident && ident.Identifier?.Sequence == varName)
            return true;

        // Recursively check nested 'and' expressions
        // e.g., a and is_instance_valid(x) and x.visible
        if (expr is GDDualOperatorExpression andOp)
        {
            var opType = andOp.Operator?.OperatorType;
            if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
            {
                if (IsNullGuardFor(andOp.LeftExpression, varName) ||
                    IsNullGuardFor(andOp.RightExpression, varName))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an expression is a null literal.
    /// </summary>
    public static bool IsNullLiteral(GDExpression? expr)
    {
        return expr is GDIdentifierExpression ident && ident.Identifier?.Sequence == "null";
    }

    /// <summary>
    /// Checks if an expression is guaranteed to be non-null.
    /// Returns true for literals (strings, numbers, arrays, dicts, bools) which are never null.
    /// </summary>
    public static bool IsNonNullExpression(GDExpression? expr)
    {
        if (expr == null)
            return false;

        // String, number, boolean, array, dictionary literals are never null
        if (expr is GDStringExpression ||
            expr is GDNumberExpression ||
            expr is GDBoolExpression ||
            expr is GDArrayInitializerExpression ||
            expr is GDDictionaryInitializerExpression)
            return true;

        // null literal is obviously null
        if (IsNullLiteral(expr))
            return false;

        // Other identifiers - assume they might be null (conservative)
        return false;
    }

    /// <summary>
    /// Checks if the child node is a descendant of the parent node.
    /// </summary>
    public static bool IsDescendantOf(GDNode? child, GDNode? parent)
    {
        if (child == null || parent == null)
            return false;

        var current = child;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Checks if the if statement is a guard clause that ensures the variable is valid.
    /// A guard clause has:
    /// - A condition that checks for null/invalid (not is_instance_valid(x), x == null)
    /// - A body that exits early (return, break, continue)
    /// </summary>
    private static bool IsGuardClauseForVariable(GDIfStatement ifStmt, string varName)
    {
        var ifBranch = ifStmt.IfBranch;
        if (ifBranch?.Condition == null)
            return false;

        // Check if condition is a negative null check
        if (!IsNegativeNullCheckFor(ifBranch.Condition, varName))
            return false;

        // Check if body is an early exit
        var statements = ifBranch.Statements?.ToList();
        if (statements == null || statements.Count == 0)
            return false;

        // The body should be just an early exit (return, break, continue)
        if (statements.Count == 1 && IsEarlyExit(statements[0]))
            return true;

        // Or the last statement should be an early exit
        if (IsEarlyExit(statements[statements.Count - 1]))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the condition is a negative null check for the variable.
    /// Patterns: not is_instance_valid(x), !is_instance_valid(x), x == null,
    ///           x == null or not is_instance_valid(x)
    /// </summary>
    private static bool IsNegativeNullCheckFor(GDExpression condition, string varName)
    {
        // Handle 'or' combination: x == null or not is_instance_valid(x)
        // If either side is a negative null check for the variable, the whole expression is
        if (condition is GDDualOperatorExpression orExpr)
        {
            var opType = orExpr.Operator?.OperatorType;
            if (opType == GDDualOperatorType.Or || opType == GDDualOperatorType.Or2)
            {
                // Either side can be the null check
                if (IsNegativeNullCheckFor(orExpr.LeftExpression, varName) ||
                    IsNegativeNullCheckFor(orExpr.RightExpression, varName))
                    return true;
            }
        }

        // not is_instance_valid(x) or !is_instance_valid(x)
        if (condition is GDSingleOperatorExpression singleOp)
        {
            var opType = singleOp.Operator?.OperatorType;
            if (opType == GDSingleOperatorType.Not || opType == GDSingleOperatorType.Not2)
            {
                // The inner expression should be a positive null guard
                if (IsPositiveNullGuardFor(singleOp.TargetExpression, varName))
                    return true;
            }
        }

        // x == null
        if (condition is GDDualOperatorExpression eqOp &&
            eqOp.Operator?.OperatorType == GDDualOperatorType.Equal)
        {
            if (IsNullLiteral(eqOp.RightExpression) &&
                eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
                leftIdent.Identifier?.Sequence == varName)
                return true;

            if (IsNullLiteral(eqOp.LeftExpression) &&
                eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                rightIdent.Identifier?.Sequence == varName)
                return true;
        }

        // not x (falsy check)
        if (condition is GDSingleOperatorExpression notOp)
        {
            var opType = notOp.Operator?.OperatorType;
            if (opType == GDSingleOperatorType.Not || opType == GDSingleOperatorType.Not2)
            {
                if (notOp.TargetExpression is GDIdentifierExpression ident &&
                    ident.Identifier?.Sequence == varName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the expression is a positive null guard (is_instance_valid(x), x != null, x).
    /// </summary>
    private static bool IsPositiveNullGuardFor(GDExpression? expr, string varName)
    {
        if (expr == null)
            return false;

        // is_instance_valid(x)
        if (expr is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDIdentifierExpression funcIdent &&
                funcIdent.Identifier?.Sequence == "is_instance_valid")
            {
                var args = callExpr.Parameters?.ToList();
                if (args != null && args.Count > 0 && args[0] is GDIdentifierExpression argIdent)
                {
                    if (argIdent.Identifier?.Sequence == varName)
                        return true;
                }
            }
        }

        // x != null
        if (expr is GDDualOperatorExpression eqOp &&
            eqOp.Operator?.OperatorType == GDDualOperatorType.NotEqual)
        {
            if (IsNullLiteral(eqOp.RightExpression) &&
                eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
                leftIdent.Identifier?.Sequence == varName)
                return true;

            if (IsNullLiteral(eqOp.LeftExpression) &&
                eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                rightIdent.Identifier?.Sequence == varName)
                return true;
        }

        // x (truthiness check)
        if (expr is GDIdentifierExpression ident && ident.Identifier?.Sequence == varName)
            return true;

        // x is Type (type guard implies non-null)
        if (expr is GDDualOperatorExpression isOp &&
            isOp.Operator?.OperatorType == GDDualOperatorType.Is)
        {
            if (isOp.LeftExpression is GDIdentifierExpression leftIsIdent &&
                leftIsIdent.Identifier?.Sequence == varName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the statement is an early exit (return, break, continue).
    /// In GDShrapt, these are wrapped in GDExpressionStatement.
    /// </summary>
    private static bool IsEarlyExit(GDStatement? stmt)
    {
        if (stmt is GDExpressionStatement exprStmt)
        {
            return exprStmt.Expression is GDReturnExpression
                   || exprStmt.Expression is GDBreakExpression
                   || exprStmt.Expression is GDContinueExpression;
        }
        return false;
    }

    /// <summary>
    /// Finds the containing method for a node.
    /// </summary>
    public static GDMethodDeclaration? FindContainingMethod(GDNode? node)
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
}
