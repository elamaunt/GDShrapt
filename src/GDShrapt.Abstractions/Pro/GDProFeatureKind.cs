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
    DebtScoring,

    // Dead Code Analysis
    /// <summary>
    /// Remove dead code (--apply for dead-code command).
    /// </summary>
    DeadCodeRemoval,

    /// <summary>
    /// Export dependency graph in DOT or Mermaid format.
    /// </summary>
    DependencyVisualization,

    // CI Gates
    /// <summary>
    /// Fail CI if metrics exceed thresholds.
    /// </summary>
    MetricsGates,

    /// <summary>
    /// Fail CI if type coverage drops below threshold.
    /// </summary>
    CoverageGates,

    // Export & PR Integration
    /// <summary>
    /// Export dead code as patch/diff for PR review.
    /// </summary>
    PatchExport,

    /// <summary>
    /// Safe deletion with transaction and rollback on diagnostics regression.
    /// </summary>
    SafeDeletionPlan,

    // Additional Analysis
    /// <summary>
    /// Detect duplicated code blocks.
    /// </summary>
    DuplicateDetection,

    /// <summary>
    /// Security vulnerability scanning.
    /// </summary>
    SecurityScanning,

    /// <summary>
    /// Trend analysis over time (metrics history).
    /// </summary>
    TrendAnalysis,

    // Risky Refactoring
    /// <summary>
    /// Inline dead code functions at call sites before removal.
    /// Risky operation with restrictions: no yield, no recursion, limited call sites.
    /// </summary>
    SafeInline,

    // Duplicate Code Fixes
    /// <summary>
    /// Apply Extract Method refactoring for duplicate code.
    /// </summary>
    DuplicateExtractMethod
}
