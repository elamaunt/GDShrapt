namespace GDShrapt.Semantics.Incremental.Results;

/// <summary>
/// Result of incremental analysis.
/// </summary>
public class GDIncrementalAnalysisResult
{
    /// <summary>
    /// Whether the analysis completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Files that were analyzed (not from cache).
    /// </summary>
    public List<string> AnalyzedFiles { get; set; } = new();

    /// <summary>
    /// Files that used cached results.
    /// </summary>
    public List<string> CachedFiles { get; set; } = new();

    /// <summary>
    /// Files that timed out during analysis.
    /// </summary>
    public List<string> TimedOutFiles { get; set; } = new();

    /// <summary>
    /// All diagnostics from the analysis.
    /// </summary>
    public List<GDFileDiagnostics> Diagnostics { get; set; } = new();

    /// <summary>
    /// Total files processed (analyzed + cached).
    /// </summary>
    public int TotalFiles => AnalyzedFiles.Count + CachedFiles.Count;

    /// <summary>
    /// Total diagnostics count.
    /// </summary>
    public int TotalDiagnostics => Diagnostics.Sum(d => d.Items.Count);

    /// <summary>
    /// Total error count.
    /// </summary>
    public int TotalErrors => Diagnostics.Sum(d => d.Items.Count(i => i.Severity == 0));

    /// <summary>
    /// Total warning count.
    /// </summary>
    public int TotalWarnings => Diagnostics.Sum(d => d.Items.Count(i => i.Severity == 1));

    /// <summary>
    /// Time taken for analysis.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GDIncrementalAnalysisResult Succeeded()
    {
        return new GDIncrementalAnalysisResult { Success = true };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GDIncrementalAnalysisResult Failed(string errorMessage)
    {
        return new GDIncrementalAnalysisResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Diagnostics for a single file.
/// </summary>
public class GDFileDiagnostics
{
    /// <summary>
    /// File path.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Diagnostics for this file.
    /// </summary>
    public List<GDSerializedDiagnostic> Items { get; set; } = new();

    /// <summary>
    /// Whether this file's diagnostics came from cache.
    /// </summary>
    public bool FromCache { get; set; }
}
