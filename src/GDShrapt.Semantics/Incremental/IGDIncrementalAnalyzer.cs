using GDShrapt.Semantics.Incremental.Results;
using GDShrapt.Semantics.Incremental.Tracking;

namespace GDShrapt.Semantics.Incremental;

/// <summary>
/// Interface for incremental analysis of GDScript projects.
/// Only analyzes changed files and their dependents.
/// </summary>
public interface IGDIncrementalAnalyzer
{
    /// <summary>
    /// Analyzes the project incrementally, only processing changed files.
    /// </summary>
    /// <param name="project">The GDScript project to analyze.</param>
    /// <param name="config">Configuration for incremental analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result with diagnostics from all files.</returns>
    GDIncrementalAnalysisResult Analyze(
        GDScriptProject project,
        GDIncrementalConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current incremental analysis state.
    /// </summary>
    GDIncrementalState GetState();

    /// <summary>
    /// Invalidates specific files in the cache, forcing re-analysis.
    /// </summary>
    /// <param name="filePaths">Paths of files to invalidate.</param>
    void Invalidate(IEnumerable<string> filePaths);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Saves the current incremental state to disk.
    /// </summary>
    /// <param name="directory">Directory to save state to.</param>
    Task SaveStateAsync(string directory);

    /// <summary>
    /// Loads incremental state from disk.
    /// </summary>
    /// <param name="directory">Directory to load state from.</param>
    /// <returns>True if state was loaded successfully.</returns>
    Task<bool> LoadStateAsync(string directory);
}
