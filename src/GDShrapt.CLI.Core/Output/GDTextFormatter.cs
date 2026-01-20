using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            output.WriteLine($"--- {file.FilePath} ---");

            foreach (var diag in file.Diagnostics)
            {
                var severity = FormatSeverity(diag.Severity);
                output.WriteLine($"  {file.FilePath}:{diag.Line}:{diag.Column}: {severity} {diag.Code}: {diag.Message}");
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
        GDSeverity.Error => "error",
        GDSeverity.Warning => "warning",
        GDSeverity.Information => "info",
        GDSeverity.Hint => "hint",
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
        foreach (var reference in references)
        {
            var marker = reference.IsDeclaration ? "[decl]" : reference.IsWrite ? "[write]" : "[read]";
            var context = string.IsNullOrEmpty(reference.Context) ? "" : $" // {reference.Context}";
            output.WriteLine($"  {reference.FilePath}:{reference.Line}:{reference.Column} {marker}{context}");
        }
    }

    public void WriteMessage(TextWriter output, string message)
    {
        output.WriteLine(message);
    }

    public void WriteError(TextWriter output, string error)
    {
        output.WriteLine($"Error: {error}");
    }
}
