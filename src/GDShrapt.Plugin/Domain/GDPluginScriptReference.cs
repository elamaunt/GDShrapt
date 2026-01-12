using GDShrapt.Semantics;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Plugin-specific wrapper over GDScriptReference that adds Godot-dependent ResourcePath resolution.
/// This allows the Plugin to use Godot.ProjectSettings for path resolution while the core
/// GDScriptReference remains Godot-independent.
/// </summary>
internal class GDPluginScriptReference : IEquatable<GDPluginScriptReference>
{
    private readonly GDScriptReference _inner;

    /// <summary>
    /// Creates a script reference from a full filesystem path.
    /// </summary>
    public GDPluginScriptReference(string fullPath)
    {
        _inner = new GDScriptReference(fullPath);
    }

    /// <summary>
    /// Creates a plugin script reference wrapping an existing GDScriptReference.
    /// </summary>
    public GDPluginScriptReference(GDScriptReference reference)
    {
        _inner = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// Gets the underlying GDScriptReference from Semantics.
    /// </summary>
    public GDScriptReference Inner => _inner;

    /// <summary>
    /// Gets the full filesystem path to the script.
    /// </summary>
    public string FullPath => _inner.FullPath;

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName => _inner.FileName;

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    public string FileNameWithoutExtension => _inner.FileNameWithoutExtension;

    /// <summary>
    /// Gets the resource path (res://...) for this script using Godot's ProjectSettings.
    /// Returns null if the path is outside the project or empty.
    /// </summary>
    public string? ResourcePath
    {
        get
        {
            if (string.IsNullOrEmpty(FullPath))
                return null;

            try
            {
                var projectPath = Godot.ProjectSettings.GlobalizePath("res://")
                    .Replace('\\', '/').TrimEnd('/');
                var normalizedPath = FullPath.Replace('\\', '/');

                if (normalizedPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "res://" + normalizedPath.Substring(projectPath.Length).TrimStart('/');
                }
            }
            catch
            {
                // Godot not available or path resolution failed
            }

            return null;
        }
    }

    public override int GetHashCode()
    {
        return _inner.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GDPluginScriptReference);
    }

    public bool Equals(GDPluginScriptReference? other)
    {
        if (other is null)
            return false;

        return _inner.Equals(other._inner);
    }

    public override string ToString()
    {
        return ResourcePath ?? FullPath ?? "(empty)";
    }

    public static bool operator ==(GDPluginScriptReference? left, GDPluginScriptReference? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(GDPluginScriptReference? left, GDPluginScriptReference? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Implicit conversion from string path.
    /// </summary>
    public static implicit operator GDPluginScriptReference(string fullPath)
    {
        return new GDPluginScriptReference(fullPath);
    }
}
