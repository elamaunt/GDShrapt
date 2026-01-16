namespace GDShrapt.Semantics;

/// <summary>
/// Detailed report about an argument at a specific call site.
/// Used for visualization of parameter type inference.
/// </summary>
public class GDCallSiteArgumentReport
{
    /// <summary>
    /// Full file path of the source script.
    /// </summary>
    public string SourceFilePath { get; init; } = "";

    /// <summary>
    /// Resource path (res://...) of the source script.
    /// </summary>
    public string? ResPath { get; init; }

    /// <summary>
    /// Line number of the argument.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number of the argument.
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Source code representation of the argument expression.
    /// </summary>
    public string ArgumentExpression { get; init; } = "";

    /// <summary>
    /// The inferred type of the argument.
    /// </summary>
    public string? InferredType { get; init; }

    /// <summary>
    /// Whether the type inference is high confidence.
    /// </summary>
    public bool IsHighConfidence { get; init; }

    /// <summary>
    /// Whether this call site is from a duck-typed receiver.
    /// </summary>
    public bool IsDuckTyped { get; init; }

    /// <summary>
    /// If duck-typed, the name of the receiver variable.
    /// </summary>
    public string? ReceiverVariableName { get; init; }

    /// <summary>
    /// Whether this call site is from a Union receiver type.
    /// </summary>
    public bool FromUnionReceiver { get; init; }

    /// <summary>
    /// If from Union receiver, the Union type string.
    /// </summary>
    public string? UnionReceiverType { get; init; }

    /// <summary>
    /// The type of the receiver (if known).
    /// </summary>
    public string? ReceiverType { get; init; }

    /// <summary>
    /// Confidence level of the call site resolution.
    /// </summary>
    public GDReferenceConfidence Confidence { get; init; }

    /// <summary>
    /// Creates a report from a GDArgumentInfo and GDCallSiteInfo.
    /// </summary>
    public static GDCallSiteArgumentReport FromCallSite(GDCallSiteInfo callSite, GDArgumentInfo argument)
    {
        return new GDCallSiteArgumentReport
        {
            SourceFilePath = callSite.FilePath,
            ResPath = callSite.ResPath,
            Line = argument.Line > 0 ? argument.Line : callSite.Line,
            Column = argument.Column > 0 ? argument.Column : callSite.Column,
            ArgumentExpression = argument.ExpressionText,
            InferredType = argument.InferredType,
            IsHighConfidence = argument.IsHighConfidence,
            IsDuckTyped = callSite.IsDuckTyped,
            ReceiverVariableName = callSite.ReceiverVariableName,
            FromUnionReceiver = !string.IsNullOrEmpty(callSite.UnionReceiverType),
            UnionReceiverType = callSite.UnionReceiverType,
            ReceiverType = callSite.ReceiverType,
            Confidence = callSite.Confidence
        };
    }

    public override string ToString()
    {
        var location = $"{System.IO.Path.GetFileName(SourceFilePath)}:{Line}:{Column}";
        var type = InferredType ?? "?";
        var confidence = IsHighConfidence ? "high" : "low";

        if (IsDuckTyped)
            return $"[duck] {ArgumentExpression} -> {type} ({confidence}) @ {location}";

        if (FromUnionReceiver)
            return $"[union:{UnionReceiverType}] {ArgumentExpression} -> {type} ({confidence}) @ {location}";

        return $"{ArgumentExpression} -> {type} ({confidence}) @ {location}";
    }
}
