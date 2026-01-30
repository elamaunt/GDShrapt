namespace GDShrapt.Semantics;

/// <summary>
/// Semantic analysis configuration options.
/// Controls parallelism, incremental analysis, and performance tuning.
/// </summary>
public class GDSemanticsConfig
{
    /// <summary>
    /// Maximum degree of parallelism for multi-threaded analysis.
    /// -1 = automatic (Environment.ProcessorCount)
    /// 0 = sequential (no parallelism)
    /// Positive number = explicit thread count
    /// Default: -1 (automatic)
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = -1;

    /// <summary>
    /// Enable parallel analysis of script files.
    /// When enabled, independent files are analyzed concurrently.
    /// Default: true
    /// </summary>
    public bool EnableParallelAnalysis { get; set; } = true;

    /// <summary>
    /// Batch size for parallel processing.
    /// Controls how many files to batch together.
    /// Smaller = more fine-grained, more overhead.
    /// Larger = less overhead, less responsive cancellation.
    /// Default: 10
    /// </summary>
    public int ParallelBatchSize { get; set; } = 10;

    /// <summary>
    /// Enable incremental analysis (only re-analyze changed files).
    /// Requires file watching to be enabled.
    /// Default: true
    /// </summary>
    public bool EnableIncrementalAnalysis { get; set; } = true;

    /// <summary>
    /// Enable incremental parsing (member-level reparsing).
    /// When disabled, full file reparse is used for all changes.
    /// Default: true
    /// </summary>
    public bool EnableIncrementalParsing { get; set; } = true;

    /// <summary>
    /// Debounce interval for file change processing (milliseconds).
    /// Rapid file changes within this interval are coalesced.
    /// Default: 300
    /// </summary>
    public int FileChangeDebounceMs { get; set; } = 300;

    /// <summary>
    /// Threshold for triggering full reparse instead of incremental.
    /// If the ratio of changed characters to file size exceeds this value,
    /// full reparse is used. Value from 0.0 to 1.0 (0.5 = 50%).
    /// Default: 0.5
    /// </summary>
    public double IncrementalFullReparseThreshold { get; set; } = 0.5;

    /// <summary>
    /// Maximum number of class members affected by changes
    /// before triggering full reparse instead of incremental.
    /// Default: 3
    /// </summary>
    public int IncrementalMaxAffectedMembers { get; set; } = 3;
}
