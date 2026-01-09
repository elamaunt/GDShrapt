using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

/// <summary>
/// Parser for Godot's project.godot configuration file.
/// Extracts project settings like autoloads.
/// </summary>
public static class GDGodotProjectParser
{
    private static readonly Regex AutoloadEntryRegex = new Regex(
        @"^(\w+)\s*=\s*""(\*?)(.+)""$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses autoload entries from project.godot file.
    /// </summary>
    /// <param name="projectGodotPath">Full path to project.godot file.</param>
    /// <param name="fileSystem">File system abstraction (optional, uses default if null).</param>
    /// <returns>List of autoload entries.</returns>
    public static IReadOnlyList<GDAutoloadEntry> ParseAutoloads(string projectGodotPath, IGDFileSystem? fileSystem = null)
    {
        var fs = fileSystem ?? new GDDefaultFileSystem();
        var autoloads = new List<GDAutoloadEntry>();

        if (!fs.FileExists(projectGodotPath))
        {
            return autoloads;
        }

        try
        {
            var content = fs.ReadAllText(projectGodotPath);
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            bool inAutoloadSection = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Check for section headers
                if (line.StartsWith("["))
                {
                    inAutoloadSection = line.Equals("[autoload]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                // Skip if not in autoload section
                if (!inAutoloadSection)
                    continue;

                // Parse autoload entry: Name="*res://path/to/script.gd"
                var match = AutoloadEntryRegex.Match(line);
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    var enabled = match.Groups[2].Value == "*";
                    var path = match.Groups[3].Value;

                    autoloads.Add(new GDAutoloadEntry
                    {
                        Name = name,
                        Path = path,
                        Enabled = enabled
                    });
                }
            }
        }
        catch
        {
            // Return empty list on parse errors
        }

        return autoloads;
    }

    /// <summary>
    /// Finds the project.godot file starting from a directory.
    /// </summary>
    /// <param name="startPath">Starting path to search from.</param>
    /// <param name="fileSystem">File system abstraction (optional).</param>
    /// <returns>Full path to project.godot or null if not found.</returns>
    public static string? FindProjectGodot(string startPath, IGDFileSystem? fileSystem = null)
    {
        var fs = fileSystem ?? new GDDefaultFileSystem();
        var current = Path.GetFullPath(startPath);

        // If it's a file, start from its directory
        if (fs.FileExists(current))
        {
            current = Path.GetDirectoryName(current)!;
        }

        while (!string.IsNullOrEmpty(current))
        {
            var projectFile = Path.Combine(current, "project.godot");
            if (fs.FileExists(projectFile))
            {
                return projectFile;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent == current)
                break;

            current = parent;
        }

        return null;
    }
}

/// <summary>
/// Represents an autoload entry from project.godot.
/// </summary>
public class GDAutoloadEntry
{
    /// <summary>
    /// The autoload name (used as singleton accessor in GDScript).
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The resource path (e.g., "res://scripts/global.gd").
    /// </summary>
    public string Path { get; init; } = "";

    /// <summary>
    /// Whether the autoload is enabled (* prefix in project.godot).
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the file extension of the autoload path.
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(Path).ToLowerInvariant();

    /// <summary>
    /// Whether this autoload is a GDScript file.
    /// </summary>
    public bool IsScript => Extension == ".gd";

    /// <summary>
    /// Whether this autoload is a scene file.
    /// </summary>
    public bool IsScene => Extension == ".tscn" || Extension == ".scn";
}
