namespace GDShrapt.Abstractions;

/// <summary>
/// Handler interface for dependency analysis.
/// </summary>
public interface IGDDependencyHandler
{
    /// <summary>
    /// Analyzes dependencies for a single file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Dependency information for the file.</returns>
    GDFileDependencyInfo AnalyzeFile(string filePath);

    /// <summary>
    /// Analyzes dependencies across the entire project.
    /// </summary>
    /// <returns>Dependency report for the project.</returns>
    GDProjectDependencyReport AnalyzeProject();
}
