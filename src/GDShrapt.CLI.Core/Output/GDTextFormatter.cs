using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Human-readable text output formatter.
/// </summary>
public class GDTextFormatter : IGDOutputFormatter
{
    public string FormatName => "text";

    public void WriteAnalysisResult(TextWriter output, GDAnalysisResult result)
    {
        output.WriteLine($"Analysis of: {result.ProjectPath}");
        output.WriteLine($"Total files: {result.TotalFiles}");
        output.WriteLine($"Files with errors: {result.FilesWithErrors}");
        output.WriteLine($"Total errors: {result.TotalErrors}");
        output.WriteLine($"Total warnings: {result.TotalWarnings}");
        output.WriteLine($"Total hints: {result.TotalHints}");
        output.WriteLine();

        switch (result.GroupBy)
        {
            case GDGroupBy.Rule:
                WriteGroupedByRule(output, result);
                break;
            case GDGroupBy.Severity:
                WriteGroupedBySeverity(output, result);
                break;
            case GDGroupBy.File:
            default:
                WriteGroupedByFile(output, result);
                break;
        }
    }

    private void WriteGroupedByFile(TextWriter output, GDAnalysisResult result)
    {
        foreach (var file in result.Files)
        {
            if (file.Diagnostics.Count == 0)
                continue;

            output.WriteLine($"--- {GDAnsiColors.Bold(file.FilePath)} ---");

            foreach (var diag in file.Diagnostics)
            {
                var severity = FormatSeverity(diag.Severity);
                var code = GDAnsiColors.Dim(diag.Code);
                output.WriteLine($"  {file.FilePath}:{diag.Line}:{diag.Column}: {severity} {code}: {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private void WriteGroupedByRule(TextWriter output, GDAnalysisResult result)
    {
        var allDiagnostics = result.Files
            .SelectMany(f => f.Diagnostics.Select(d => (File: f.FilePath, Diag: d)))
            .GroupBy(x => x.Diag.Code)
            .OrderBy(g => g.Key);

        foreach (var group in allDiagnostics)
        {
            var count = group.Count();
            output.WriteLine($"--- {group.Key} ({count} occurrence{(count == 1 ? "" : "s")}) ---");

            foreach (var (filePath, diag) in group.OrderBy(x => x.File).ThenBy(x => x.Diag.Line))
            {
                var severity = FormatSeverity(diag.Severity);
                output.WriteLine($"  {filePath}:{diag.Line}:{diag.Column}: {severity} {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private void WriteGroupedBySeverity(TextWriter output, GDAnalysisResult result)
    {
        var allDiagnostics = result.Files
            .SelectMany(f => f.Diagnostics.Select(d => (File: f.FilePath, Diag: d)))
            .GroupBy(x => x.Diag.Severity)
            .OrderBy(g => g.Key); // Error (0) first, then Warning, Info, Hint

        foreach (var group in allDiagnostics)
        {
            var severityName = group.Key switch
            {
                GDSeverity.Error => "Errors",
                GDSeverity.Warning => "Warnings",
                GDSeverity.Information => "Information",
                GDSeverity.Hint => "Hints",
                _ => "Unknown"
            };
            var count = group.Count();
            output.WriteLine($"--- {severityName} ({count}) ---");

            foreach (var (filePath, diag) in group.OrderBy(x => x.File).ThenBy(x => x.Diag.Line))
            {
                output.WriteLine($"  {filePath}:{diag.Line}:{diag.Column}: {diag.Code}: {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private static string FormatSeverity(GDSeverity severity) => severity switch
    {
        GDSeverity.Error => GDAnsiColors.Red("error"),
        GDSeverity.Warning => GDAnsiColors.Yellow("warning"),
        GDSeverity.Information => GDAnsiColors.Cyan("info"),
        GDSeverity.Hint => GDAnsiColors.Blue("hint"),
        _ => "unknown"
    };

    public void WriteSymbols(TextWriter output, IEnumerable<GDSymbolInfo> symbols)
    {
        foreach (var symbol in symbols)
        {
            var type = string.IsNullOrEmpty(symbol.Type) ? "" : $" : {symbol.Type}";
            var container = string.IsNullOrEmpty(symbol.ContainerName) ? "" : $" in {symbol.ContainerName}";
            output.WriteLine($"  {symbol.Kind,-12} {symbol.Name}{type} at line {symbol.Line}{container}");
        }
    }

    public void WriteReferences(TextWriter output, IEnumerable<GDReferenceInfo> references)
    {
        var grouped = references
            .GroupBy(r => r.FilePath)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            output.WriteLine($"  --- {GDAnsiColors.Bold(group.Key)} ---");

            foreach (var reference in group.OrderBy(r => r.Line).ThenBy(r => r.Column))
            {
                WriteReferenceLine(output, reference, "  ");
            }
        }
    }

    public void WriteReferenceGroups(TextWriter output, IEnumerable<GDReferenceGroupInfo> groups)
    {
        var groupList = groups.ToList();
        var regularGroups = groupList.Where(g => !g.IsCrossFile).ToList();
        var crossFileGroups = groupList.Where(g => g.IsCrossFile).ToList();

        foreach (var group in regularGroups)
        {
            WriteGroupTree(output, group, indent: "");
            output.WriteLine();
        }

        // Duck-typed / potential references
        var duckTypedRefs = crossFileGroups
            .SelectMany(g => g.References.Where(r => !r.IsContractString).Select(r => (Group: g, Ref: r)))
            .ToList();

        if (duckTypedRefs.Count > 0)
        {
            output.WriteLine(GDAnsiColors.Yellow("Duck-typed references:"));
            var byFile = duckTypedRefs.GroupBy(x => x.Ref.FilePath);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"  {fileGroup.Key}");
                foreach (var (_, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    var conf = reference.Confidence.HasValue
                        ? $"[{reference.Confidence.Value.ToString().ToLowerInvariant()}]"
                        : "[potential]";
                    var reason = !string.IsNullOrEmpty(reference.Reason) ? $" {reference.Reason}" : "";
                    output.WriteLine($"    {reference.Line}:{reference.Column} {GDAnsiColors.Yellow(conf)}{reason}");
                }
            }
            output.WriteLine();
        }

        // Contract string references
        var contractRefs = crossFileGroups
            .SelectMany(g => g.References.Where(r => r.IsContractString).Select(r => (Group: g, Ref: r)))
            .ToList();

        if (contractRefs.Count > 0)
        {
            output.WriteLine(GDAnsiColors.Magenta("Contract strings:"));
            var byFile = contractRefs.GroupBy(x => x.Ref.FilePath);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"  {fileGroup.Key}");
                foreach (var (_, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    var context = !string.IsNullOrEmpty(reference.Context) ? $" {reference.Context}" : "";
                    output.WriteLine($"    {reference.Line}:{reference.Column}{context}");
                }
            }
            output.WriteLine();
        }
    }

    private static void WriteGroupTree(TextWriter output, GDReferenceGroupInfo group, string indent)
    {
        var header = !string.IsNullOrEmpty(group.ClassName)
            ? GDAnsiColors.Bold(group.ClassName)
            : GDAnsiColors.Bold(group.DeclarationFilePath);

        if (group.IsInherited)
            output.WriteLine($"{indent}{header} ({group.DeclarationFilePath})");
        else
            output.WriteLine($"{indent}{header} ({group.DeclarationFilePath}:{group.DeclarationLine})");

        foreach (var reference in group.References.OrderBy(r => r.Line).ThenBy(r => r.Column))
        {
            WriteReferenceLine(output, reference, indent, group.IsInherited);
        }

        var overrides = group.Overrides;
        for (int i = 0; i < overrides.Count; i++)
        {
            var isLast = i == overrides.Count - 1;
            var branch = isLast ? "\u2514\u2500\u2500 " : "\u251c\u2500\u2500 ";
            var childIndent = indent + (isLast ? "    " : "\u2502   ");

            var ovr = overrides[i];
            var ovrHeader = !string.IsNullOrEmpty(ovr.ClassName)
                ? GDAnsiColors.Bold(ovr.ClassName)
                : GDAnsiColors.Bold(ovr.DeclarationFilePath);

            if (ovr.IsInherited)
                output.WriteLine($"{indent}{branch}{ovrHeader} ({ovr.DeclarationFilePath})");
            else
                output.WriteLine($"{indent}{branch}{ovrHeader} ({ovr.DeclarationFilePath}:{ovr.DeclarationLine})");

            foreach (var reference in ovr.References.OrderBy(r => r.Line).ThenBy(r => r.Column))
            {
                WriteReferenceLine(output, reference, childIndent, ovr.IsInherited);
            }

            if (ovr.Overrides.Count > 0)
            {
                WriteGroupTree(output, ovr, childIndent);
            }
        }
    }

    private static void WriteReferenceLine(TextWriter output, GDReferenceInfo reference, string indent = "", bool isInherited = false)
    {
        string marker;
        if (reference.Confidence.HasValue)
        {
            var conf = reference.Confidence.Value.ToString().ToLowerInvariant();
            marker = reference.IsContractString ? $"[contract-{conf}]" : $"[{conf}]";
        }
        else if (isInherited)
            marker = reference.IsWrite ? "[write-base]" : "[read-base]";
        else if (reference.IsOverride)
            marker = "[overr]";
        else if (reference.IsDeclaration)
            marker = "[decl]";
        else if (reference.IsSuperCall)
            marker = "[read-base]";
        else if (reference.IsWrite)
            marker = "[write]";
        else
            marker = "[read]";

        var context = string.IsNullOrEmpty(reference.Context) ? "" : $" // {reference.Context}";
        output.WriteLine($"{indent}  {reference.Line}:{reference.Column} {marker}{context}");
    }

    public void WriteMessage(TextWriter output, string message)
    {
        output.WriteLine(message);
    }

    public void WriteError(TextWriter output, string error)
    {
        output.WriteLine($"{GDAnsiColors.Red("Error")}: {error}");
    }
}
