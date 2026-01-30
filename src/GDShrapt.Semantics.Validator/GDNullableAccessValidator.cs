using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates access on potentially-null variables.
/// Reports warnings for:
/// - Property access on potentially null variables (x.property where x may be null)
/// - Method calls on potentially null variables (x.method() where x may be null)
/// - Indexer access on potentially null variables (x[i] where x may be null)
/// </summary>
public class GDNullableAccessValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDDiagnosticSeverity _severity;
    private readonly GDNullableStrictnessMode _strictness;
    private readonly bool _warnOnDictionaryIndexer;
    private readonly bool _warnOnUntypedParameters;

    public GDNullableAccessValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Warning)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = severity;
        _strictness = GDNullableStrictnessMode.Strict;
        _warnOnDictionaryIndexer = true;
        _warnOnUntypedParameters = true;
    }

    public GDNullableAccessValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = options.NullableAccessSeverity;
        _strictness = options.NullableStrictness;
        _warnOnDictionaryIndexer = options.WarnOnDictionaryIndexer;
        _warnOnUntypedParameters = options.WarnOnUntypedParameters;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDMemberOperatorExpression memberAccess)
    {
        ValidateNullAccess(memberAccess, memberAccess.CallerExpression, GDDiagnosticCode.PotentiallyNullAccess);
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Only validate member calls (obj.method())
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            ValidateNullAccess(callExpr, memberExpr.CallerExpression, GDDiagnosticCode.PotentiallyNullMethodCall);
        }
    }

    public override void Visit(GDIndexerExpression indexerExpr)
    {
        ValidateNullAccess(indexerExpr, indexerExpr.CallerExpression, GDDiagnosticCode.PotentiallyNullIndexer);
    }

    private void ValidateNullAccess(GDNode accessNode, GDExpression? callerExpr, GDDiagnosticCode code)
    {
        // Check strictness mode - if Off, skip all checks
        if (_strictness == GDNullableStrictnessMode.Off)
            return;

        if (callerExpr == null)
            return;

        // Get the root variable name from the caller expression
        var varName = GetRootVariableName(callerExpr);
        if (string.IsNullOrEmpty(varName))
            return;

        // Skip 'self' - it's never null
        if (varName == "self")
            return;

        // Check if we're in the right side of an 'and' expression with a null guard on the left
        // e.g., is_instance_valid(x) and x.visible - x is guaranteed non-null in the right side
        if (IsGuardedByAndCondition(accessNode, varName))
            return;

        // Check if the variable is protected by a preceding guard clause
        // e.g., if not is_instance_valid(x): return
        //       x.property  # <-- x is safe here
        if (IsProtectedByGuardClause(accessNode, varName))
            return;

        // Skip untyped parameters based on options
        if (!_warnOnUntypedParameters && IsUntypedParameter(varName, accessNode))
            return;

        // Skip dictionary indexer results based on options
        if (!_warnOnDictionaryIndexer && IsFromDictionaryIndexer(callerExpr))
            return;

        // Relaxed mode: only warn on explicitly nullable variables (var x = null)
        if (_strictness == GDNullableStrictnessMode.Relaxed)
        {
            if (!IsExplicitlyNullable(varName, accessNode))
                return;
        }

        // Check if the variable is potentially null at this location
        if (_semanticModel.IsVariablePotentiallyNull(varName, accessNode))
        {
            var memberName = GetAccessedMemberName(accessNode);
            var message = string.IsNullOrEmpty(memberName)
                ? $"Variable '{varName}' may be null"
                : $"Variable '{varName}' may be null when accessing '{memberName}'";

            // In Error mode, always report as error regardless of configured severity
            var effectiveSeverity = _strictness == GDNullableStrictnessMode.Error
                ? GDDiagnosticSeverity.Error
                : _severity;

            ReportDiagnosticWithSeverity(code, message, accessNode, effectiveSeverity);
        }
    }

    /// <summary>
    /// Checks if the variable is an untyped function parameter.
    /// </summary>
    private bool IsUntypedParameter(string varName, GDNode atLocation)
    {
        var method = FindContainingMethod(atLocation);
        if (method?.Parameters == null)
            return false;

        foreach (var param in method.Parameters)
        {
            if (param.Identifier?.Sequence == varName)
                return param.Type == null;
        }
        return false;
    }

    /// <summary>
    /// Checks if the expression comes from a dictionary indexer access.
    /// </summary>
    private static bool IsFromDictionaryIndexer(GDExpression? expr)
    {
        return expr is GDIndexerExpression;
    }

    /// <summary>
    /// Checks if the variable is explicitly initialized to null (var x = null).
    /// Used for Relaxed mode.
    /// </summary>
    private bool IsExplicitlyNullable(string varName, GDNode atLocation)
    {
        // Check if the variable was assigned null
        return _semanticModel.IsVariablePotentiallyNull(varName, atLocation);
    }

    private void ReportDiagnosticWithSeverity(GDDiagnosticCode code, string message, GDNode node, GDDiagnosticSeverity severity)
    {
        switch (severity)
        {
            case GDDiagnosticSeverity.Error:
                ReportError(code, message, node);
                break;
            case GDDiagnosticSeverity.Warning:
                ReportWarning(code, message, node);
                break;
            case GDDiagnosticSeverity.Hint:
                ReportHint(code, message, node);
                break;
        }
    }

    /// <summary>
    /// Checks if the access node is guarded by a null check.
    /// Handles patterns like:
    /// - In 'and' expression: is_instance_valid(x) and x.visible, x != null and x.method(), x and x.property
    /// - In if-body: if x: x.method(), if x and ...: x.method()
    /// </summary>
    private static bool IsGuardedByAndCondition(GDNode accessNode, string varName)
    {
        // Check if we're in the right side of an 'and' expression with a null guard on the left
        if (IsGuardedByAndExpressionLeft(accessNode, varName))
            return true;

        // Check if we're in an if/elif body where the condition contains a truthiness guard
        if (IsInIfBodyWithTruthinessGuard(accessNode, varName))
            return true;

        return false;
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
    /// Checks if access is in if-body where the condition contains a truthiness guard.
    /// Handles: if obj: obj.method()
    ///          if obj and ...: obj.method()
    ///          if is_instance_valid(obj): obj.method()
    /// </summary>
    private static bool IsInIfBodyWithTruthinessGuard(GDNode accessNode, string varName)
    {
        // Find containing GDIfBranch or GDElifBranch
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
        }

        return false;
    }

    /// <summary>
    /// Checks if the expression is a null guard for the specified variable.
    /// Recognizes: is_instance_valid(var), var != null, var (truthiness check)
    /// </summary>
    private static bool IsNullGuardFor(GDExpression? expr, string varName)
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
    private static bool IsNullLiteral(GDExpression? expr)
    {
        return expr is GDIdentifierExpression ident && ident.Identifier?.Sequence == "null";
    }

    /// <summary>
    /// Checks if an expression is guaranteed to be non-null.
    /// Returns true for literals (strings, numbers, arrays, dicts, bools) which are never null.
    /// </summary>
    private static bool IsNonNullExpression(GDExpression? expr)
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
    private static bool IsDescendantOf(GDNode? child, GDNode? parent)
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
    /// Checks if the access is protected by a preceding guard clause that handles the null case.
    /// Pattern: if not is_instance_valid(x): return   (or x == null: return)
    ///          x.property  # <-- x is guaranteed non-null here
    /// </summary>
    private static bool IsProtectedByGuardClause(GDNode accessNode, string varName)
    {
        // Find the containing statements list
        var containingMethod = FindContainingMethod(accessNode);
        if (containingMethod == null)
            return false;

        // Get all statements in the method
        var statements = containingMethod.Statements?.ToList();
        if (statements == null || statements.Count == 0)
            return false;

        // Find the statement containing our access
        var accessStatementIndex = -1;
        for (int i = 0; i < statements.Count; i++)
        {
            if (IsDescendantOf(accessNode, statements[i]))
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
            if (statements[i] is GDIfStatement ifStmt)
            {
                if (IsGuardClauseForVariable(ifStmt, varName))
                    return true;
            }
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
    /// Patterns: not is_instance_valid(x), !is_instance_valid(x), x == null
    /// </summary>
    private static bool IsNegativeNullCheckFor(GDExpression condition, string varName)
    {
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
    private static GDMethodDeclaration? FindContainingMethod(GDNode? node)
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

    private static string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;

        return null;
    }

    private static string? GetAccessedMemberName(GDNode node)
    {
        return node switch
        {
            GDMemberOperatorExpression memberExpr => memberExpr.Identifier?.Sequence,
            GDCallExpression callExpr when callExpr.CallerExpression is GDMemberOperatorExpression memberExpr
                => memberExpr.Identifier?.Sequence,
            GDIndexerExpression => "[...]",
            _ => null
        };
    }
}
