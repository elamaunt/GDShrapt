using GDShrapt.Reader;
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
            // Convert from 1-based (Semantics) to 0-based (Plugin)
            StartLine = Math.Max(0, unified.StartLine - 1),
            StartColumn = Math.Max(0, unified.StartColumn - 1),
            EndLine = Math.Max(0, unified.EndLine - 1),
            EndColumn = Math.Max(0, unified.EndColumn - 1),
            Fixes = fixes
        };
    }

    /// <summary>
    /// Converts a GDDiagnostic from the Validator to a Plugin diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic from GDSemanticValidator.</param>
    /// <param name="script">The script reference for this diagnostic.</param>
    /// <returns>A Plugin diagnostic with 0-based coordinates.</returns>
    public static GDPluginDiagnostic ConvertFromValidator(GDDiagnostic diagnostic, GDScriptFile script)
    {
        return new GDPluginDiagnostic
        {
            RuleId = diagnostic.CodeString,
            RuleName = null,
            Message = diagnostic.Message,
            Severity = MapSeverity(diagnostic.Severity),
            Category = GDDiagnosticCategory.Correctness,
            Script = script,
            // Convert from 1-based (Validator) to 0-based (Plugin)
            StartLine = Math.Max(0, diagnostic.StartLine - 1),
            StartColumn = Math.Max(0, diagnostic.StartColumn - 1),
            EndLine = Math.Max(0, diagnostic.EndLine - 1),
            EndColumn = Math.Max(0, diagnostic.EndColumn - 1),
            Fixes = Array.Empty<GDCodeFix>()
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

    /// <summary>
    /// Maps validator severity to plugin severity.
    /// </summary>
    private static GDDiagnosticSeverity MapSeverity(Reader.GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            Reader.GDDiagnosticSeverity.Error => GDDiagnosticSeverity.Error,
            Reader.GDDiagnosticSeverity.Warning => GDDiagnosticSeverity.Warning,
            Reader.GDDiagnosticSeverity.Hint => GDDiagnosticSeverity.Hint,
            _ => GDDiagnosticSeverity.Warning
        };
    }
}
