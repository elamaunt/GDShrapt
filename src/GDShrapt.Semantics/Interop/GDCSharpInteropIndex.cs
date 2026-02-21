using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Lightweight index of C# interop surfaces in a Godot project.
/// Built from existing data sources (file system, autoloads, scene bindings).
/// Consumers: dead code, rename, find-refs.
/// </summary>
public class GDCSharpInteropIndex
{
    private readonly bool _hasCSharpCode;
    private readonly List<string> _csharpScriptPaths;
    private readonly List<string> _csharpAutoloadNames;
    private readonly int _csharpSceneBindingCount;

    internal GDCSharpInteropIndex(GDScriptProject project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        // 1. Detect .cs files in project via file system
        try
        {
            _csharpScriptPaths = project.FileSystem
                .GetFiles(project.ProjectPath, "*.cs", true)
                .ToList();
        }
        catch
        {
            _csharpScriptPaths = new List<string>();
        }

        _hasCSharpCode = _csharpScriptPaths.Count > 0;

        // 2. C# autoloads (reuse GDAutoloadEntry.IsCSharp)
        _csharpAutoloadNames = project.AutoloadEntries
            .Where(a => a.IsCSharp)
            .Select(a => a.Name)
            .ToList();

        // 3. C# scene bindings (reuse GDNodeTypeInfo.IsCSharpScript)
        var sp = project.SceneTypesProvider;
        _csharpSceneBindingCount = sp != null
            ? sp.AllScenes.SelectMany(s => s.Nodes).Count(n => n.IsCSharpScript)
            : 0;
    }

    /// <summary>
    /// Whether any C# code (.cs files) exists in the project.
    /// </summary>
    public bool HasCSharpCode => _hasCSharpCode;

    /// <summary>
    /// Whether any autoloads point to C# scripts.
    /// </summary>
    public bool HasCSharpAutoloads => _csharpAutoloadNames.Count > 0;

    /// <summary>
    /// Paths to all .cs files found in the project.
    /// </summary>
    public IReadOnlyList<string> CSharpScriptPaths => _csharpScriptPaths;

    /// <summary>
    /// Names of autoloads that point to C# scripts.
    /// </summary>
    public IReadOnlyList<string> CSharpAutoloadNames => _csharpAutoloadNames;

    /// <summary>
    /// Number of scene nodes with C# script bindings.
    /// </summary>
    public int CSharpSceneBindingCount => _csharpSceneBindingCount;
}
