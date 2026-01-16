using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Contains information about an argument at a call site.
/// Used for parameter type inference from usage.
/// </summary>
public class GDArgumentInfo
{
    /// <summary>
    /// The index of this argument (0-based).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The argument expression node.
    /// </summary>
    public GDExpression? Expression { get; }

    /// <summary>
    /// The inferred type of the argument.
    /// </summary>
    public string? InferredType { get; }

    /// <summary>
    /// Whether the type inference is high confidence.
    /// </summary>
    public bool IsHighConfidence { get; }

    /// <summary>
    /// Source code representation of the argument expression.
    /// </summary>
    public string ExpressionText { get; }

    /// <summary>
    /// Line number of the argument.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the argument.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Creates a new argument info.
    /// </summary>
    public GDArgumentInfo(
        int index,
        GDExpression? expression,
        string? inferredType,
        bool isHighConfidence)
    {
        Index = index;
        Expression = expression;
        InferredType = inferredType;
        IsHighConfidence = isHighConfidence;
        ExpressionText = expression?.ToString() ?? "";

        var token = expression?.AllTokens.FirstOrDefault();
        Line = token?.StartLine ?? 0;
        Column = token?.StartColumn ?? 0;
    }

    /// <summary>
    /// Creates an argument info with explicit location.
    /// </summary>
    public GDArgumentInfo(
        int index,
        GDExpression? expression,
        string? inferredType,
        bool isHighConfidence,
        int line,
        int column)
    {
        Index = index;
        Expression = expression;
        InferredType = inferredType;
        IsHighConfidence = isHighConfidence;
        ExpressionText = expression?.ToString() ?? "";
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        var confidence = IsHighConfidence ? "high" : "low";
        return $"arg[{Index}]: {InferredType ?? "?"} ({confidence}) @ {Line}:{Column}";
    }

    /// <summary>
    /// Creates an unknown argument info (when expression is not an expression type).
    /// </summary>
    public static GDArgumentInfo Unknown(int index)
    {
        return new GDArgumentInfo(index, null, null, false, 0, 0);
    }
}
