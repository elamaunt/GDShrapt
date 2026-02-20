using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a single piece of detected dead code.
/// </summary>
public class GDDeadCodeItem
{
    /// <summary>
    /// Type of dead code.
    /// </summary>
    public GDDeadCodeKind Kind { get; set; }

    /// <summary>
    /// Name of the dead code element (variable name, function name, etc.).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Full path to the file containing the dead code.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// End line number (1-based). For multi-line elements like functions.
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// End column number (1-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Confidence level of the detection.
    /// </summary>
    public GDReferenceConfidence Confidence { get; set; } = GDReferenceConfidence.Strict;

    /// <summary>
    /// Compact reason code for the detection.
    /// </summary>
    public GDDeadCodeReasonCode ReasonCode { get; set; }

    /// <summary>
    /// Human-readable reason why this is considered dead code.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Whether this is a private member (starts with _ in GDScript convention).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Whether this variable has @export or @onready annotation.
    /// </summary>
    public bool IsExportedOrOnready { get; set; }

    /// <summary>
    /// Evidence details for --explain mode. Null unless CollectEvidence is enabled.
    /// </summary>
    public GDDeadCodeEvidence? Evidence { get; set; }

    public GDDeadCodeItem()
    {
    }

    public GDDeadCodeItem(GDDeadCodeKind kind, string name, string filePath)
    {
        Kind = kind;
        Name = name;
        FilePath = filePath;
    }

    /// <summary>
    /// Creates a unique identifier for baseline comparison.
    /// </summary>
    public string GetBaselineKey() =>
        $"{Kind}:{FilePath}:{Name}:{Line}";

    /// <summary>
    /// Checks if this item matches another for baseline comparison.
    /// Uses fuzzy matching (same file, name, and kind - line may differ slightly).
    /// </summary>
    public bool Matches(GDDeadCodeItem other) =>
        Kind == other.Kind &&
        Name == other.Name &&
        string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
}
