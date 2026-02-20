using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Shared formatting helper for dead-code output. Used by both Base and Pro CLI.
/// </summary>
public static class GDDeadCodeOutputHelper
{
    /// <summary>
    /// Gets a human-readable label for a reason code.
    /// </summary>
    public static string GetReasonCodeLabel(GDDeadCodeReasonCode code) => code switch
    {
        GDDeadCodeReasonCode.VNR => "Variable never read",
        GDDeadCodeReasonCode.VEX => "Variable has @export",
        GDDeadCodeReasonCode.VOR => "Variable has @onready",
        GDDeadCodeReasonCode.FNC => "Function has no callers",
        GDDeadCodeReasonCode.FDT => "May be called via duck-typing",
        GDDeadCodeReasonCode.SNE => "Signal never emitted",
        GDDeadCodeReasonCode.SCB => "Signal connected but never emitted",
        GDDeadCodeReasonCode.CNU => "Constant never used",
        GDDeadCodeReasonCode.PNU => "Parameter never used",
        GDDeadCodeReasonCode.UCR => "Unreachable code",
        GDDeadCodeReasonCode.ENU => "Enum value never used",
        GDDeadCodeReasonCode.ICU => "Inner class unused",
        _ => code.ToString()
    };

    /// <summary>
    /// Writes the legend section, showing only reason codes present in the results.
    /// </summary>
    public static void WriteLegend(IGDOutputFormatter formatter, TextWriter output, IEnumerable<GDDeadCodeReasonCode> codes)
    {
        var distinctCodes = codes.Distinct().OrderBy(c => c).ToList();
        if (distinctCodes.Count == 0)
            return;

        formatter.WriteMessage(output, "Legend:");
        foreach (var code in distinctCodes)
        {
            formatter.WriteMessage(output, $"  {code,-4} {GetReasonCodeLabel(code)}");
        }
    }

    /// <summary>
    /// Writes the "Top files" section showing files with most dead code.
    /// </summary>
    public static void WriteTopOffenders(
        IGDOutputFormatter formatter,
        TextWriter output,
        GDDeadCodeReport report,
        string projectRoot,
        int count = 5)
    {
        var top = report.TopOffenders(count);
        if (top.Count == 0)
            return;

        formatter.WriteMessage(output, "Top files:");
        var maxLen = top.Max(t => GetRelativePath(t.FilePath, projectRoot).Length);
        foreach (var (filePath, itemCount) in top)
        {
            var relPath = GetRelativePath(filePath, projectRoot);
            formatter.WriteMessage(output, $"  {relPath.PadRight(maxLen + 2)}{itemCount,3}");
        }
    }

    /// <summary>
    /// Writes the "By kind" compact summary line.
    /// </summary>
    public static void WriteKindSummary(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeReport report)
    {
        var byKind = report.Items.GroupBy(i => i.Kind).OrderBy(g => g.Key).ToList();
        if (byKind.Count == 0)
            return;

        formatter.WriteMessage(output, "By kind:");
        var parts = byKind.Select(g => $"{g.Key}: {g.Count()}");
        formatter.WriteMessage(output, $"  {string.Join("  ", parts)}");
    }

    /// <summary>
    /// Writes a single dead-code item line with reason code.
    /// </summary>
    public static void WriteItem(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeItem item, bool showEvidence)
    {
        var confidence = item.Confidence == GDReferenceConfidence.Strict ? "" : $" [{item.Confidence}]";
        var exportTag = item.IsExportedOrOnready ? " @export" : "";
        formatter.WriteMessage(output,
            $"  {item.Line + 1}:{item.Column} {item.Kind}: {item.Name,-20} [{item.ReasonCode}]{confidence}{exportTag}");

        if (showEvidence && item.Evidence != null)
        {
            var ev = item.Evidence;
            formatter.WriteMessage(output,
                $"    Evidence: {ev.CallSitesScanned} call sites scanned, {ev.CrossFileAccessChecks} cross-file checks, {ev.DuckTypeMatches} duck-type matches");
            if (ev.IsVirtualOrEntrypoint)
                formatter.WriteMessage(output, "    Note: Virtual/entrypoint method");
        }
    }

    /// <summary>
    /// Formats a single-line compact summary for quiet/CI mode.
    /// </summary>
    public static string FormatSummaryLine(GDDeadCodeReport report)
    {
        var byKind = report.Items.GroupBy(i => i.Kind).OrderBy(g => g.Key);
        var parts = byKind.Select(g =>
        {
            var abbrev = g.Key switch
            {
                GDDeadCodeKind.Variable => "var",
                GDDeadCodeKind.Function => "func",
                GDDeadCodeKind.Signal => "sig",
                GDDeadCodeKind.Parameter => "param",
                GDDeadCodeKind.Unreachable => "unreachable",
                GDDeadCodeKind.Constant => "const",
                GDDeadCodeKind.EnumValue => "enum",
                GDDeadCodeKind.InnerClass => "class",
                _ => g.Key.ToString().ToLowerInvariant()
            };
            return $"{g.Count()} {abbrev}";
        });

        return $"dead-code: {report.Items.Count} items ({string.Join(", ", parts)})";
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath).Replace('\\', '/');
        }
        catch
        {
            return fullPath.Replace('\\', '/');
        }
    }
}
