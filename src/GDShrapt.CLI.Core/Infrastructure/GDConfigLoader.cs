using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Loads GDShrapt configuration from .gdshrapt.json files.
/// </summary>
public static class GDConfigLoader
{
    /// <summary>
    /// Default configuration file name.
    /// </summary>
    public const string ConfigFileName = ".gdshrapt.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// Loads configuration from the project directory.
    /// Returns default config if file doesn't exist.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <returns>Loaded or default configuration.</returns>
    public static GDProjectConfig LoadConfig(string projectPath)
    {
        var configPath = Path.Combine(projectPath, ConfigFileName);

        if (!File.Exists(configPath))
        {
            return new GDProjectConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<GDProjectConfig>(json, JsonOptions) ?? new GDProjectConfig();
        }
        catch (JsonException)
        {
            // Return default config if JSON is invalid
            return new GDProjectConfig();
        }
    }

    /// <summary>
    /// Saves configuration to the project directory.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="config">Configuration to save.</param>
    public static void SaveConfig(string projectPath, GDProjectConfig config)
    {
        var configPath = Path.Combine(projectPath, ConfigFileName);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Checks if a file path matches any of the exclude patterns.
    /// </summary>
    /// <param name="relativePath">Relative path from project root.</param>
    /// <param name="excludePatterns">List of glob patterns to exclude.</param>
    /// <returns>True if the file should be excluded.</returns>
    public static bool ShouldExclude(string relativePath, System.Collections.Generic.IEnumerable<string> excludePatterns)
    {
        // Normalize path separators
        var normalizedPath = relativePath.Replace('\\', '/');

        foreach (var pattern in excludePatterns)
        {
            if (MatchesGlobPattern(normalizedPath, pattern.Replace('\\', '/')))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob matching for common patterns
        // Supports: **, *, ?

        if (pattern == "**")
            return true;

        if (pattern.StartsWith("**/"))
        {
            // Match any path containing this suffix
            var suffix = pattern.Substring(3);
            return path.Contains("/" + suffix) || path.StartsWith(suffix) || path.EndsWith("/" + suffix.TrimEnd('/'));
        }

        if (pattern.EndsWith("/**"))
        {
            // Match any path starting with this prefix
            var prefix = pattern.Substring(0, pattern.Length - 3);
            return path.StartsWith(prefix + "/") || path == prefix;
        }

        if (pattern.Contains("**"))
        {
            // Split by ** and match prefix/suffix
            var parts = pattern.Split(new[] { "**" }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return path.StartsWith(parts[0]) && path.EndsWith(parts[1]);
            }
        }

        // Simple wildcard matching
        if (pattern.Contains("*") || pattern.Contains("?"))
        {
            return WildcardMatch(path, pattern);
        }

        // Exact match
        return path == pattern || path.StartsWith(pattern + "/");
    }

    private static bool WildcardMatch(string input, string pattern)
    {
        int inputIndex = 0, patternIndex = 0;
        int inputMark = -1, patternMark = -1;

        while (inputIndex < input.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == input[inputIndex]))
            {
                inputIndex++;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternMark = patternIndex++;
                inputMark = inputIndex;
            }
            else if (patternMark != -1)
            {
                patternIndex = patternMark + 1;
                inputIndex = ++inputMark;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
