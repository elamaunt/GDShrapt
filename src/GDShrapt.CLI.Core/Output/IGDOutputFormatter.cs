using System;
using System.Collections.Generic;
using System.IO;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Interface for output formatters.
/// </summary>
public interface IGDOutputFormatter
{
    /// <summary>
    /// Gets the format name (e.g., "text", "json", "sarif").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Writes analysis results to the output.
    /// </summary>
    void WriteAnalysisResult(TextWriter output, GDAnalysisResult result);

    /// <summary>
    /// Writes symbol list to the output.
    /// </summary>
    void WriteSymbols(TextWriter output, IEnumerable<GDSymbolInfo> symbols);

    /// <summary>
    /// Writes reference list to the output.
    /// </summary>
    void WriteReferences(TextWriter output, IEnumerable<GDReferenceInfo> references);

    /// <summary>
    /// Writes a simple message.
    /// </summary>
    void WriteMessage(TextWriter output, string message);

    /// <summary>
    /// Writes an error message.
    /// </summary>
    void WriteError(TextWriter output, string error);
}

/// <summary>
/// Analysis result data.
/// </summary>
public class GDAnalysisResult
{
    public string ProjectPath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int FilesWithErrors { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalHints { get; set; }
    public List<GDFileDiagnostics> Files { get; set; } = new();
}

/// <summary>
/// Diagnostics for a single file.
/// </summary>
public class GDFileDiagnostics
{
    public string FilePath { get; set; } = string.Empty;
    public List<GDDiagnosticInfo> Diagnostics { get; set; } = new();
}

/// <summary>
/// Single diagnostic info.
/// </summary>
public class GDDiagnosticInfo
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public GDSeverity Severity { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}

/// <summary>
/// Diagnostic severity levels.
/// </summary>
public enum GDSeverity
{
    Error,
    Warning,
    Information,
    Hint
}

/// <summary>
/// Symbol information for output.
/// </summary>
public class GDSymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string? ContainerName { get; set; }
}

/// <summary>
/// Reference information for output.
/// </summary>
public class GDReferenceInfo
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? Context { get; set; }
    public bool IsDeclaration { get; set; }
    public bool IsWrite { get; set; }
}

/// <summary>
/// Helpers for converting severity types to CLI's GDSeverity.
/// Uses GDSeverityMapper from Semantics to avoid duplication.
/// </summary>
public static class GDSeverityHelper
{
    /// <summary>
    /// Converts linter severity to CLI severity.
    /// </summary>
    public static GDSeverity FromLinter(GDLintSeverity severity)
        => FromIndex(GDSeverityMapper.ToCliSeverityIndex(severity));

    /// <summary>
    /// Converts validator severity to CLI severity.
    /// </summary>
    public static GDSeverity FromValidator(Reader.GDDiagnosticSeverity severity)
        => FromIndex(GDSeverityMapper.ToCliSeverityIndex(severity));

    /// <summary>
    /// Converts unified severity to CLI severity.
    /// </summary>
    public static GDSeverity FromUnified(GDUnifiedDiagnosticSeverity severity)
        => FromIndex(GDSeverityMapper.ToCliSeverityIndex(severity));

    /// <summary>
    /// Gets configured severity from config or returns default.
    /// </summary>
    public static GDSeverity GetConfigured(
        GDProjectConfig config,
        string ruleId,
        GDSeverity defaultSeverity)
    {
        if (config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) && ruleConfig.Severity.HasValue)
        {
            // Convert from Reader.GDDiagnosticSeverity to unified, then to CLI
            var unified = GDSeverityMapper.FromValidator(ruleConfig.Severity.Value);
            return FromUnified(unified);
        }
        return defaultSeverity;
    }

    private static GDSeverity FromIndex(int index)
    {
        return index switch
        {
            0 => GDSeverity.Error,
            1 => GDSeverity.Warning,
            2 => GDSeverity.Information,
            3 => GDSeverity.Hint,
            _ => GDSeverity.Information
        };
    }
}
