namespace GDShrapt.Abstractions;

/// <summary>
/// Handler interface for dead code analysis.
/// Base implementation uses only Strict confidence.
/// Pro implementation allows higher confidence levels.
/// </summary>
public interface IGDDeadCodeHandler
{
    /// <summary>
    /// Analyzes dead code in a single file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="options">Analysis options.</param>
    /// <returns>Dead code report for the file.</returns>
    GDDeadCodeReport AnalyzeFile(string filePath, GDDeadCodeOptions options);

    /// <summary>
    /// Analyzes dead code across the entire project.
    /// </summary>
    /// <param name="options">Analysis options.</param>
    /// <returns>Dead code report for the project.</returns>
    GDDeadCodeReport AnalyzeProject(GDDeadCodeOptions options);
}
