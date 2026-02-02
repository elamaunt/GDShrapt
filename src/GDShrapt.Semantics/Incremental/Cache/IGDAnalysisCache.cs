using GDShrapt.Semantics.Incremental.Results;

namespace GDShrapt.Semantics.Incremental.Cache;

/// <summary>
/// Interface for caching analysis results.
/// </summary>
public interface IGDAnalysisCache
{
    /// <summary>
    /// Tries to get a cached entry.
    /// </summary>
    bool TryGet(GDCacheKey key, out GDCachedAnalysisEntry? entry);

    /// <summary>
    /// Sets a cached entry.
    /// </summary>
    void Set(GDCacheKey key, GDCachedAnalysisEntry entry);

    /// <summary>
    /// Removes a cached entry.
    /// </summary>
    void Remove(GDCacheKey key);

    /// <summary>
    /// Removes all entries for a given file path (any hash).
    /// </summary>
    void RemoveByFilePath(string filePath);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets all cached file paths.
    /// </summary>
    IEnumerable<string> GetCachedFilePaths();
}
