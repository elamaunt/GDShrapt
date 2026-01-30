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
}
