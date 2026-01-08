using GDShrapt.Abstractions;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Implementation of IGDProjectContext for the Godot editor environment.
/// Uses Godot.ProjectSettings for path resolution.
/// </summary>
internal class GodotEditorProjectContext : IGDProjectContext
{
    private readonly string _projectPath;

    /// <summary>
    /// Gets the root path of the project (equivalent to res://).
    /// </summary>
    public string ProjectPath => _projectPath;

    /// <summary>
    /// Gets the file system abstraction for this project context.
    /// </summary>
    public IGDFileSystem FileSystem => GDDefaultFileSystem.Instance;

    public GodotEditorProjectContext()
    {
        _projectPath = Godot.ProjectSettings.GlobalizePath("res://");
    }

    /// <summary>
    /// Converts a resource path (res://...) to an absolute filesystem path.
    /// </summary>
    public string GlobalizePath(string resourcePath)
    {
        return Godot.ProjectSettings.GlobalizePath(resourcePath);
    }

    /// <summary>
    /// Converts an absolute filesystem path to a resource path (res://...).
    /// </summary>
    public string LocalizePath(string absolutePath)
    {
        var normalized = absolutePath.Replace('\\', '/');
        var projectNormalized = _projectPath.Replace('\\', '/').TrimEnd('/');
        
        if (normalized.StartsWith(projectNormalized, StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + normalized.Substring(projectNormalized.Length).TrimStart('/');
        }
        
        return absolutePath;
    }
}
