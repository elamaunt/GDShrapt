using GDShrapt.Semantics;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Adapter for converting GDUnifiedDiagnostic from Semantics to Plugin.Diagnostic.
/// Handles coordinate system conversion (1-based to 0-based) and category mapping.
/// </summary>
internal static class GDPluginDiagnosticAdapter
{
    /// <summary>
    /// Converts a unified diagnostic from the Semantics kernel to a Plugin diagnostic.
    /// </summary>
    /// <param name="unified">The unified diagnostic from GDDiagnosticsService.</param>
    /// <param name="script">The script reference for this diagnostic.</param>
    /// <returns>A Plugin diagnostic with 0-based coordinates.</returns>
    public static GDPluginDiagnostic Convert(GDUnifiedDiagnostic unified, GDScriptFile script)
    {
        // Convert fix descriptors to CodeFix objects
        var fixes = unified.FixDescriptors.Count > 0
            ? GDPluginFixConverter.Convert(unified.FixDescriptors)
            : Array.Empty<GDCodeFix>();

        return new GDPluginDiagnostic
        {
            RuleId = unified.Code,
            RuleName = unified.RuleName,
            Message = unified.Message,
            Severity = unified.Severity,
            Category = MapCategory(unified.Source),
            Script = script,
            // Convert Line from 1-based to 0-based. Column already 0-based.
            StartLine = Math.Max(0, unified.StartLine - 1),
            StartColumn = unified.StartColumn,
            EndLine = Math.Max(0, unified.EndLine - 1),
            EndColumn = unified.EndColumn,
            Fixes = fixes
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
            GDDiagnosticSource.SemanticValidator => GDDiagnosticCategory.Correctness,
            GDDiagnosticSource.Linter => GDDiagnosticCategory.Style,
            _ => GDDiagnosticCategory.Style
        };
    }

}
