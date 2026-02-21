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
    /// Colors a kind label based on the kind type.
    /// </summary>
    public static string ColorKind(GDDeadCodeKind kind) => kind switch
    {
        GDDeadCodeKind.Variable => GDAnsiColors.Green($"{kind}:"),
        GDDeadCodeKind.Function => GDAnsiColors.Cyan($"{kind}:"),
        GDDeadCodeKind.Signal => GDAnsiColors.Magenta($"{kind}:"),
        GDDeadCodeKind.Constant => GDAnsiColors.Yellow($"{kind}:"),
        GDDeadCodeKind.Parameter => GDAnsiColors.Blue($"{kind}:"),
        GDDeadCodeKind.Unreachable => GDAnsiColors.Red($"{kind}:"),
        GDDeadCodeKind.EnumValue => GDAnsiColors.Orange($"{kind}:"),
        GDDeadCodeKind.InnerClass => GDAnsiColors.Blue($"{kind}:"),
        _ => $"{kind}:"
    };

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

        formatter.WriteMessage(output, GDAnsiColors.Bold("Legend:"));
        foreach (var code in distinctCodes)
        {
            formatter.WriteMessage(output, $"  {GDAnsiColors.Yellow(code.ToString()),-4} {GetReasonCodeLabel(code)}");
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

        formatter.WriteMessage(output, GDAnsiColors.Bold("Top files:"));
        var maxLen = top.Max(t => GetRelativePath(t.FilePath, projectRoot).Length);
        foreach (var (filePath, itemCount) in top)
        {
            var relPath = GetRelativePath(filePath, projectRoot);
            formatter.WriteMessage(output, $"  {relPath.PadRight(maxLen + 2)}{GDAnsiColors.Cyan(itemCount.ToString()),3}");
        }
    }

    /// <summary>
    /// Writes the "By kind" summary. In default mode, only non-zero kinds are shown.
    /// In verbose mode, all kinds are shown including zero-count.
    /// </summary>
    public static void WriteKindSummary(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeReport report, bool verbose = false)
    {
        var kindCounts = report.Items.GroupBy(i => i.Kind).ToDictionary(g => g.Key, g => g.Count());

        formatter.WriteMessage(output, GDAnsiColors.Bold("By kind:"));
        var allKinds = (GDDeadCodeKind[])Enum.GetValues(typeof(GDDeadCodeKind));
        var parts = allKinds
            .Where(k => verbose || (kindCounts.TryGetValue(k, out var cnt) && cnt > 0))
            .Select(k =>
            {
                var count = kindCounts.TryGetValue(k, out var c) ? c : 0;
                return $"{ColorKind(k)} {GDAnsiColors.White(count.ToString())}";
            });
        formatter.WriteMessage(output, $"  {string.Join("  ", parts)}");
    }

    /// <summary>
    /// Writes the "By confidence" summary line.
    /// </summary>
    public static void WriteConfidenceSummary(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeReport report)
    {
        formatter.WriteMessage(output, GDAnsiColors.Bold("By confidence:"));

        var parts = new List<string>();
        parts.Add($"{GDAnsiColors.Green("Strict:")} {GDAnsiColors.White(report.StrictCount.ToString())}");

        if (report.PotentialCount > 0)
            parts.Add($"{GDAnsiColors.Yellow("Potential:")} {GDAnsiColors.White(report.PotentialCount.ToString())}");

        if (report.NameMatchCount > 0)
            parts.Add($"{GDAnsiColors.Orange("NameMatch:")} {GDAnsiColors.White(report.NameMatchCount.ToString())}");

        if (report.DroppedByReflectionCount > 0)
            parts.Add($"{GDAnsiColors.Dim("Suppressed:")} {GDAnsiColors.White(report.DroppedByReflectionCount.ToString())}");

        formatter.WriteMessage(output, $"  {string.Join("  ", parts)}");
    }

    /// <summary>
    /// Writes the "Analysis scope" section (project-wide stats for --explain mode).
    /// </summary>
    public static void WriteAnalysisScope(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeReport report)
    {
        formatter.WriteMessage(output, GDAnsiColors.Bold("Analysis scope:"));

        if (report.TotalCallSitesRegistered > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  Call sites registered:        {report.TotalCallSitesRegistered}"));

        if (report.FilesAnalyzed > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  Files analyzed:               {report.FilesAnalyzed}"));

        if (report.SceneSignalConnectionsConsidered > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  Scene signal connections:      {report.SceneSignalConnectionsConsidered}"));

        if (report.VirtualMethodsSkipped > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  Engine callbacks excluded:     {report.VirtualMethodsSkipped}"));

        if (report.AutoloadsResolved > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  Autoloads resolved:            {report.AutoloadsResolved}"));

        if (report.CSharpInteropExcluded > 0)
            formatter.WriteMessage(output, GDAnsiColors.Dim($"  C# interop excluded:           {report.CSharpInteropExcluded}"));
    }

    /// <summary>
    /// Computes the plain-text width of the item prefix (position + kind + name) for alignment.
    /// </summary>
    public static int GetItemTextWidth(GDDeadCodeItem item)
    {
        return $"  {item.Line + 1}:{item.Column} {item.Kind}: {item.Name}".Length;
    }

    /// <summary>
    /// Writes a single dead-code item line with reason code, right-aligned tags.
    /// </summary>
    public static void WriteItem(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeItem item, bool showEvidence, int maxItemTextWidth = 0)
    {
        var confidence = item.Confidence == GDReferenceConfidence.Strict ? "" : $" {GDAnsiColors.Yellow($"[{item.Confidence}]")}";
        var exportTag = item.IsExportedOrOnready ? $" {GDAnsiColors.Magenta("@export")}" : "";
        var tag = $"{GDAnsiColors.Yellow($"[{item.ReasonCode}]")}{confidence}{exportTag}";

        var prefix = $"  {GDAnsiColors.Dim($"{item.Line + 1}:{item.Column}")} {ColorKind(item.Kind)} {item.Name}";
        var plainWidth = GetItemTextWidth(item);
        var padding = maxItemTextWidth > plainWidth ? new string(' ', maxItemTextWidth - plainWidth) : "";

        formatter.WriteMessage(output, $"{prefix}{padding} {tag}");

        if (showEvidence && item.Evidence != null)
        {
            var ev = item.Evidence;
            if (ev.IsVirtualOrEntrypoint)
                formatter.WriteMessage(output, GDAnsiColors.Dim("    Note: Virtual/entrypoint method"));
            if (ev.ReflectionSites?.Count > 0)
            {
                foreach (var site in ev.ReflectionSites)
                    formatter.WriteMessage(output, GDAnsiColors.Dim($"    Reflected from: {site}"));
            }
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

    /// <summary>
    /// Writes the "Suppressed by reflection" section showing items excluded from the report.
    /// When limit > 0, shows only the top N items with a "see all" hint.
    /// When limit = 0, shows all items.
    /// </summary>
    public static void WriteDroppedByReflection(
        IGDOutputFormatter formatter,
        TextWriter output,
        GDDeadCodeReport report,
        string projectRoot,
        int limit = 0)
    {
        var dropped = report.DroppedByReflection;
        if (dropped.Count == 0)
            return;

        formatter.WriteMessage(output, GDAnsiColors.Bold($"Suppressed by reflection: {GDAnsiColors.Cyan(dropped.Count.ToString())} item(s)"));

        // Kind breakdown
        var byKind = dropped.GroupBy(d => d.Kind).OrderBy(g => g.Key).ToList();
        var kindParts = byKind.Select(g => $"{ColorKind(g.Key)} {GDAnsiColors.White(g.Count().ToString())}");
        formatter.WriteMessage(output, $"  {string.Join("  ", kindParts)}");

        // Suppression rules (only in full view)
        if (limit == 0)
        {
            var rules = dropped
                .Select(d => (d.ReflectionKind, d.CallMethod))
                .Distinct()
                .OrderBy(r => r.ReflectionKind)
                .ThenBy(r => r.CallMethod)
                .ToList();

            formatter.WriteMessage(output, "");
            formatter.WriteMessage(output, GDAnsiColors.Bold("Suppression rules:"));
            foreach (var (kind, method) in rules)
            {
                var listMethod = kind switch
                {
                    GDReflectionKind.Method => "get_method_list()",
                    GDReflectionKind.Property => "get_property_list()",
                    GDReflectionKind.Signal => "get_signal_list()",
                    _ => "reflection"
                };
                formatter.WriteMessage(output, GDAnsiColors.Dim($"  {listMethod} + {method}()"));
            }
        }

        formatter.WriteMessage(output, "");

        var allItems = dropped.OrderBy(d => d.FilePath).ThenBy(d => d.Line).ToList();
        var itemsToShow = limit > 0 ? allItems.Take(limit).ToList() : allItems;

        var byFile = itemsToShow.GroupBy(d => d.FilePath).OrderBy(g => g.Key);
        foreach (var fileGroup in byFile)
        {
            var relPath = GetRelativePath(fileGroup.Key, projectRoot);
            formatter.WriteMessage(output, GDAnsiColors.Bold($"{relPath}:"));

            foreach (var item in fileGroup.OrderBy(d => d.Line))
            {
                var listMethod = item.ReflectionKind switch
                {
                    GDReflectionKind.Method => "get_method_list()",
                    GDReflectionKind.Property => "get_property_list()",
                    GDReflectionKind.Signal => "get_signal_list()",
                    _ => "reflection"
                };

                var siteFile = Path.GetFileName(item.ReflectionSiteFile);
                var evidence = GDAnsiColors.Dim($"<- {listMethod} + {item.CallMethod}() at {siteFile}:{item.ReflectionSiteLine + 1}");
                formatter.WriteMessage(output,
                    $"  {GDAnsiColors.Dim($"{item.Line + 1}:{item.Column}")} {ColorKind(item.Kind)} {item.Name,-20} {evidence}");
            }
            formatter.WriteMessage(output, "");
        }

        if (limit > 0 && dropped.Count > limit)
        {
            var remaining = dropped.Count - limit;
            formatter.WriteMessage(output,
                GDAnsiColors.Dim($"... and {remaining} more (use ")
                + GDAnsiColors.Magenta("--show-suppressed")
                + GDAnsiColors.Dim(" to print full suppressed list)"));
        }
    }

    /// <summary>
    /// Writes a contextual tip based on options not yet used by the user.
    /// </summary>
    public static void WriteTip(IGDOutputFormatter formatter, TextWriter output, GDDeadCodeCommandOptions options)
    {
        string? option = null;
        string? description = null;

        if (!options.Explain)
        {
            option = "--explain";
            description = "to see evidence details for each item";
        }
        else if (!options.ExcludeTests)
        {
            option = "--exclude-tests";
            description = "to skip test files";
        }
        else if (!options.TopN.HasValue)
        {
            option = "--top N";
            description = "to limit output to top N files";
        }
        else if (!options.FailIfFound)
        {
            option = "--fail-if-found";
            description = "for CI gating";
        }

        if (option != null)
            formatter.WriteMessage(output,
                GDAnsiColors.Dim("Tip: Use ") + GDAnsiColors.Magenta(option) + GDAnsiColors.Dim($" {description}"));
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
