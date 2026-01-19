using GDShrapt.Formatter;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Handles finding and applying quick fixes for diagnostics.
/// Uses GDFormatCodeService for formatting fixes.
/// </summary>
internal class GDQuickFixHandler
{
    private readonly GDPluginDiagnosticService _diagnosticService;
    private readonly GDConfigManager _configManager;

    public GDQuickFixHandler(GDPluginDiagnosticService diagnosticService, GDConfigManager configManager)
    {
        _diagnosticService = diagnosticService;
        _configManager = configManager;
    }

    /// <summary>
    /// Gets all available fixes at the specified cursor position.
    /// </summary>
    /// <param name="script">The script reference.</param>
    /// <param name="line">Cursor line (0-based).</param>
    /// <param name="column">Cursor column (0-based).</param>
    /// <returns>List of available fixes with their parent diagnostics.</returns>
    public IReadOnlyList<GDQuickFixItem> GetFixesAtPosition(GDScriptFile script, int line, int column)
    {
        var diagnostics = _diagnosticService.GetDiagnostics(script);
        var result = new List<GDQuickFixItem>();

        foreach (var diagnostic in diagnostics)
        {
            // Check if cursor is within the diagnostic span
            if (IsCursorInGDPluginDiagnostic(diagnostic, line, column))
            {
                foreach (var fix in diagnostic.Fixes)
                {
                    result.Add(new GDQuickFixItem(diagnostic, fix));
                }
            }
        }

        // Sort by severity (errors first, then warnings, then hints)
        return result
            .OrderBy(f => f.GDPluginDiagnostic.Severity)
            .ToList();
    }

    /// <summary>
    /// Gets all available fixes on the specified line.
    /// </summary>
    /// <param name="script">The script reference.</param>
    /// <param name="line">Line number (0-based).</param>
    /// <returns>List of available fixes with their parent diagnostics.</returns>
    public IReadOnlyList<GDQuickFixItem> GetFixesOnLine(GDScriptFile script, int line)
    {
        var diagnostics = _diagnosticService.GetDiagnostics(script);
        var result = new List<GDQuickFixItem>();

        foreach (var diagnostic in diagnostics)
        {
            // Check if diagnostic is on this line
            if (diagnostic.StartLine <= line && diagnostic.EndLine >= line)
            {
                foreach (var fix in diagnostic.Fixes)
                {
                    result.Add(new GDQuickFixItem(diagnostic, fix));
                }
            }
        }

        return result
            .OrderBy(f => f.GDPluginDiagnostic.Severity)
            .ToList();
    }

    /// <summary>
    /// Gets all available fixes for the entire script.
    /// </summary>
    /// <param name="script">The script reference.</param>
    /// <returns>List of all available fixes.</returns>
    public IReadOnlyList<GDQuickFixItem> GetAllFixes(GDScriptFile script)
    {
        var diagnostics = _diagnosticService.GetDiagnostics(script);
        var result = new List<GDQuickFixItem>();

        foreach (var diagnostic in diagnostics)
        {
            foreach (var fix in diagnostic.Fixes)
            {
                result.Add(new GDQuickFixItem(diagnostic, fix));
            }
        }

        return result
            .OrderBy(f => f.GDPluginDiagnostic.StartLine)
            .ThenBy(f => f.GDPluginDiagnostic.StartColumn)
            .ToList();
    }

    /// <summary>
    /// Applies a single fix to the source code.
    /// </summary>
    /// <param name="fix">The fix to apply.</param>
    /// <param name="sourceCode">Original source code.</param>
    /// <returns>Modified source code.</returns>
    public string ApplyFix(GDCodeFix fix, string sourceCode)
    {
        try
        {
            return fix.Apply(sourceCode);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error applying fix '{fix.Title}': {ex.Message}");
            return sourceCode;
        }
    }

    /// <summary>
    /// Applies multiple fixes to the source code.
    /// Fixes are applied in reverse order (bottom to top) to preserve line numbers.
    /// </summary>
    /// <param name="fixes">The fixes to apply.</param>
    /// <param name="sourceCode">Original source code.</param>
    /// <returns>Modified source code.</returns>
    public string ApplyFixes(IEnumerable<GDQuickFixItem> fixes, string sourceCode)
    {
        // Sort by position in reverse order (bottom to top, right to left)
        var sortedFixes = fixes
            .OrderByDescending(f => f.GDPluginDiagnostic.StartLine)
            .ThenByDescending(f => f.GDPluginDiagnostic.StartColumn)
            .ToList();

        var result = sourceCode;
        foreach (var fixItem in sortedFixes)
        {
            result = ApplyFix(fixItem.Fix, result);
        }

        return result;
    }

    /// <summary>
    /// Applies all formatting fixes to the source code using GDFormatter.
    /// Formats the entire file using the formatter from Semantics kernel.
    /// </summary>
    /// <param name="ScriptFile">The script map.</param>
    /// <returns>Formatted source code or null if no formatting needed.</returns>
    public string? ApplyAllFormattingFixes(GDScriptFile ScriptFile)
    {
        if (ScriptFile?.Class == null)
            return null;

        try
        {
            var formatterOptions = GDFormatterOptionsFactory.FromConfig(_configManager.Config);
            var formatter = new GDFormatter(formatterOptions);

            var originalCode = ScriptFile.Class.ToString();
            var formattedCode = formatter.FormatCode(originalCode);

            if (formattedCode != originalCode)
            {
                return formattedCode;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error applying formatting: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Applies formatting fix to a specific region using GDFormatter.
    /// For simplicity, formats the entire file but returns only the changed region.
    /// </summary>
    /// <param name="ScriptFile">The script map.</param>
    /// <param name="startLine">Start line (0-based).</param>
    /// <param name="endLine">End line (0-based).</param>
    /// <returns>Formatted source code or null if no formatting needed.</returns>
    public string? ApplyFormattingToRegion(GDScriptFile ScriptFile, int startLine, int endLine)
    {
        // For simplicity, format the entire file
        // Region-specific formatting would require parsing partial code which is more complex
        return ApplyAllFormattingFixes(ScriptFile);
    }

    /// <summary>
    /// Legacy method: Applies all formatting fixes to the source code using individual fixes.
    /// </summary>
    /// <param name="script">The script reference.</param>
    /// <param name="sourceCode">Original source code.</param>
    /// <returns>Modified source code.</returns>
    [Obsolete("Use ApplyAllFormattingFixes(GDScriptFile) instead")]
    public string ApplyAllFormattingFixesLegacy(GDScriptFile script, string sourceCode)
    {
        var formattingFixes = GetAllFixes(script)
            .Where(f => f.GDPluginDiagnostic.Category == GDDiagnosticCategory.Formatting)
            .ToList();

        return ApplyFixes(formattingFixes, sourceCode);
    }

    private static bool IsCursorInGDPluginDiagnostic(GDPluginDiagnostic diagnostic, int line, int column)
    {
        // Single line diagnostic
        if (diagnostic.StartLine == diagnostic.EndLine)
        {
            return line == diagnostic.StartLine &&
                   column >= diagnostic.StartColumn &&
                   column <= diagnostic.EndColumn;
        }

        // Multi-line diagnostic
        if (line == diagnostic.StartLine)
            return column >= diagnostic.StartColumn;

        if (line == diagnostic.EndLine)
            return column <= diagnostic.EndColumn;

        return line > diagnostic.StartLine && line < diagnostic.EndLine;
    }
}

/// <summary>
/// Represents a quick fix item combining a diagnostic and its fix.
/// </summary>
internal class GDQuickFixItem
{
    /// <summary>
    /// The parent diagnostic.
    /// </summary>
    public GDPluginDiagnostic GDPluginDiagnostic { get; }

    /// <summary>
    /// The code fix.
    /// </summary>
    public GDCodeFix Fix { get; }

    /// <summary>
    /// Display title for the fix menu.
    /// </summary>
    public string DisplayTitle => Fix.Title;

    /// <summary>
    /// Severity icon for display.
    /// </summary>
    public string SeverityIcon => GDPluginDiagnostic.Severity switch
    {
        GDDiagnosticSeverity.Error => "X",
        GDDiagnosticSeverity.Warning => "!",
        GDDiagnosticSeverity.Info => "i",
        GDDiagnosticSeverity.Hint => "?",
        _ => " "
    };

    public GDQuickFixItem(GDPluginDiagnostic diagnostic, GDCodeFix fix)
    {
        GDPluginDiagnostic = diagnostic;
        Fix = fix;
    }

    public override string ToString()
    {
        return $"[{GDPluginDiagnostic.RuleId}] {Fix.Title}";
    }
}
