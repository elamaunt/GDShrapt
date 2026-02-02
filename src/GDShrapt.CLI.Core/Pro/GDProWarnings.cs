using System.IO;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Standard warning messages for Pro feature gating.
/// </summary>
public static class GDProWarnings
{
    /// <summary>
    /// Gets the human-readable name for a Pro feature.
    /// </summary>
    public static string GetFeatureName(GDProFeatureKind feature) => feature switch
    {
        GDProFeatureKind.ConfidenceAwarePotential => "Potential confidence mode",
        GDProFeatureKind.ConfidenceAwareNameMatch => "NameMatch confidence mode",
        GDProFeatureKind.BatchRefactoring => "Batch refactoring",
        GDProFeatureKind.ApplyTypeAnnotations => "Apply type annotations",
        GDProFeatureKind.BatchTypeAnnotations => "Batch add type annotations",
        GDProFeatureKind.GenerateOnready => "Generate @onready",
        GDProFeatureKind.ApplyReorderMembers => "Apply member reordering",
        GDProFeatureKind.BatchReorderMembers => "Batch reorder members",
        GDProFeatureKind.PersistentCache => "Persistent cache",
        GDProFeatureKind.SarifExport => "SARIF export",
        GDProFeatureKind.BaselineComparison => "Baseline comparison",
        GDProFeatureKind.CIAnnotations => "CI annotations",
        GDProFeatureKind.PolicyEnforcement => "Policy enforcement",
        GDProFeatureKind.HtmlReports => "HTML reports",
        GDProFeatureKind.DebtScoring => "Technical debt scoring",
        GDProFeatureKind.DeadCodeRemoval => "Dead code removal",
        GDProFeatureKind.DependencyVisualization => "Dependency visualization",
        GDProFeatureKind.MetricsGates => "Metrics gates",
        GDProFeatureKind.CoverageGates => "Coverage gates",
        GDProFeatureKind.PatchExport => "Patch export",
        GDProFeatureKind.SafeDeletionPlan => "Safe deletion plan",
        GDProFeatureKind.DuplicateDetection => "Duplicate detection",
        GDProFeatureKind.SecurityScanning => "Security scanning",
        GDProFeatureKind.TrendAnalysis => "Trend analysis",
        GDProFeatureKind.SafeInline => "Safe inline",
        GDProFeatureKind.DuplicateExtractMethod => "Duplicate extract method",
        _ => feature.ToString()
    };

    /// <summary>
    /// Writes a warning when Pro module is not available.
    /// Only writes if wasEverLicensed is true (for users who had Pro before).
    /// Silent for users who never purchased.
    /// </summary>
    public static void WriteProNotAvailable(GDProFeatureKind feature, TextWriter output, bool wasEverLicensed = true)
    {
        // Only show message if user was ever licensed
        if (!wasEverLicensed)
            return;

        var featureName = GetFeatureName(feature);
        output.WriteLine($"[Warning] {featureName} not available in this build.");
        output.WriteLine("  Renew at: https://gdshrapt.com/pro");
    }

    /// <summary>
    /// Writes a warning when license is required but not present/valid.
    /// Only writes if wasEverLicensed is true (for expired/invalid licenses).
    /// Silent for users who never purchased.
    /// </summary>
    public static void WriteLicenseRequired(
        GDProFeatureKind feature,
        GDProLicenseState state,
        TextWriter output,
        bool wasEverLicensed = true)
    {
        // Only show message if user was ever licensed
        if (!wasEverLicensed)
            return;

        var featureName = GetFeatureName(feature);
        var stateMessage = GetLicenseStateMessage(state);

        output.WriteLine($"[Warning] License expired for: {featureName}");
        output.WriteLine($"  License status: {stateMessage}");
        output.WriteLine();
        output.WriteLine("  To renew your license:");
        output.WriteLine("    gdshrapt license activate <key>");
        output.WriteLine();
        output.WriteLine("  Renew at: https://gdshrapt.com/pro");
    }

    /// <summary>
    /// Writes a warning when a specific feature is not enabled.
    /// </summary>
    public static void WriteFeatureNotEnabled(GDProFeatureKind feature, TextWriter output)
    {
        var featureName = GetFeatureName(feature);
        output.WriteLine($"[Warning] Feature not enabled: {featureName}");
        output.WriteLine("  Your license may not include this feature.");
        output.WriteLine("  Contact support at: https://gdshrapt.com/support");
    }

    /// <summary>
    /// Gets a human-readable message for a license state.
    /// </summary>
    public static string GetLicenseStateMessage(GDProLicenseState state) => state switch
    {
        GDProLicenseState.ProNotAvailable => "Pro module not available",
        GDProLicenseState.Valid => "Valid",
        GDProLicenseState.Expired => "Expired",
        GDProLicenseState.Invalid => "Invalid signature",
        GDProLicenseState.Trial => "Trial",
        GDProLicenseState.NotFound => "Not found",
        GDProLicenseState.Perpetual => "Perpetual (valid)",
        GDProLicenseState.UpdatesExpired => "Perpetual (updates expired for this version)",
        _ => state.ToString()
    };

    /// <summary>
    /// Creates a short inline warning message.
    /// Returns empty string if wasEverLicensed is false.
    /// </summary>
    public static string GetShortWarning(GDProFeatureKind feature, bool wasEverLicensed = true)
    {
        if (!wasEverLicensed)
            return string.Empty;
        return $"[License expired: {GetFeatureName(feature)}]";
    }
}
