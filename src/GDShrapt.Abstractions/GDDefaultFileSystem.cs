using System;
using System.Collections.Generic;
using System.IO;

namespace GDShrapt.Abstractions;

/// <summary>
/// Default implementation of <see cref="IGDFileSystem"/> using System.IO.
/// </summary>
public class GDDefaultFileSystem : IGDFileSystem
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GDDefaultFileSystem Instance { get; } = new GDDefaultFileSystem();

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(directory, pattern, searchOption);
    }

    public IEnumerable<string> GetDirectories(string directory)
    {
        return Directory.GetDirectories(directory);
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public string GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public string GetExtension(string path)
    {
        return Path.GetExtension(path);
    }
}
