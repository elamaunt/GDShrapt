namespace GDShrapt.Semantics;

/// <summary>
/// Evidence from a single call site showing where a parameter type was inferred from.
/// </summary>
public class GDCallSiteEvidence
{
    public string FilePath { get; init; } = "";
    public int Line { get; init; }
    public int Column { get; init; }
    public string ArgumentExpression { get; init; } = "";
    public string? InferredType { get; init; }
    public bool IsHighConfidence { get; init; }
    public bool IsDuckTyped { get; init; }
    public GDTypeProvenance Provenance { get; init; }
    public string? SourceVariableName { get; init; }

    /// <summary>
    /// Creates evidence from a call-site argument report.
    /// </summary>
    internal static GDCallSiteEvidence FromReport(GDCallSiteArgumentReport report)
    {
        return new GDCallSiteEvidence
        {
            FilePath = report.SourceFilePath ?? "",
            Line = report.Line,
            Column = report.Column,
            ArgumentExpression = report.ArgumentExpression ?? "",
            InferredType = report.InferredType,
            IsHighConfidence = report.IsHighConfidence,
            IsDuckTyped = report.IsDuckTyped,
            Provenance = report.Provenance,
            SourceVariableName = report.SourceVariableName
        };
    }
}
