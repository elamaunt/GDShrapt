using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for analyzing file dependencies in GDScript projects.
/// Uses GDTypeDependencyGraph and GDInferenceCycleDetector.
/// </summary>
public class GDDependencyService
{
    private readonly GDScriptProject _project;
    private readonly GDSignalConnectionRegistry? _signalRegistry;

    internal GDDependencyService(GDScriptProject project)
    {
        _project = project;
        _signalRegistry = null;
    }

    /// <summary>
    /// Creates a service with an explicit signal registry.
    /// Use this when you have a GDProjectSemanticModel.
    /// </summary>
    internal GDDependencyService(GDScriptProject project, GDSignalConnectionRegistry? signalRegistry)
    {
        _project = project;
        _signalRegistry = signalRegistry;
    }

    /// <summary>
    /// Analyzes the entire project and returns dependency information.
    /// </summary>
    public GDProjectDependencyReport AnalyzeProject()
    {
        var files = new List<GDFileDependencyInfo>();

        // Build file dependency graph first (for cycle detection)
        var fileDependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in _project.ScriptFiles)
        {
            var filePath = file.FullPath ?? "";
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Collect preloads and loads
            var loadCollector = new LoadCollector();
            file.Class?.WalkIn(loadCollector);

            foreach (var preload in loadCollector.Preloads)
            {
                var resolved = ResolveResPath(preload, filePath);
                if (!string.IsNullOrEmpty(resolved))
                    deps.Add(resolved);
            }

            foreach (var load in loadCollector.Loads)
            {
                var resolved = ResolveResPath(load, filePath);
                if (!string.IsNullOrEmpty(resolved))
                    deps.Add(resolved);
            }

            // Extends script dependency
            if (file.Class?.Extends?.Type != null)
            {
                var extendsName = file.Class.Extends.Type.BuildName();
                if (extendsName?.StartsWith("res://") == true)
                {
                    var resolved = ResolveResPath(extendsName, filePath);
                    if (!string.IsNullOrEmpty(resolved))
                        deps.Add(resolved);
                }
                // Handle class_name extends (e.g., "extends BaseClass")
                else if (!string.IsNullOrEmpty(extendsName) && !IsBuiltInType(extendsName))
                {
                    var resolved = ResolveClassName(extendsName);
                    if (!string.IsNullOrEmpty(resolved))
                        deps.Add(resolved);
                }
            }

            fileDependencies[filePath] = deps;
        }

        // Scene dependencies (scene→script, scene→sub-scene)
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider != null)
        {
            foreach (var sceneInfo in sceneProvider.AllScenes)
            {
                var scenePath = sceneInfo.FullPath?.Replace('\\', '/');
                if (string.IsNullOrEmpty(scenePath))
                    continue;

                var sceneDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var node in sceneInfo.Nodes)
                {
                    if (!string.IsNullOrEmpty(node.ScriptPath))
                    {
                        var resolved = ResolveResPath(node.ScriptPath, scenePath);
                        if (!string.IsNullOrEmpty(resolved))
                            sceneDeps.Add(resolved);
                    }
                }

                foreach (var sub in sceneInfo.SubSceneReferences)
                {
                    if (!string.IsNullOrEmpty(sub.SubScenePath))
                    {
                        var resolved = ResolveResPath(sub.SubScenePath, scenePath);
                        if (!string.IsNullOrEmpty(resolved))
                            sceneDeps.Add(resolved);
                    }
                }

                if (sceneDeps.Count > 0)
                    fileDependencies[scenePath] = sceneDeps;
            }
        }

        // Detect file-level cycles using DFS
        var cyclesFound = DetectFileCycles(fileDependencies);

        // Build filesInCycles set
        var filesInCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cycle in cyclesFound)
        {
            foreach (var filePath in cycle)
            {
                filesInCycles.Add(filePath);
            }
        }

        // Build file dependency info for each script file
        foreach (var file in _project.ScriptFiles)
        {
            var info = AnalyzeFileInternal(file, filesInCycles, cyclesFound);
            files.Add(info);
        }

        // Add scene file dependency info
        if (sceneProvider != null)
        {
            foreach (var sceneInfo in sceneProvider.AllScenes)
            {
                var scenePath = sceneInfo.FullPath?.Replace('\\', '/');
                if (string.IsNullOrEmpty(scenePath))
                    continue;

                var info = new GDFileDependencyInfo(scenePath)
                {
                    IsInCycle = filesInCycles.Contains(scenePath)
                };

                if (info.IsInCycle)
                {
                    info.CycleMembers = cyclesFound
                        .Where(c => c.Contains(scenePath))
                        .SelectMany(c => c)
                        .Distinct()
                        .ToList();
                }

                files.Add(info);
            }
        }

        // Build transitive dependencies
        BuildTransitiveDependencies(files, fileDependencies);

        return new GDProjectDependencyReport
        {
            Files = files,
            Cycles = cyclesFound
        };
    }

    /// <summary>
    /// Detects cycles in the file dependency graph using DFS.
    /// </summary>
    private List<IReadOnlyList<string>> DetectFileCycles(
        Dictionary<string, HashSet<string>> dependencies)
    {
        var cycles = new List<IReadOnlyList<string>>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentPath = new List<string>();

        foreach (var file in dependencies.Keys)
        {
            if (!visited.Contains(file))
            {
                DetectCycleDFS(file, dependencies, visited, recursionStack, currentPath, cycles);
            }
        }

        return cycles;
    }

    private void DetectCycleDFS(
        string current,
        Dictionary<string, HashSet<string>> dependencies,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath,
        List<IReadOnlyList<string>> cycles)
    {
        visited.Add(current);
        recursionStack.Add(current);
        currentPath.Add(current);

        if (dependencies.TryGetValue(current, out var deps))
        {
            foreach (var dep in deps)
            {
                if (!visited.Contains(dep))
                {
                    DetectCycleDFS(dep, dependencies, visited, recursionStack, currentPath, cycles);
                }
                else if (recursionStack.Contains(dep))
                {
                    // Found cycle - extract from dep to current
                    var cycleStart = currentPath.IndexOf(dep);
                    if (cycleStart >= 0)
                    {
                        var cycle = currentPath.Skip(cycleStart).ToList();
                        cycles.Add(cycle);
                    }
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        recursionStack.Remove(current);
    }

    /// <summary>
    /// Analyzes a single file and returns its dependency information.
    /// </summary>
    public GDFileDependencyInfo AnalyzeFile(GDScriptFile file)
    {
        var info = AnalyzeFileInternal(file, new HashSet<string>(), new List<IReadOnlyList<string>>());

        // Build direct dependencies for this single file
        var deps = new List<string>();

        if (!string.IsNullOrEmpty(info.ExtendsScript))
        {
            var resolvedPath = ResolveResPath(info.ExtendsScript, info.FilePath);
            if (!string.IsNullOrEmpty(resolvedPath))
                deps.Add(resolvedPath);
        }
        else if (!string.IsNullOrEmpty(info.ExtendsClass) && !IsBuiltInType(info.ExtendsClass))
        {
            var resolvedPath = ResolveClassName(info.ExtendsClass);
            if (!string.IsNullOrEmpty(resolvedPath))
                deps.Add(resolvedPath);
        }

        foreach (var preload in info.Preloads)
        {
            var resolvedPath = ResolveResPath(preload, info.FilePath);
            if (!string.IsNullOrEmpty(resolvedPath))
                deps.Add(resolvedPath);
        }

        foreach (var load in info.Loads)
        {
            var resolvedPath = ResolveResPath(load, info.FilePath);
            if (!string.IsNullOrEmpty(resolvedPath))
                deps.Add(resolvedPath);
        }

        info.Dependencies = deps.Distinct().ToList();
        return info;
    }

    private GDFileDependencyInfo AnalyzeFileInternal(
        GDScriptFile file,
        HashSet<string> filesInCycles,
        IReadOnlyList<IReadOnlyList<string>> allCycles)
    {
        var filePath = file.FullPath ?? "";
        var info = new GDFileDependencyInfo(filePath);

        // Check extends
        if (file.Class?.Extends != null)
        {
            var extendsType = file.Class.Extends.Type;
            if (extendsType != null)
            {
                var extendsTypeName = extendsType.BuildName();
                // Check if it's a path extends (starts with "res://")
                if (extendsTypeName != null && extendsTypeName.StartsWith("res://"))
                {
                    info.ExtendsScript = extendsTypeName;
                }
                else
                {
                    info.ExtendsClass = extendsTypeName;
                    if (!string.IsNullOrEmpty(extendsTypeName) && !IsBuiltInType(extendsTypeName))
                    {
                        var resolved = ResolveClassName(extendsTypeName);
                        if (!string.IsNullOrEmpty(resolved))
                            info.ExtendsProjectClass = true;
                    }
                }
            }
        }

        // Collect preloads and loads
        var preloads = new List<string>();
        var loads = new List<string>();

        var loadCollector = new LoadCollector();
        file.Class?.WalkIn(loadCollector);

        preloads.AddRange(loadCollector.Preloads);
        loads.AddRange(loadCollector.Loads);

        info.Preloads = preloads;
        info.Loads = loads;

        // Collect signal connections
        var signalSources = new List<string>();
        var signalListeners = new List<string>();

        if (_signalRegistry != null)
        {
            var connectionsInFile = _signalRegistry.GetConnectionsInFile(filePath);
            foreach (var conn in connectionsInFile)
            {
                // SourceFilePath is where the connection is made
                if (!string.IsNullOrEmpty(conn.SourceFilePath) && conn.SourceFilePath != filePath)
                {
                    signalSources.Add(conn.SourceFilePath);
                }
            }

            // Find files that listen to signals from this file
            var className = file.TypeName ?? file.Class?.ClassName?.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(className))
            {
                var allConnections = _signalRegistry.GetAllConnections();
                foreach (var conn in allConnections)
                {
                    // If emitter type matches our class, this file emits signals that others listen to
                    if (string.Equals(conn.EmitterType, className, StringComparison.OrdinalIgnoreCase))
                    {
                        signalListeners.Add(conn.SourceFilePath);
                    }
                }
            }
        }

        info.SignalSources = signalSources.Distinct().ToList();
        info.SignalListeners = signalListeners.Distinct().ToList();

        // Check if in cycle
        info.IsInCycle = filesInCycles.Contains(filePath);
        if (info.IsInCycle)
        {
            var cycleMembers = allCycles
                .Where(c => c.Contains(filePath))
                .SelectMany(c => c)
                .Distinct()
                .ToList();
            info.CycleMembers = cycleMembers;
        }

        return info;
    }

    private void BuildTransitiveDependencies(
        List<GDFileDependencyInfo> files,
        Dictionary<string, HashSet<string>> fileDependencies)
    {
        // Use the pre-computed fileDependencies map as the authoritative source
        // This includes both script deps (extends, preload, load) and scene deps (script attachments, sub-scenes)
        var directDeps = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (fileDependencies.TryGetValue(file.FilePath, out var deps))
                directDeps[file.FilePath] = deps;
            else
                directDeps[file.FilePath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Build reverse dependency map (dependents)
        var dependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in directDeps)
        {
            foreach (var dep in kvp.Value)
            {
                if (!dependents.TryGetValue(dep, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    dependents[dep] = set;
                }
                set.Add(kvp.Key);
            }
        }

        // Calculate transitive closures
        foreach (var file in files)
        {
            var transDeps = GetTransitiveClosure(file.FilePath, directDeps);
            var transDependents = GetTransitiveClosure(file.FilePath, dependents);

            file.Dependencies = transDeps.ToList();
            file.Dependents = transDependents.ToList();
        }
    }

    private HashSet<string> GetTransitiveClosure(
        string startFile,
        Dictionary<string, HashSet<string>> graph)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        if (graph.TryGetValue(startFile, out var directDeps))
        {
            foreach (var dep in directDeps)
            {
                queue.Enqueue(dep);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            result.Add(current);

            if (graph.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (!visited.Contains(dep))
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return result;
    }

    private string? ResolveResPath(string resPath, string fromFile)
    {
        if (string.IsNullOrEmpty(resPath))
            return null;

        // Handle res:// paths
        if (resPath.StartsWith("res://"))
        {
            var relativePath = resPath.Substring(6); // Remove "res://"
            var projectRoot = _project.ProjectPath;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                // Normalize to forward slashes to match GDScriptReference.FullPath
                var combined = Path.Combine(projectRoot, relativePath);
                return combined.Replace('\\', '/');
            }
        }

        return resPath;
    }

    private string? ResolveClassName(string className)
    {
        var script = _project.GetScriptByTypeName(className);
        return script?.FullPath?.Replace('\\', '/');
    }

    private static bool IsBuiltInType(string typeName)
    {
        // Godot built-in types - not project dependencies
        return typeName switch
        {
            "Node" or "Node2D" or "Node3D" or "Control" or "Resource" or "Object" or
            "CharacterBody2D" or "CharacterBody3D" or "RigidBody2D" or "RigidBody3D" or
            "Area2D" or "Area3D" or "StaticBody2D" or "StaticBody3D" or
            "Sprite2D" or "Sprite3D" or "Camera2D" or "Camera3D" or
            "Label" or "Button" or "TextEdit" or "LineEdit" or "Panel" or
            "RefCounted" or "PackedScene" or "Texture2D" or "Texture3D" or
            "AnimationPlayer" or "AudioStreamPlayer" or "Timer" or "Tween" or
            "CanvasLayer" or "Viewport" or "SubViewport" or "Window" => true,
            _ => false
        };
    }

    #region Load Collector

    private class LoadCollector : GDVisitor
    {
        public List<string> Preloads { get; } = new();
        public List<string> Loads { get; } = new();

        public override void Visit(GDCallExpression node)
        {
            var callerExpr = node.CallerExpression;

            if (callerExpr is GDIdentifierExpression identExpr)
            {
                var funcName = identExpr.Identifier?.Sequence;
                if (GDWellKnownFunctions.IsResourceLoader(funcName))
                {
                    var path = ExtractFirstStringArg(node);
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (funcName == GDWellKnownFunctions.Preload)
                            Preloads.Add(path);
                        else
                            Loads.Add(path);
                    }
                }
            }

            base.Visit(node);
        }

        private string? ExtractFirstStringArg(GDCallExpression call)
        {
            var args = call.Parameters;
            if (args == null || args.Count == 0)
                return null;

            var firstArg = args.FirstOrDefault();

            if (firstArg is GDStringExpression strExpr)
            {
                return strExpr.String?.Sequence;
            }

            if (firstArg is GDStringNameExpression strNameExpr)
            {
                return strNameExpr.String?.Sequence;
            }

            return null;
        }
    }

    #endregion
}
