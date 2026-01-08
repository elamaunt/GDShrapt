using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of IProjectAnalyzer that wraps GDProjectMap.
/// </summary>
internal class ProjectAnalyzerImpl : IProjectAnalyzer
{
    private readonly GDProjectMap _projectMap;
    private readonly Dictionary<string, ScriptInfoImpl> _scriptCache = new();

    public ProjectAnalyzerImpl(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
    }

    public IReadOnlyList<IScriptInfo> Scripts
    {
        get
        {
            RefreshCache();
            return _scriptCache.Values.ToList();
        }
    }

    public event Action<IScriptInfo>? ScriptAdded;
    public event Action<string>? ScriptRemoved;
    public event Action<IScriptInfo>? ScriptChanged;

    public IScriptInfo? GetScriptByResourcePath(string resourcePath)
    {
        var scriptMap = _projectMap.GetScriptMapByResourcePath(resourcePath);
        return scriptMap != null ? GetOrCreateScriptInfo(scriptMap) : null;
    }

    public IScriptInfo? GetScriptByFullPath(string fullPath)
    {
        var scriptMap = _projectMap.GetScriptMap(fullPath);
        return scriptMap != null ? GetOrCreateScriptInfo(scriptMap) : null;
    }

    public IScriptInfo? GetScriptByTypeName(string typeName)
    {
        var scriptMap = _projectMap.GetScriptMapByTypeName(typeName);
        return scriptMap != null ? GetOrCreateScriptInfo(scriptMap) : null;
    }

    private ScriptInfoImpl GetOrCreateScriptInfo(GDScriptMap scriptMap)
    {
        var key = scriptMap.Reference?.FullPath ?? string.Empty;
        if (!_scriptCache.TryGetValue(key, out var info))
        {
            info = new ScriptInfoImpl(scriptMap);
            _scriptCache[key] = info;
        }
        return info;
    }

    private void RefreshCache()
    {
        // Clear outdated entries
        var currentPaths = _projectMap.Scripts
            .Select(s => s.Reference?.FullPath ?? string.Empty)
            .ToHashSet();

        var keysToRemove = _scriptCache.Keys
            .Where(k => !currentPaths.Contains(k))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _scriptCache.Remove(key);
        }

        // Add new entries
        foreach (var scriptMap in _projectMap.Scripts)
        {
            GetOrCreateScriptInfo(scriptMap);
        }
    }

    /// <summary>
    /// Raises the ScriptAdded event.
    /// </summary>
    internal void RaiseScriptAdded(GDScriptMap scriptMap)
    {
        var info = GetOrCreateScriptInfo(scriptMap);
        ScriptAdded?.Invoke(info);
    }

    /// <summary>
    /// Raises the ScriptRemoved event.
    /// </summary>
    internal void RaiseScriptRemoved(string resourcePath)
    {
        ScriptRemoved?.Invoke(resourcePath);
    }

    /// <summary>
    /// Raises the ScriptChanged event.
    /// </summary>
    internal void RaiseScriptChanged(GDScriptMap scriptMap)
    {
        var info = GetOrCreateScriptInfo(scriptMap);
        ScriptChanged?.Invoke(info);
    }
}
