using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    private static readonly Regex FeaturesRegex = new Regex(
        @"config/features\s*=\s*PackedStringArray\(""(\d+\.\d+)""",
        RegexOptions.Compiled);

    private static readonly Regex UidInHeaderRegex = new Regex(
        @"uid=""(uid://[a-y0-8]+)""",
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

        // Resolve uid:// paths if any autoloads use them
        ResolveUidPaths(autoloads, projectGodotPath, fs);

        return autoloads;
    }

    /// <summary>
    /// Parses the Godot engine version from the project.godot config/features field.
    /// Returns null if version cannot be determined.
    /// </summary>
    public static Version? ParseGodotVersion(string projectGodotPath, IGDFileSystem? fileSystem = null)
    {
        var fs = fileSystem ?? new GDDefaultFileSystem();

        if (!fs.FileExists(projectGodotPath))
            return null;

        try
        {
            var content = fs.ReadAllText(projectGodotPath);
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            bool inApplicationSection = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("["))
                {
                    inApplicationSection = line.Equals("[application]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inApplicationSection)
                    continue;

                var match = FeaturesRegex.Match(line);
                if (match.Success)
                {
                    if (Version.TryParse(match.Groups[1].Value, out var version))
                        return version;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static void ResolveUidPaths(List<GDAutoloadEntry> autoloads, string projectGodotPath, IGDFileSystem fs)
    {
        var neededUids = new HashSet<string>(
            autoloads.Where(a => a.Path.StartsWith("uid://")).Select(a => a.Path));

        if (neededUids.Count == 0)
            return;

        var projectDir = Path.GetDirectoryName(projectGodotPath);
        if (string.IsNullOrEmpty(projectDir))
            return;

        var uidMap = BuildUidMap(projectDir, fs, neededUids);

        for (int i = 0; i < autoloads.Count; i++)
        {
            if (autoloads[i].Path.StartsWith("uid://")
                && uidMap.TryGetValue(autoloads[i].Path, out var resolved))
            {
                autoloads[i] = new GDAutoloadEntry
                {
                    Name = autoloads[i].Name,
                    Path = resolved,
                    Enabled = autoloads[i].Enabled
                };
            }
        }
    }

    private static Dictionary<string, string> BuildUidMap(
        string projectDir, IGDFileSystem fs, HashSet<string> neededUids)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            // 1. Scan .uid sidecar files (Godot 4.4+)
            foreach (var uidFile in fs.GetFiles(projectDir, "*.uid", true))
            {
                if (neededUids.Count == 0)
                    break;

                try
                {
                    var content = fs.ReadAllText(uidFile).Trim();
                    if (content.StartsWith("uid://") && neededUids.Contains(content))
                    {
                        // Strip .uid suffix to get the resource path
                        var resourceFullPath = uidFile.Substring(0, uidFile.Length - 4);
                        var resPath = GetRelativeResPath(projectDir, resourceFullPath);
                        if (resPath != null)
                        {
                            map[content] = resPath;
                            neededUids.Remove(content);
                        }
                    }
                }
                catch { }
            }

            // 2. Scan .tscn/.tres headers for uid (if any UIDs still unresolved)
            if (neededUids.Count > 0)
            {
                IEnumerable<string> sceneFiles;
                try { sceneFiles = fs.GetFiles(projectDir, "*.tscn", true); }
                catch { sceneFiles = Enumerable.Empty<string>(); }

                IEnumerable<string> resFiles;
                try { resFiles = fs.GetFiles(projectDir, "*.tres", true); }
                catch { resFiles = Enumerable.Empty<string>(); }

                foreach (var file in sceneFiles.Concat(resFiles))
                {
                    if (neededUids.Count == 0)
                        break;

                    try
                    {
                        var fileContent = fs.ReadAllText(file);
                        var firstLineEnd = fileContent.IndexOf('\n');
                        var firstLine = firstLineEnd >= 0
                            ? fileContent.Substring(0, firstLineEnd).Trim()
                            : fileContent.Trim();

                        var uidMatch = UidInHeaderRegex.Match(firstLine);
                        if (uidMatch.Success)
                        {
                            var uid = uidMatch.Groups[1].Value;
                            if (neededUids.Contains(uid) && !map.ContainsKey(uid))
                            {
                                var resPath = GetRelativeResPath(projectDir, file);
                                if (resPath != null)
                                {
                                    map[uid] = resPath;
                                    neededUids.Remove(uid);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return map;
    }

    private static string? GetRelativeResPath(string projectDir, string fullPath)
    {
        var normalizedDir = projectDir.Replace('\\', '/').TrimEnd('/') + '/';
        var normalizedPath = fullPath.Replace('\\', '/');
        if (normalizedPath.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
            return "res://" + normalizedPath.Substring(normalizedDir.Length);
        return null;
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

    /// <summary>
    /// Whether this autoload is a C# script.
    /// </summary>
    public bool IsCSharp => Extension == ".cs";
}
