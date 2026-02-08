namespace GDShrapt.Semantics;

/// <summary>
/// Report about a single return statement in a method.
/// </summary>
internal class GDReturnStatementReport
{
    /// <summary>
    /// Line number of the return statement.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number of the return statement.
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Source code of the return expression (null for implicit return or return without value).
    /// </summary>
    public string? ReturnExpression { get; init; }

    /// <summary>
    /// The inferred type of the return expression.
    /// </summary>
    public string? InferredType { get; init; }

    /// <summary>
    /// Whether the type inference is high confidence.
    /// </summary>
    public bool IsHighConfidence { get; init; }

    /// <summary>
    /// Whether this is an implicit return (method ends without explicit return).
    /// </summary>
    public bool IsImplicit { get; init; }

    /// <summary>
    /// Branch context (e.g., "if", "else", "match case", "early return").
    /// </summary>
    public string? BranchContext { get; init; }

    /// <summary>
    /// Creates a report from a GDReturnInfo.
    /// </summary>
    public static GDReturnStatementReport FromReturnInfo(GDReturnInfo returnInfo)
    {
        return new GDReturnStatementReport
        {
            Line = returnInfo.Line,
            Column = returnInfo.Column,
            ReturnExpression = returnInfo.ExpressionText,
            InferredType = returnInfo.InferredType?.DisplayName,
            IsHighConfidence = returnInfo.IsHighConfidence,
            IsImplicit = returnInfo.IsImplicit,
            BranchContext = returnInfo.BranchContext
        };
    }

    public override string ToString()
    {
        if (IsImplicit)
            return $"[implicit] -> null @ line {Line}";

        var typeStr = InferredType ?? "null";
        var confidence = IsHighConfidence ? "high" : "low";
        var context = !string.IsNullOrEmpty(BranchContext) ? $" ({BranchContext})" : "";
        var expr = !string.IsNullOrEmpty(ReturnExpression) ? $"return {ReturnExpression}" : "return";
        return $"{expr} -> {typeStr} ({confidence}){context} @ {Line}:{Column}";
    }
}
