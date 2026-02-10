using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using SemanticProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Helper for loading GDScript projects.
/// This is a thin wrapper over GDProjectLoader from Semantics for backwards compatibility.
/// </summary>
public static class GDProjectLoader
{
    /// <summary>
    /// Loads a project from the specified path.
    /// </summary>
    /// <param name="projectPath">Path to the project directory (should contain project.godot).</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Loaded and analyzed project.</returns>
    public static GDScriptProject LoadProject(string projectPath, IGDLogger? logger = null)
    {
        return SemanticProjectLoader.LoadProject(projectPath, logger);
    }

    /// <summary>
    /// Loads a project with custom options.
    /// </summary>
    public static GDScriptProject LoadProject(string projectPath, GDScriptProjectOptions options)
    {
        return SemanticProjectLoader.LoadProject(projectPath, options);
    }

    /// <summary>
    /// Finds the project root by looking for project.godot file.
    /// </summary>
    /// <param name="startPath">Starting path to search from.</param>
    /// <returns>Project root path or null if not found.</returns>
    public static string? FindProjectRoot(string startPath)
    {
        return SemanticProjectLoader.FindProjectRoot(startPath);
    }
}
