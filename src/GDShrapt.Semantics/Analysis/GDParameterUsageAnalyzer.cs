using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes how parameters are used within a method body.
/// Builds constraints that can be resolved to concrete types via duck typing.
/// </summary>
internal class GDParameterUsageAnalyzer : GDVisitor
{
    private readonly Dictionary<string, GDParameterConstraints> _paramConstraints = new();
    private readonly HashSet<string> _parameterNames;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    /// <summary>
    /// The collected constraints for each parameter.
    /// </summary>
    public IReadOnlyDictionary<string, GDParameterConstraints> Constraints => _paramConstraints;

    /// <summary>
    /// Creates a new parameter usage analyzer.
    /// </summary>
    /// <param name="parameterNames">Names of parameters to analyze.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    public GDParameterUsageAnalyzer(
        IEnumerable<string> parameterNames,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        _parameterNames = new HashSet<string>(parameterNames);
        _runtimeProvider = runtimeProvider;

        // Initialize constraints for each parameter
        foreach (var name in _parameterNames)
            _paramConstraints[name] = new GDParameterConstraints(name);
    }

    #region Member Access

    /// <summary>
    /// Tracks member access on parameters (property or method calls).
    /// </summary>
    public override void Visit(GDMemberOperatorExpression memberOp)
    {
        base.Visit(memberOp);

        var rootVar = GetRootVariable(memberOp.CallerExpression);
        if (rootVar != null && _parameterNames.Contains(rootVar))
        {
            var memberName = memberOp.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(memberName))
            {
                // Check if this is a method call or property access
                if (memberOp.Parent is GDCallExpression)
                    _paramConstraints[rootVar].AddRequiredMethod(memberName);
                else
                    _paramConstraints[rootVar].AddRequiredProperty(memberName);
            }
        }
    }

    #endregion

    #region Iteration and Indexing

    /// <summary>
    /// Tracks for loop usage - parameter must be iterable.
    /// </summary>
    public override void Visit(GDForStatement forStmt)
    {
        base.Visit(forStmt);

        // for x in param → param is iterable
        var collectionVar = GetRootVariable(forStmt.Collection);
        if (collectionVar != null && _parameterNames.Contains(collectionVar))
        {
            _paramConstraints[collectionVar].AddIterableConstraint();
        }
    }

    /// <summary>
    /// Tracks indexer access - parameter must be indexable.
    /// </summary>
    public override void Visit(GDIndexerExpression indexer)
    {
        base.Visit(indexer);

        var rootVar = GetRootVariable(indexer.CallerExpression);
        if (rootVar != null && _parameterNames.Contains(rootVar))
        {
            _paramConstraints[rootVar].AddIndexableConstraint();
        }
    }

    #endregion

    #region Call Sites

    /// <summary>
    /// Tracks when parameters are passed to other methods.
    /// </summary>
    public override void Visit(GDCallExpression call)
    {
        base.Visit(call);

        // Track when parameter is passed to another method
        var args = call.Parameters?.ToList() ?? new List<GDExpression>();
        for (int i = 0; i < args.Count; i++)
        {
            var argVar = GetRootVariable(args[i]);
            if (argVar != null && _parameterNames.Contains(argVar))
            {
                _paramConstraints[argVar].AddPassedToCall(call, i);
            }
        }
    }

    #endregion

    #region Type Checks

    /// <summary>
    /// Tracks 'is' type checks to narrow possible types.
    /// </summary>
    public override void Visit(GDDualOperatorExpression dualOp)
    {
        base.Visit(dualOp);

        var op = dualOp.Operator?.OperatorType;
        if (op != GDDualOperatorType.Is)
            return;

        // Check if left side is a parameter
        var paramName = GetRootVariable(dualOp.LeftExpression);
        if (paramName == null || !_parameterNames.Contains(paramName))
            return;

        // Get the type being checked
        var typeName = GetTypeFromExpression(dualOp.RightExpression);
        if (string.IsNullOrEmpty(typeName))
            return;

        // Check if this is a negative check (not param is Type)
        var isNegative = IsNegativeCheck(dualOp);

        if (isNegative)
            _paramConstraints[paramName].ExcludeType(typeName);
        else
            _paramConstraints[paramName].AddPossibleType(typeName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the root variable name from an expression chain.
    /// E.g., player.weapon.damage → "player"
    /// </summary>
    private string? GetRootVariable(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
    }

    /// <summary>
    /// Gets the type name from a type expression (used in 'is' checks).
    /// </summary>
    private string? GetTypeFromExpression(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        // Handle dotted type names like Node2D.SomeClass
        if (expr is GDMemberOperatorExpression memberOp)
        {
            var parts = new List<string>();
            GDExpression? current = expr;
            while (current is GDMemberOperatorExpression mo)
            {
                var memberName = mo.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                    parts.Insert(0, memberName);
                current = mo.CallerExpression;
            }
            if (current is GDIdentifierExpression rootIdent)
            {
                var rootName = rootIdent.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(rootName))
                    parts.Insert(0, rootName);
            }
            return parts.Count > 0 ? string.Join(".", parts) : null;
        }

        return null;
    }

    /// <summary>
    /// Checks if an 'is' expression is inside a negation.
    /// </summary>
    private bool IsNegativeCheck(GDDualOperatorExpression dualOp)
    {
        // Check if parent is a 'not' operator
        var parent = dualOp.Parent;
        if (parent is GDSingleOperatorExpression singleOp)
        {
            var opType = singleOp.OperatorType;
            return opType == GDSingleOperatorType.Not || opType == GDSingleOperatorType.Not2;
        }

        // Check if used in "if not x is Type" pattern
        // This is harder to detect, skip for now
        return false;
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Analyzes a method and returns constraints for all its parameters.
    /// </summary>
    public static IReadOnlyDictionary<string, GDParameterConstraints> AnalyzeMethod(
        GDMethodDeclaration method,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        var paramNames = method.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return new Dictionary<string, GDParameterConstraints>();

        var analyzer = new GDParameterUsageAnalyzer(paramNames, runtimeProvider);
        method.Statements?.WalkIn(analyzer);
        return analyzer.Constraints;
    }

    /// <summary>
    /// Analyzes a lambda and returns constraints for all its parameters.
    /// </summary>
    public static IReadOnlyDictionary<string, GDParameterConstraints> AnalyzeLambda(
        GDMethodExpression lambda,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        var paramNames = lambda.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return new Dictionary<string, GDParameterConstraints>();

        var analyzer = new GDParameterUsageAnalyzer(paramNames, runtimeProvider);
        lambda.Expression?.WalkIn(analyzer);
        lambda.Statements?.WalkIn(analyzer);
        return analyzer.Constraints;
    }

    #endregion
}
