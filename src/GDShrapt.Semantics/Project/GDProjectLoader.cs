using System.IO;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Helper for loading GDScript projects.
/// Used by CLI, LSP, and Plugin.
/// </summary>
public static class GDProjectLoader
{
    /// <summary>
    /// Loads a project from the specified path.
    /// </summary>
    /// <param name="projectPath">Path to the project directory (should contain project.godot).</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="enableSceneTypes">Enable scene types provider for autoloads.</param>
    /// <returns>Loaded and analyzed project.</returns>
    public static GDScriptProject LoadProject(string projectPath, IGDSemanticLogger? logger = null, bool enableSceneTypes = true)
    {
        var fullPath = Path.GetFullPath(projectPath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {fullPath}");
        }

        var context = new GDDefaultProjectContext(fullPath);
        var options = new GDScriptProjectOptions
        {
            Logger = logger,
            EnableSceneTypesProvider = enableSceneTypes
        };

        var project = new GDScriptProject(context, options);
        project.LoadScripts();
        project.LoadScenes();
        project.AnalyzeAll();

        return project;
    }

    /// <summary>
    /// Loads a project without analyzing (useful for lazy loading).
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Loaded but not analyzed project.</returns>
    public static GDScriptProject LoadProjectWithoutAnalysis(string projectPath, IGDSemanticLogger? logger = null)
    {
        var fullPath = Path.GetFullPath(projectPath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {fullPath}");
        }

        var context = new GDDefaultProjectContext(fullPath);
        var options = new GDScriptProjectOptions
        {
            Logger = logger,
            EnableSceneTypesProvider = true
        };

        var project = new GDScriptProject(context, options);
        project.LoadScripts();
        project.LoadScenes();

        return project;
    }

    /// <summary>
    /// Finds the project root by looking for project.godot file.
    /// </summary>
    /// <param name="startPath">Starting path to search from (file or directory).</param>
    /// <returns>Project root path or null if not found.</returns>
    public static string? FindProjectRoot(string startPath)
    {
        var current = Path.GetFullPath(startPath);

        // If it's a file, start from its directory
        if (File.Exists(current))
        {
            current = Path.GetDirectoryName(current)!;
        }

        while (!string.IsNullOrEmpty(current))
        {
            var projectFile = Path.Combine(current, "project.godot");
            if (File.Exists(projectFile))
            {
                return current;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent == current)
                break;

            current = parent;
        }

        return null;
    }

    /// <summary>
    /// Checks if a directory is a valid Godot project root.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if the directory contains project.godot.</returns>
    public static bool IsProjectRoot(string path)
    {
        if (!Directory.Exists(path))
            return false;

        return File.Exists(Path.Combine(path, "project.godot"));
    }
}
