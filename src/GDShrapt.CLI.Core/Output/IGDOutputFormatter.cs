using System;
using System.Collections.Generic;
using System.IO;
using GDShrapt.Abstractions;
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
    /// Writes reference groups to the output, grouped by declaration.
    /// </summary>
    void WriteReferenceGroups(TextWriter output, IEnumerable<GDReferenceGroupInfo> groups);

    /// <summary>
    /// Writes a structured find-refs result with symbol header, categorized sections, and summary.
    /// </summary>
    void WriteFindRefsResult(TextWriter output, GDFindRefsResultInfo result);

    /// <summary>
    /// Writes a list query result to the output.
    /// </summary>
    void WriteListResult(TextWriter output, GDListResult result);

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

    /// <summary>
    /// How to group the output. Default is by file.
    /// </summary>
    public GDGroupBy GroupBy { get; set; } = GDGroupBy.File;
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
/// Grouping mode for analysis output.
/// </summary>
public enum GDGroupBy
{
    /// <summary>
    /// Group by file (default).
    /// </summary>
    File,

    /// <summary>
    /// Group by rule code.
    /// </summary>
    Rule,

    /// <summary>
    /// Group by severity.
    /// </summary>
    Severity
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
    public int? EndColumn { get; set; }
    public string? Context { get; set; }
    public bool IsDeclaration { get; set; }
    public bool IsOverride { get; set; }
    public bool IsSuperCall { get; set; }
    public bool IsWrite { get; set; }
    public GDReferenceConfidence? Confidence { get; set; }
    public string? Reason { get; set; }
    public bool IsContractString { get; set; }
    public bool IsSignalConnection { get; set; }
    public string? SignalName { get; set; }
    public bool IsSceneSignal { get; set; }
    public string? ReceiverTypeName { get; set; }

    // Provenance fields (populated from rename planner)
    public string? PromotionLabel { get; set; }
    public List<string>? PromotionProofParts { get; set; }
    public string? PromotionFilter { get; set; }
    public List<GDProvenanceEntryInfo>? DetailedProvenance { get; set; }
    public string? ProvenanceVariableName { get; set; }
}

/// <summary>
/// Per-type provenance entry for output.
/// </summary>
public class GDProvenanceEntryInfo
{
    public string TypeName { get; set; } = "";
    public string SourceReason { get; set; } = "";
    public int? SourceLine { get; set; }
    public string? SourceFilePath { get; set; }
    public List<GDCallSiteInfo> CallSites { get; set; } = new();
}

/// <summary>
/// Call site info for provenance chains.
/// </summary>
public class GDCallSiteInfo
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public string Expression { get; set; } = "";
    public bool? IsExplicitType { get; set; }
    public List<GDCallSiteInfo> InnerChain { get; set; } = new();
}

/// <summary>
/// A group of references belonging to one declaration.
/// </summary>
public class GDReferenceGroupInfo
{
    public string? ClassName { get; set; }
    public string DeclarationFilePath { get; set; } = string.Empty;
    public int DeclarationLine { get; set; }
    public int DeclarationColumn { get; set; }
    public bool IsOverride { get; set; }
    public bool IsInherited { get; set; }
    public bool IsCrossFile { get; set; }
    public bool IsSignalConnection { get; set; }
    public string? SymbolName { get; set; }
    public List<GDReferenceInfo> References { get; set; } = new();
    public List<GDReferenceGroupInfo> Overrides { get; set; } = new();
}

/// <summary>
/// Structured find-refs result with symbol metadata and categorized groups.
/// </summary>
public class GDFindRefsResultInfo
{
    public string SymbolName { get; set; } = string.Empty;
    public string SymbolKind { get; set; } = "unknown";
    public string? DeclaredInClassName { get; set; }
    public string? DeclaredInFilePath { get; set; }
    public int DeclaredAtLine { get; set; }
    public List<GDReferenceGroupInfo> PrimaryGroups { get; set; } = new();
    public List<GDReferenceGroupInfo> UnrelatedGroups { get; set; } = new();
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

/// <summary>
/// Kind of item returned by the list command.
/// </summary>
public enum GDListItemKind
{
    Class,
    Signal,
    Autoload,
    EngineCallback,
    Method,
    Variable,
    Export,
    Node,
    Scene,
    Resource,
    Enum
}

/// <summary>
/// Sort order for list results.
/// </summary>
public enum GDListSortBy
{
    Name,
    File
}

/// <summary>
/// A single item in a list query result.
/// </summary>
public class GDListItemInfo
{
    public string Name { get; set; } = "";
    public GDListItemKind Kind { get; set; }
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public string? OwnerScope { get; set; }
    public string? SemanticType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Result of a list query.
/// </summary>
public class GDListResult
{
    public GDListItemKind QueryKind { get; set; }
    public List<GDListItemInfo> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string ProjectPath { get; set; } = "";
}
