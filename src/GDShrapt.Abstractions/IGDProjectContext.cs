namespace GDShrapt.Abstractions;

/// <summary>
/// Abstraction for Godot project context.
/// Provides path resolution and file system access.
/// </summary>
public interface IGDProjectContext
{
    /// <summary>
    /// Gets the root path of the project (equivalent to res://).
    /// This is the absolute filesystem path to the project directory.
    /// </summary>
    string ProjectPath { get; }

    /// <summary>
    /// Converts a resource path (res://...) to an absolute filesystem path.
    /// </summary>
    /// <param name="resourcePath">Resource path starting with "res://".</param>
    /// <returns>Absolute filesystem path.</returns>
    string GlobalizePath(string resourcePath);

    /// <summary>
    /// Converts an absolute filesystem path to a resource path (res://...).
    /// </summary>
    /// <param name="absolutePath">Absolute filesystem path.</param>
    /// <returns>Resource path starting with "res://".</returns>
    string LocalizePath(string absolutePath);

    /// <summary>
    /// Gets the file system abstraction for this project context.
    /// </summary>
    IGDFileSystem FileSystem { get; }
}
