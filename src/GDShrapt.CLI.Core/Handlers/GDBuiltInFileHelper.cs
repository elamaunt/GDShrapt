using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

public static class GDBuiltInFileHelper
{
    public static readonly string BuiltInTypesDir =
        Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");

    private static readonly ConcurrentDictionary<string, (string Hash, GDScriptFile File)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsBuiltInTypeFile(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var builtInDir = Path.GetFullPath(BuiltInTypesDir);
            return fullPath.StartsWith(builtInDir, StringComparison.OrdinalIgnoreCase)
                && fullPath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string ExtractTypeName(string filePath)
        => Path.GetFileNameWithoutExtension(filePath);

    public static GDScriptFile? GetOrParse(string filePath, IGDRuntimeProvider? runtimeProvider)
    {
        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch
        {
            return null;
        }

        var hash = ComputeHash(content);

        if (_cache.TryGetValue(filePath, out var cached) && cached.Hash == hash)
            return cached.File;

        var reference = new GDScriptReference(filePath);
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(content);

        if (scriptFile.Class == null)
            return null;

        scriptFile.Analyze(runtimeProvider);

        _cache[filePath] = (hash, scriptFile);
        return scriptFile;
    }

    private static string ComputeHash(string content)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(bytes);
    }
}
