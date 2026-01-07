using System;
using System.IO;

namespace GDShrapt.Abstractions;

/// <summary>
/// Default implementation of <see cref="IGDProjectContext"/> for standalone usage.
/// </summary>
public class GDDefaultProjectContext : IGDProjectContext
{
    private readonly string _projectPath;
    private readonly IGDFileSystem _fileSystem;

    /// <summary>
    /// Creates a new project context with the specified project path.
    /// </summary>
    /// <param name="projectPath">The root path of the Godot project.</param>
    /// <param name="fileSystem">Optional file system implementation. Uses <see cref="GDDefaultFileSystem"/> if not provided.</param>
    public GDDefaultProjectContext(string projectPath, IGDFileSystem? fileSystem = null)
    {
        if (string.IsNullOrEmpty(projectPath))
            throw new ArgumentNullException(nameof(projectPath));

        _projectPath = NormalizePath(projectPath);
        _fileSystem = fileSystem ?? GDDefaultFileSystem.Instance;
    }

    /// <inheritdoc/>
    public string ProjectPath => _projectPath;

    /// <inheritdoc/>
    public IGDFileSystem FileSystem => _fileSystem;

    /// <inheritdoc/>
    public string GlobalizePath(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return _projectPath;

        if (resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = resourcePath.Substring(6); // Remove "res://"
            return NormalizePath(Path.Combine(_projectPath, relativePath));
        }

        // If it's already an absolute path, return as-is
        if (Path.IsPathRooted(resourcePath))
            return NormalizePath(resourcePath);

        // Treat as relative to project
        return NormalizePath(Path.Combine(_projectPath, resourcePath));
    }

    /// <inheritdoc/>
    public string LocalizePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return "res://";

        var normalizedPath = NormalizePath(absolutePath);
        var normalizedProjectPath = NormalizePath(_projectPath);

        if (normalizedPath.StartsWith(normalizedProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = normalizedPath.Substring(normalizedProjectPath.Length).TrimStart('/');
            return "res://" + relativePath;
        }

        // Path is outside project, return as-is
        return absolutePath;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }
}
