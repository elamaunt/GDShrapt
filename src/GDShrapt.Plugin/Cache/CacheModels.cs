using GDShrapt.Plugin.Config;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Cache;

/// <summary>
/// Index of all cached files for quick lookup.
/// Stored in .gdshrapt/cache.index.json
/// </summary>
internal class CacheIndex
{
    /// <summary>
    /// Cache format version. Increment when cache format changes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Mapping of script paths to their cache info.
    /// </summary>
    public Dictionary<string, CacheFileInfo> Files { get; set; } = new();

    /// <summary>
    /// When the index was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Information about a cached file.
/// </summary>
internal class CacheFileInfo
{
    /// <summary>
    /// SHA256 hash of the file content when cached.
    /// </summary>
    public string ContentHash { get; set; } = "";

    /// <summary>
    /// When the file was last cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// File name of the lint cache (relative to cache directory).
    /// </summary>
    public string? LintCacheFile { get; set; }
}

/// <summary>
/// Cached lint analysis results for a single file.
/// </summary>
internal class LintCacheEntry
{
    /// <summary>
    /// Hash of content that was analyzed.
    /// </summary>
    public string ContentHash { get; set; } = "";

    /// <summary>
    /// Configuration hash to detect config changes.
    /// </summary>
    public string ConfigHash { get; set; } = "";

    /// <summary>
    /// When analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// Serialized diagnostics.
    /// </summary>
    public List<SerializedDiagnostic> Diagnostics { get; set; } = new();
}

/// <summary>
/// Serializable version of Diagnostic for caching.
/// </summary>
internal class SerializedDiagnostic
{
    public string RuleId { get; set; } = "";
    public string Message { get; set; } = "";
    public int Severity { get; set; }
    public int Category { get; set; }
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string? SourceText { get; set; }

    /// <summary>
    /// Converts from Diagnostic to serializable form.
    /// </summary>
    public static SerializedDiagnostic FromDiagnostic(Diagnostics.Diagnostic diag)
    {
        return new SerializedDiagnostic
        {
            RuleId = diag.RuleId,
            Message = diag.Message,
            Severity = (int)diag.Severity,
            Category = (int)diag.Category,
            StartLine = diag.StartLine,
            StartColumn = diag.StartColumn,
            EndLine = diag.EndLine,
            EndColumn = diag.EndColumn,
            SourceText = diag.SourceText
        };
    }

    /// <summary>
    /// Converts to Diagnostic (without fixes - those must be regenerated).
    /// </summary>
    public Diagnostics.Diagnostic ToDiagnostic(ScriptReference? script = null)
    {
        return new Diagnostics.Diagnostic
        {
            RuleId = RuleId,
            Message = Message,
            Severity = (GDDiagnosticSeverity)Severity,
            Category = (DiagnosticCategory)Category,
            Script = script,
            StartLine = StartLine,
            StartColumn = StartColumn,
            EndLine = EndLine,
            EndColumn = EndColumn,
            SourceText = SourceText,
            Fixes = Array.Empty<Diagnostics.CodeFix>() // Fixes need to be regenerated
        };
    }
}
