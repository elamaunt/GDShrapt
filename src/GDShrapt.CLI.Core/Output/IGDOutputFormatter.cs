using System;
using System.Collections.Generic;
using System.IO;

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
