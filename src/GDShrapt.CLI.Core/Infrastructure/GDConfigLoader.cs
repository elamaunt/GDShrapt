using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Loads GDShrapt configuration from .gdshrapt.json files.
/// This is a thin wrapper over GDConfigManager from Semantics for backwards compatibility.
/// </summary>
public static class GDConfigLoader
{
    /// <summary>
    /// Default configuration file name.
    /// </summary>
    public const string ConfigFileName = GDConfigManager.ConfigFileName;

    /// <summary>
    /// Loads configuration from the project directory.
    /// Returns default config if file doesn't exist.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <returns>Loaded or default configuration.</returns>
    public static GDProjectConfig LoadConfig(string projectPath)
    {
        return GDConfigManager.LoadConfigStatic(projectPath);
    }

    /// <summary>
    /// Saves configuration to the project directory.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="config">Configuration to save.</param>
    public static void SaveConfig(string projectPath, GDProjectConfig config)
    {
        GDConfigManager.SaveConfigStatic(projectPath, config);
    }

    /// <summary>
    /// Checks if a file path matches any of the exclude patterns.
    /// </summary>
    /// <param name="relativePath">Relative path from project root.</param>
    /// <param name="excludePatterns">List of glob patterns to exclude.</param>
    /// <returns>True if the file should be excluded.</returns>
    public static bool ShouldExclude(string relativePath, IEnumerable<string> excludePatterns)
    {
        return GDConfigManager.ShouldExclude(relativePath, excludePatterns);
    }
}
