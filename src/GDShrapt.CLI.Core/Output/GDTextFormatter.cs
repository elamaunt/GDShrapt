using System;
using System.Collections.Generic;
using System.IO;

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

        foreach (var file in result.Files)
        {
            if (file.Diagnostics.Count == 0)
                continue;

            output.WriteLine($"--- {file.FilePath} ---");

            foreach (var diag in file.Diagnostics)
            {
                var severity = diag.Severity switch
                {
                    GDSeverity.Error => "error",
                    GDSeverity.Warning => "warning",
                    GDSeverity.Information => "info",
                    GDSeverity.Hint => "hint",
                    _ => "unknown"
                };

                output.WriteLine($"  {file.FilePath}:{diag.Line}:{diag.Column}: {severity} {diag.Code}: {diag.Message}");
            }

            output.WriteLine();
        }
    }

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
