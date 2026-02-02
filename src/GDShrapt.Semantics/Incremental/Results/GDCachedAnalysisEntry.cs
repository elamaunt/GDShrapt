using GDShrapt.Semantics.Incremental.Cache;

namespace GDShrapt.Semantics.Incremental.Results;

/// <summary>
/// Represents a cached analysis entry for a single file.
/// </summary>
public class GDCachedAnalysisEntry
{
    /// <summary>
    /// Cache key for this entry.
    /// </summary>
    public GDCacheKey Key { get; set; }

    /// <summary>
    /// When this entry was created.
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cached diagnostics for this file.
    /// </summary>
    public List<GDSerializedDiagnostic> Diagnostics { get; set; } = new();

    /// <summary>
    /// Cached symbol information.
    /// </summary>
    public List<GDSerializedSymbol> Symbols { get; set; } = new();

    /// <summary>
    /// Dependencies this file has on other files (for invalidation).
    /// </summary>
    public HashSet<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Files that depend on this file (for cascade invalidation).
    /// </summary>
    public HashSet<string> Dependents { get; set; } = new();

    /// <summary>
    /// File path at time of caching.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Content hash at time of caching.
    /// </summary>
    public string ContentHash { get; set; } = "";
}

/// <summary>
/// Serialized diagnostic information for caching.
/// </summary>
public class GDSerializedDiagnostic
{
    /// <summary>
    /// Diagnostic code (e.g., GD1001).
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Diagnostic message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Severity level (0=Error, 1=Warning, 2=Info, 3=Hint).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Start line (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Start column (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End line (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// End column (1-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Source of the diagnostic (validator, linter, etc.).
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Serialized symbol information for caching.
/// </summary>
public class GDSerializedSymbol
{
    /// <summary>
    /// Symbol name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Symbol kind (class, function, variable, etc.).
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Inferred or declared type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Definition line (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Definition column (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Container symbol name (for nested symbols).
    /// </summary>
    public string? ContainerName { get; set; }
}
