using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Information about a .call() or .callv() invocation on a Callable variable.
/// </summary>
internal class GDCallableCallSiteInfo
{
    /// <summary>
    /// The call expression AST node (e.g., cb.call(42)).
    /// </summary>
    public GDCallExpression CallExpression { get; }

    /// <summary>
    /// The expression representing the Callable being called (e.g., cb in cb.call()).
    /// </summary>
    public GDExpression CallableExpression { get; }

    /// <summary>
    /// The arguments passed to .call().
    /// </summary>
    public IReadOnlyList<GDCallableArgumentInfo> Arguments { get; }

    /// <summary>
    /// The source file containing this call site.
    /// </summary>
    public GDScriptFile? SourceFile { get; }

    /// <summary>
    /// Whether this is a .callv() call (arguments in array).
    /// </summary>
    public bool IsCallV { get; }

    /// <summary>
    /// The resolved Callable definition (if known).
    /// </summary>
    public GDCallableDefinition? ResolvedDefinition { get; set; }

    /// <summary>
    /// Line number of the call site.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the call site.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The variable name of the Callable (if it's a simple identifier).
    /// </summary>
    public string? CallableVariableName { get; }

    public GDCallableCallSiteInfo(
        GDCallExpression callExpression,
        GDExpression callableExpression,
        IReadOnlyList<GDCallableArgumentInfo> arguments,
        GDScriptFile? sourceFile,
        bool isCallV,
        int line,
        int column,
        string? callableVariableName)
    {
        CallExpression = callExpression;
        CallableExpression = callableExpression;
        Arguments = arguments;
        SourceFile = sourceFile;
        IsCallV = isCallV;
        Line = line;
        Column = column;
        CallableVariableName = callableVariableName;
    }

    /// <summary>
    /// Creates a call site info from a call expression.
    /// </summary>
    public static GDCallableCallSiteInfo? TryCreate(
        GDCallExpression callExpr,
        GDScriptFile? sourceFile,
        System.Func<GDExpression, GDSemanticType?>? typeInferrer = null)
    {
        // Check if this is a .call() or .callv() invocation
        if (callExpr.CallerExpression is not GDMemberOperatorExpression memberOp)
            return null;

        var methodName = memberOp.Identifier?.Sequence;
        if (methodName != "call" && methodName != "callv")
            return null;

        var callableExpr = memberOp.CallerExpression;
        if (callableExpr == null)
            return null;

        var isCallV = methodName == "callv";

        // Extract arguments
        var arguments = new List<GDCallableArgumentInfo>();
        if (callExpr.Parameters != null)
        {
            int index = 0;
            foreach (var param in callExpr.Parameters)
            {
                var expr = param as GDExpression;
                if (expr != null)
                {
                    var inferredType = typeInferrer?.Invoke(expr);
                    arguments.Add(new GDCallableArgumentInfo(index, expr, inferredType));
                }
                index++;
            }
        }

        // Get position
        var firstToken = callExpr.AllTokens.FirstOrDefault();
        var line = firstToken?.StartLine ?? 0;
        var column = firstToken?.StartColumn ?? 0;

        // Try to get variable name
        string? variableName = null;
        if (callableExpr is GDIdentifierExpression identExpr)
        {
            variableName = identExpr.Identifier?.Sequence;
        }
        else if (callableExpr is GDMemberOperatorExpression memberAccess)
        {
            variableName = memberAccess.Identifier?.Sequence;
        }

        return new GDCallableCallSiteInfo(
            callExpr,
            callableExpr,
            arguments,
            sourceFile,
            isCallV,
            line,
            column,
            variableName);
    }

    public override string ToString()
    {
        var argsStr = string.Join(", ", Arguments.Select(a => a.InferredType?.DisplayName ?? "?"));
        var method = IsCallV ? "callv" : "call";
        return $"{CallableVariableName ?? "?"}.{method}({argsStr}) at {Line}:{Column}";
    }
}

/// <summary>
/// Information about an argument passed to .call().
/// </summary>
internal class GDCallableArgumentInfo
{
    /// <summary>
    /// Index of the argument (0-based).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The argument expression.
    /// </summary>
    public GDExpression Expression { get; }

    /// <summary>
    /// Inferred type of the argument.
    /// </summary>
    public GDSemanticType? InferredType { get; }

    public GDCallableArgumentInfo(int index, GDExpression expression, GDSemanticType? inferredType)
    {
        Index = index;
        Expression = expression;
        InferredType = inferredType;
    }

    public override string ToString()
    {
        return $"[{Index}]: {InferredType?.DisplayName ?? "?"}";
    }
}
