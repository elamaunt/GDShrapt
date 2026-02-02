namespace GDShrapt.Abstractions;

/// <summary>
/// Handler interface for type coverage analysis.
/// </summary>
public interface IGDTypeCoverageHandler
{
    /// <summary>
    /// Analyzes type coverage for a single file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Type coverage report for the file.</returns>
    GDTypeCoverageReport AnalyzeFile(string filePath);

    /// <summary>
    /// Analyzes type coverage across the entire project.
    /// </summary>
    /// <returns>Type coverage report for the project.</returns>
    GDTypeCoverageReport AnalyzeProject();
}
