namespace GDShrapt.Abstractions;

/// <summary>
/// Handler interface for code metrics analysis.
/// </summary>
public interface IGDMetricsHandler
{
    /// <summary>
    /// Analyzes metrics for a single file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Metrics for the file.</returns>
    GDFileMetrics AnalyzeFile(string filePath);

    /// <summary>
    /// Analyzes metrics for all files in the project.
    /// </summary>
    /// <returns>Aggregated project metrics.</returns>
    GDProjectMetrics AnalyzeProject();
}
