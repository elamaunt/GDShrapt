using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a text edit operation at a specific location in a file.
/// </summary>
public sealed class GDTextEdit
{
    /// <summary>
    /// The file path where the edit should be applied.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The line number (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The end line number (1-based) for multi-line edits.
    /// If not set, the edit affects only the Line.
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// The text to be replaced.
    /// </summary>
    public string OldText { get; }

    /// <summary>
    /// The replacement text.
    /// </summary>
    public string NewText { get; }

    /// <summary>
    /// The confidence level of this edit (strict vs potential reference).
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Reason for the confidence determination (for debugging/UI).
    /// </summary>
    public string? ConfidenceReason { get; }

    /// <summary>
    /// Detailed per-type provenance for duck-typed edits.
    /// Used by --explain mode to show where each possible type comes from.
    /// </summary>
    public IReadOnlyList<GDTypeProvenanceEntry>? DetailedProvenance { get; init; }

    /// <summary>
    /// The variable name whose provenance is traced (for dedup in --explain output).
    /// </summary>
    public string? ProvenanceVariableName { get; init; }

    /// <summary>
    /// Short label for promoted edits (e.g. "Promoted: all evidence types covered").
    /// </summary>
    public string? PromotionLabel { get; init; }

    /// <summary>
    /// Structured proof parts — each is one piece of evidence.
    /// </summary>
    public IReadOnlyList<string>? PromotionProofParts { get; init; }

    /// <summary>
    /// Types confirmed covered during promotion.
    /// </summary>
    public IReadOnlyList<string>? PromotionCoveredTypes { get; init; }

    /// <summary>
    /// Filter applied during promotion (e.g. "filtered by scene collision_layer").
    /// </summary>
    public string? PromotionFilter { get; init; }

    /// <summary>
    /// Detailed collision layer proof lines for verbose output.
    /// </summary>
    public IReadOnlyList<string>? CollisionLayerProof { get; init; }

    /// <summary>
    /// Detailed avoidance layer proof lines for verbose output.
    /// </summary>
    public IReadOnlyList<string>? AvoidanceLayerProof { get; init; }

    /// <summary>
    /// Whether this edit targets a contract string (e.g. has_method("name"), emit_signal("name")).
    /// </summary>
    public bool IsContractString { get; init; }

    public GDTextEdit(string filePath, int line, int column, string oldText, string newText,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict,
        string? confidenceReason = null)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        OldText = oldText;
        NewText = newText;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
    }

    public override string ToString() => $"{FilePath}:{Line}:{Column}: {OldText} -> {NewText} [{Confidence}]";
}

/// <summary>
/// Per-type provenance: how this type was inferred + all call sites that pass it.
/// </summary>
public sealed class GDTypeProvenanceEntry
{
    public string TypeName { get; }
    public string SourceReason { get; }
    public int? SourceLine { get; }
    public IReadOnlyList<GDCallSiteProvenanceEntry> CallSites { get; }
    public string? SourceFilePath { get; }

    public GDTypeProvenanceEntry(string typeName, string sourceReason,
        int? sourceLine = null, IReadOnlyList<GDCallSiteProvenanceEntry>? callSites = null,
        string? sourceFilePath = null)
    {
        TypeName = typeName;
        SourceReason = sourceReason;
        SourceLine = sourceLine;
        CallSites = callSites ?? (IReadOnlyList<GDCallSiteProvenanceEntry>)System.Array.Empty<GDCallSiteProvenanceEntry>();
        SourceFilePath = sourceFilePath;
    }
}

/// <summary>
/// One call site where a specific type is passed as argument.
/// Supports recursive chaining via InnerChain for tracing data flow.
/// </summary>
public sealed class GDCallSiteProvenanceEntry
{
    public string FilePath { get; }
    public int Line { get; }
    public string Expression { get; }
    public IReadOnlyList<GDCallSiteProvenanceEntry> InnerChain { get; }

    /// <summary>
    /// Whether the type at this step came from explicit type annotation (true)
    /// or was inferred from usages (false). Null = not a type declaration step.
    /// </summary>
    public bool? IsExplicitType { get; init; }

    public GDCallSiteProvenanceEntry(string filePath, int line, string expression,
        IReadOnlyList<GDCallSiteProvenanceEntry>? innerChain = null)
    {
        FilePath = filePath;
        Line = line;
        Expression = expression;
        InnerChain = innerChain ?? (IReadOnlyList<GDCallSiteProvenanceEntry>)System.Array.Empty<GDCallSiteProvenanceEntry>();
    }
}
