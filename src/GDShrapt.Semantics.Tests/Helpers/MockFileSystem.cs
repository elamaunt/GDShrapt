using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Mock file system for testing file-dependent components.
/// </summary>
internal class MockFileSystem : IGDFileSystem
{
    private readonly Dictionary<string, string> _files = new();

    public void AddFile(string path, string content)
    {
        var normalizedPath = NormalizePath(path);
        _files[normalizedPath] = content;
    }

    public void SetFileContent(string path, string content) => AddFile(path, content);

    private string NormalizePath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
    public bool DirectoryExists(string path) => true;
    public string ReadAllText(string path) => _files[NormalizePath(path)];
    public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive) =>
        _files.Keys.Where(k => k.EndsWith(pattern.TrimStart('*')));
    public IEnumerable<string> GetDirectories(string directory) => new string[0];
    public string GetFullPath(string path) => NormalizePath(path);
    public string CombinePath(params string[] paths) => Path.Combine(paths);
    public string GetFileName(string path) => Path.GetFileName(path);
    public string GetFileNameWithoutExtension(string path) =>
        Path.GetFileNameWithoutExtension(path);
    public string? GetDirectoryName(string path) =>
        Path.GetDirectoryName(path);
    public string GetExtension(string path) =>
        Path.GetExtension(path);
}
