using GDShrapt.Semantics;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Adapter for converting GDUnifiedDiagnostic from Semantics to Plugin.Diagnostic.
/// Handles coordinate system conversion (1-based to 0-based) and category mapping.
/// </summary>
internal static class PluginDiagnosticAdapter
{
    /// <summary>
    /// Converts a unified diagnostic from the Semantics kernel to a Plugin diagnostic.
    /// </summary>
    /// <param name="unified">The unified diagnostic from GDDiagnosticsService.</param>
    /// <param name="script">The script reference for this diagnostic.</param>
    /// <returns>A Plugin diagnostic with 0-based coordinates.</returns>
    public static Diagnostic Convert(GDUnifiedDiagnostic unified, GDPluginScriptReference script)
    {
        return new Diagnostic
        {
            RuleId = unified.Code,
            Message = unified.Message,
            Severity = unified.Severity,
            Category = MapCategory(unified.Source),
            Script = script,
            // Convert from 1-based (Semantics) to 0-based (Plugin)
            StartLine = Math.Max(0, unified.StartLine - 1),
            StartColumn = Math.Max(0, unified.StartColumn - 1),
            EndLine = Math.Max(0, unified.EndLine - 1),
            EndColumn = Math.Max(0, unified.EndColumn - 1),
            Fixes = Array.Empty<CodeFix>() // Fixes are generated separately via GDFormatCodeService
        };
    }

    /// <summary>
    /// Maps diagnostic source to category.
    /// </summary>
    private static GDDiagnosticCategory MapCategory(GDDiagnosticSource source)
    {
        return source switch
        {
            GDDiagnosticSource.Syntax => GDDiagnosticCategory.Syntax,
            GDDiagnosticSource.Validator => GDDiagnosticCategory.Correctness,
            GDDiagnosticSource.Linter => GDDiagnosticCategory.Style,
            _ => GDDiagnosticCategory.Style
        };
    }
}
