using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Listens to scene change events and determines which scripts need reanalysis.
/// Bridges GDSceneTypesProvider.SceneChanged to GDScriptProject.SceneScriptsChanged.
/// </summary>
public class GDSceneChangeReanalysisService : IDisposable
{
    private readonly GDScriptProject _project;
    private readonly GDSceneTypesProvider _sceneTypesProvider;
    private readonly IGDLogger _logger;
    private readonly Dictionary<string, HashSet<string>> _sceneToScripts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncLock = new();
    private bool _disposed;

    /// <summary>
    /// Fired when scripts need reanalysis due to scene changes.
    /// </summary>
    public event EventHandler<GDSceneAffectedScriptsEventArgs>? ScriptsNeedReanalysis;

    public GDSceneChangeReanalysisService(
        GDScriptProject project,
        GDSceneTypesProvider sceneTypesProvider,
        IGDLogger logger)
    {
        _project = project;
        _sceneTypesProvider = sceneTypesProvider;
        _logger = logger;

        _sceneTypesProvider.SceneChanged += OnSceneChanged;

        BuildReverseIndex();
    }

    private void BuildReverseIndex()
    {
        lock (_syncLock)
        {
            _sceneToScripts.Clear();

            foreach (var scene in _sceneTypesProvider.AllScenes)
            {
                var scriptPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var scriptPath in scene.ScriptToNodePath.Keys)
                {
                    scriptPaths.Add(scriptPath);
                }

                if (scriptPaths.Count > 0)
                {
                    _sceneToScripts[scene.ScenePath] = scriptPaths;
                }
            }
        }
    }

    private void OnSceneChanged(object? sender, GDSceneChangedEventArgs e)
    {
        try
        {
            lock (_syncLock)
            {
                var affectedScripts = new List<GDScriptFile>();

                if (e.SceneInfo != null)
                {
                    // Scene was created or modified — use current script bindings
                    var currentScriptPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var scriptPath in e.SceneInfo.ScriptToNodePath.Keys)
                    {
                        currentScriptPaths.Add(scriptPath);
                        var script = _project.GetScriptByResourcePath(scriptPath);
                        if (script != null)
                            affectedScripts.Add(script);
                    }

                    // Also include scripts from the old binding (they may have been unbound)
                    if (_sceneToScripts.TryGetValue(e.ScenePath, out var oldScriptPaths))
                    {
                        foreach (var oldPath in oldScriptPaths)
                        {
                            if (!currentScriptPaths.Contains(oldPath))
                            {
                                var script = _project.GetScriptByResourcePath(oldPath);
                                if (script != null && !affectedScripts.Contains(script))
                                    affectedScripts.Add(script);
                            }
                        }
                    }

                    // Update reverse index
                    _sceneToScripts[e.ScenePath] = currentScriptPaths;
                }
                else
                {
                    // Scene was deleted — use cached reverse index
                    if (_sceneToScripts.TryGetValue(e.ScenePath, out var oldScriptPaths))
                    {
                        foreach (var scriptPath in oldScriptPaths)
                        {
                            var script = _project.GetScriptByResourcePath(scriptPath);
                            if (script != null)
                                affectedScripts.Add(script);
                        }

                        _sceneToScripts.Remove(e.ScenePath);
                    }
                }

                if (affectedScripts.Count > 0)
                {
                    _logger.Debug($"Scene '{e.ScenePath}' changed, {affectedScripts.Count} script(s) need reanalysis");
                    ScriptsNeedReanalysis?.Invoke(this, new GDSceneAffectedScriptsEventArgs(e.ScenePath, affectedScripts));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error processing scene change for {e.ScenePath}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sceneTypesProvider.SceneChanged -= OnSceneChanged;
    }
}
