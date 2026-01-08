using System;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a reference to a GDScript file.
/// Handles path normalization and comparison.
/// </summary>
public class GDScriptReference : IEquatable<GDScriptReference>
{
    private readonly string _fullPath;
    private readonly IGDProjectContext? _context;

    /// <summary>
    /// Creates a script reference from a full filesystem path.
    /// </summary>
    public GDScriptReference(string fullPath)
    {
        _fullPath = NormalizePath(fullPath);
    }

    /// <summary>
    /// Creates a script reference with project context for resource path resolution.
    /// </summary>
    public GDScriptReference(string fullPath, IGDProjectContext context)
    {
        _fullPath = NormalizePath(fullPath);
        _context = context;
    }

    /// <summary>
    /// Gets the full filesystem path to the script.
    /// </summary>
    public string FullPath => _fullPath;

    /// <summary>
    /// Gets the resource path (res://...) for this script.
    /// Returns null if no project context is available or path is outside project.
    /// </summary>
    public string? ResourcePath
    {
        get
        {
            if (string.IsNullOrEmpty(_fullPath))
                return null;

            if (_context != null)
            {
                return _context.LocalizePath(_fullPath);
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(_fullPath);

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(_fullPath);

    public override int GetHashCode()
    {
        return _fullPath?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GDScriptReference);
    }

    public bool Equals(GDScriptReference? other)
    {
        if (other is null)
            return false;

        return string.Equals(_fullPath, other._fullPath, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return ResourcePath ?? _fullPath ?? "(empty)";
    }

    public static bool operator ==(GDScriptReference? left, GDScriptReference? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(GDScriptReference? left, GDScriptReference? right)
    {
        return !(left == right);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path.Replace('\\', '/').TrimEnd('/');
    }
}
