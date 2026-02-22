using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides parsed .tres resource file data for semantic analysis.
/// Scans, parses, and caches .tres files for property reference queries.
/// </summary>
internal class GDTresResourceProvider
{
    private readonly string _projectPath;
    private readonly IGDFileSystem _fileSystem;
    private readonly IGDLogger _logger;
    private readonly Dictionary<string, GDTresResourceInfo> _resourceCache = new(StringComparer.OrdinalIgnoreCase);

    // Lookup index: effectiveClassName → set of property names across all .tres files for that class
    private readonly Dictionary<string, HashSet<string>> _propertyIndex = new(StringComparer.OrdinalIgnoreCase);

    private bool _classNamesResolved;

    public GDTresResourceProvider(string projectPath, IGDFileSystem fileSystem, IGDLogger logger)
    {
        _projectPath = projectPath;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public IEnumerable<GDTresResourceInfo> AllResources => _resourceCache.Values;

    public int ResourceCount => _resourceCache.Count;

    public GDTresResourceInfo? GetResourceInfo(string resourcePath)
    {
        _resourceCache.TryGetValue(resourcePath, out var info);
        return info;
    }

    public void ReloadAllResources()
    {
        ClearCache();

        if (!_fileSystem.DirectoryExists(_projectPath))
            return;

        var tresFiles = _fileSystem.GetFiles(_projectPath, "*.tres", recursive: true);

        foreach (var filePath in tresFiles)
        {
            // Skip .godot cache directory
            var relativePath = filePath.Substring(_projectPath.Length).TrimStart(Path.DirectorySeparatorChar);
            if (relativePath.StartsWith(".godot", StringComparison.OrdinalIgnoreCase))
                continue;

            LoadResource(filePath);
        }

        _logger.Debug($"Loaded {_resourceCache.Count} .tres resource files");
    }

    private void LoadResource(string fullPath)
    {
        try
        {
            var content = _fileSystem.ReadAllText(fullPath);
            if (string.IsNullOrEmpty(content))
                return;

            var parseResult = GDTresParser.ParseFull(content);

            // Only index resources that have a script reference (custom GDScript resources)
            if (parseResult.ScriptClass == null && parseResult.ScriptExtResourceId == null)
                return;

            var relativePath = fullPath.Substring(_projectPath.Length).TrimStart(Path.DirectorySeparatorChar);
            var resPath = "res://" + relativePath.Replace(Path.DirectorySeparatorChar, '/');

            // Resolve script path from ExtResource id
            string? scriptPath = null;
            if (parseResult.ScriptExtResourceId != null)
            {
                var scriptExtRes = parseResult.ExtResources
                    .FirstOrDefault(e => e.Id == parseResult.ScriptExtResourceId);
                if (scriptExtRes != null)
                    scriptPath = scriptExtRes.Path;
            }

            var info = new GDTresResourceInfo
            {
                ResourcePath = resPath,
                FullPath = fullPath,
                ResourceType = parseResult.ResourceType,
                ScriptClass = parseResult.ScriptClass,
                ScriptPath = scriptPath,
                Properties = parseResult.ResourceProperties,
                ExtResources = parseResult.ExtResources
            };

            _resourceCache[resPath] = info;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to parse .tres file: {fullPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves class names from script paths using the project's loaded scripts.
    /// Must be called after scripts are loaded and analyzed.
    /// </summary>
    internal void ResolveClassNames(GDScriptProject project)
    {
        if (_classNamesResolved)
            return;

        _classNamesResolved = true;
        _propertyIndex.Clear();

        foreach (var info in _resourceCache.Values)
        {
            // Try to resolve class name from script path
            if (info.ResolvedClassName == null && info.ScriptPath != null)
            {
                var scriptFile = project.GetScriptByResourcePath(info.ScriptPath);
                if (scriptFile != null)
                    info.ResolvedClassName = scriptFile.TypeName;
            }

            // Build property index
            var className = info.EffectiveClassName;
            if (string.IsNullOrEmpty(className) || info.Properties.Count == 0)
                continue;

            if (!_propertyIndex.TryGetValue(className, out var propertySet))
            {
                propertySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _propertyIndex[className] = propertySet;
            }

            foreach (var prop in info.Properties)
            {
                propertySet.Add(prop.Name);
            }
        }

        _logger.Debug($"Resolved {_propertyIndex.Count} class(es) with .tres property references");
    }

    /// <summary>
    /// Checks if a member name is referenced as a property in any .tres file
    /// whose owning class matches one of the provided class names.
    /// </summary>
    public bool HasPropertyReference(IList<string> classNames, string memberName)
    {
        for (int i = 0; i < classNames.Count; i++)
        {
            if (_propertyIndex.TryGetValue(classNames[i], out var propertySet)
                && propertySet.Contains(memberName))
                return true;
        }

        return false;
    }

    public void ClearCache()
    {
        _resourceCache.Clear();
        _propertyIndex.Clear();
        _classNamesResolved = false;
    }
}
