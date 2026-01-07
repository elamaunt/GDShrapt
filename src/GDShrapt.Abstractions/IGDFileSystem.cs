using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Abstraction for file system operations.
/// Allows semantic analysis to work without direct file system access.
/// </summary>
public interface IGDFileSystem
{
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Gets files matching a pattern in a directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="pattern">The search pattern (e.g., "*.gd").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    IEnumerable<string> GetFiles(string directory, string pattern, bool recursive);

    /// <summary>
    /// Gets subdirectories in a directory.
    /// </summary>
    IEnumerable<string> GetDirectories(string directory);

    /// <summary>
    /// Gets the full (absolute) path for a path.
    /// </summary>
    string GetFullPath(string path);

    /// <summary>
    /// Combines path segments into a single path.
    /// </summary>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Gets the file name from a path.
    /// </summary>
    string GetFileName(string path);

    /// <summary>
    /// Gets the file name without extension from a path.
    /// </summary>
    string GetFileNameWithoutExtension(string path);

    /// <summary>
    /// Gets the directory name from a path.
    /// </summary>
    string? GetDirectoryName(string path);

    /// <summary>
    /// Gets the extension from a path.
    /// </summary>
    string GetExtension(string path);
}
