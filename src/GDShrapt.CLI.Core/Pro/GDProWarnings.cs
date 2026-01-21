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
        GDProFeatureKind.IncrementalAnalysis => "Incremental analysis",
        GDProFeatureKind.PersistentCache => "Persistent cache",
        GDProFeatureKind.SarifExport => "SARIF export",
        GDProFeatureKind.BaselineComparison => "Baseline comparison",
        GDProFeatureKind.CIAnnotations => "CI annotations",
        GDProFeatureKind.PolicyEnforcement => "Policy enforcement",
        GDProFeatureKind.HtmlReports => "HTML reports",
        GDProFeatureKind.DebtScoring => "Technical debt scoring",
        _ => feature.ToString()
    };

    /// <summary>
    /// Writes a warning when Pro module is not available.
    /// </summary>
    public static void WriteProNotAvailable(GDProFeatureKind feature, TextWriter output)
    {
        var featureName = GetFeatureName(feature);
        output.WriteLine($"[Warning] {featureName} requires GDShrapt Pro.");
        output.WriteLine("  This build does not include Pro features.");
        output.WriteLine("  Get GDShrapt Pro at: https://gdshrapt.com/pro");
    }

    /// <summary>
    /// Writes a warning when license is required but not present/valid.
    /// </summary>
    public static void WriteLicenseRequired(
        GDProFeatureKind feature,
        GDProLicenseState state,
        TextWriter output)
    {
        var featureName = GetFeatureName(feature);
        var stateMessage = GetLicenseStateMessage(state);

        output.WriteLine($"[Warning] Pro license required for: {featureName}");
        output.WriteLine($"  License status: {stateMessage}");
        output.WriteLine();
        output.WriteLine("  To activate a license:");
        output.WriteLine("    gdshrapt license activate <key>");
        output.WriteLine();
        output.WriteLine("  Or set the GDSHRAPT_LICENSE_PATH environment variable.");
        output.WriteLine("  Get a license at: https://gdshrapt.com/pro");
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
    /// </summary>
    public static string GetShortWarning(GDProFeatureKind feature)
    {
        return $"[Pro required: {GetFeatureName(feature)}]";
    }
}
