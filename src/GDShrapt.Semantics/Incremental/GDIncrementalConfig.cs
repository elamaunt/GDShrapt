namespace GDShrapt.Semantics.Incremental;

/// <summary>
/// Configuration for incremental analysis.
/// </summary>
public class GDIncrementalConfig
{
    /// <summary>
    /// Whether incremental analysis is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to persist the cache to disk.
    /// </summary>
    public bool PersistCache { get; set; } = true;

    /// <summary>
    /// Directory for cache storage. If null, uses default location.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Maximum number of parallel analysis tasks.
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Timeout in seconds for analyzing a single file.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum cache size in megabytes. 0 means unlimited.
    /// </summary>
    public int MaxCacheSizeMb { get; set; } = 100;

    /// <summary>
    /// Gets the effective cache directory.
    /// </summary>
    public string GetEffectiveCacheDirectory(string projectPath)
    {
        if (!string.IsNullOrEmpty(CacheDirectory))
            return CacheDirectory;

        return Path.Combine(projectPath, ".gdshrapt", "cache");
    }
}
