namespace GDShrapt.Abstractions;

/// <summary>
/// Pro features that require licensing.
/// Mirrors GDProFeature enum from Pro.Core for weak coupling.
/// Values must match GDShrapt.Pro.Core.GDProFeature.
/// </summary>
public enum GDProFeatureKind
{
    // Refactoring - Confidence Modes
    /// <summary>
    /// Apply Potential (duck-typed) edits during rename.
    /// </summary>
    ConfidenceAwarePotential,

    /// <summary>
    /// Apply NameMatch (heuristic) edits during rename.
    /// </summary>
    ConfidenceAwareNameMatch,

    // Refactoring - Batch Operations
    /// <summary>
    /// Batch refactoring across multiple files.
    /// </summary>
    BatchRefactoring,

    /// <summary>
    /// Apply type annotations (single file).
    /// </summary>
    ApplyTypeAnnotations,

    /// <summary>
    /// Batch add type annotations across project.
    /// </summary>
    BatchTypeAnnotations,

    /// <summary>
    /// Generate @onready from get_node calls.
    /// </summary>
    GenerateOnready,

    /// <summary>
    /// Apply member reordering (single file).
    /// </summary>
    ApplyReorderMembers,

    /// <summary>
    /// Batch reorder members across project.
    /// </summary>
    BatchReorderMembers,

    // Analysis
    /// <summary>
    /// Incremental analysis (changed files only).
    /// </summary>
    IncrementalAnalysis,

    /// <summary>
    /// Persistent analysis cache.
    /// </summary>
    PersistentCache,

    // Reporting
    /// <summary>
    /// SARIF export for code scanning.
    /// </summary>
    SarifExport,

    /// <summary>
    /// Baseline comparison (new vs fixed issues).
    /// </summary>
    BaselineComparison,

    /// <summary>
    /// CI annotations (GitHub/GitLab/etc).
    /// </summary>
    CIAnnotations,

    /// <summary>
    /// Policy enforcement (quality gates).
    /// </summary>
    PolicyEnforcement,

    /// <summary>
    /// HTML report generation with interactive features.
    /// </summary>
    HtmlReports,

    /// <summary>
    /// Technical debt scoring and analysis.
    /// </summary>
    DebtScoring
}
