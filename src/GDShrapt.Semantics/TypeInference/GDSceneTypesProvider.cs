using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides type information from Godot scene files (.tscn).
/// Parses scene files to extract node paths and their associated types/scripts.
/// Used for type inference on get_node() calls and $NodePath expressions.
/// </summary>
public class GDSceneTypesProvider : IGDRuntimeProvider, IDisposable
{
    private readonly string _projectPath;
    private readonly IGDFileSystem _fileSystem;
    private readonly IGDLogger _logger;
    private readonly Dictionary<string, GDSceneInfo> _sceneCache = new();
    private readonly Dictionary<string, GDNodeTypeInfo> _nodePathCache = new();
    private readonly Dictionary<string, GDSceneSnapshot> _snapshots = new();
    private FileSystemWatcher? _sceneWatcher;
    private System.Threading.Timer? _debounceTimer;
    private readonly object _timerLock = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingChanges = new();
    private DateTime _lastOwnWrite = DateTime.MinValue;
    private bool _disposed;

    private const int DebounceDelayMs = 300;
    private const int IgnoreOwnWriteSeconds = 2;

    #region Events

    /// <summary>
    /// Fired when a scene file is changed on disk.
    /// </summary>
    public event EventHandler<GDSceneChangedEventArgs>? SceneChanged;

    /// <summary>
    /// Fired when node renames are detected in a scene file.
    /// </summary>
    public event EventHandler<GDNodeRenameDetectedEventArgs>? NodeRenameDetected;

    #endregion

    public GDSceneTypesProvider(string projectPath, IGDFileSystem? fileSystem = null, IGDLogger? logger = null)
    {
        _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        _fileSystem = fileSystem ?? new GDDefaultFileSystem();
        _logger = logger ?? GDNullLogger.Instance;
    }

    /// <summary>
    /// Loads and caches scene information from a .tscn file.
    /// </summary>
    public void LoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return;

        var fullPath = GetFullPath(scenePath);
        if (!_fileSystem.FileExists(fullPath))
            return;

        try
        {
            var content = _fileSystem.ReadAllText(fullPath);
            var sceneInfo = ParseSceneFile(content, scenePath);
            _sceneCache[scenePath] = sceneInfo;

            // Update node path cache
            foreach (var node in sceneInfo.Nodes)
            {
                var key = $"{scenePath}:{node.Path}";
                _nodePathCache[key] = node;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load scene {scenePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the type for a node at a specific path in a scene.
    /// </summary>
    public string? GetNodeType(string scenePath, string nodePath)
    {
        var key = $"{scenePath}:{nodePath}";
        if (_nodePathCache.TryGetValue(key, out var nodeInfo))
        {
            return nodeInfo.ScriptTypeName ?? nodeInfo.NodeType;
        }
        return null;
    }

    /// <summary>
    /// Gets all node paths in a scene.
    /// </summary>
    public IReadOnlyList<string> GetNodePaths(string scenePath)
    {
        if (_sceneCache.TryGetValue(scenePath, out var sceneInfo))
        {
            return sceneInfo.Nodes.Select(n => n.Path).ToList();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets the script path attached to a node.
    /// </summary>
    public string? GetNodeScript(string scenePath, string nodePath)
    {
        var key = $"{scenePath}:{nodePath}";
        if (_nodePathCache.TryGetValue(key, out var nodeInfo))
        {
            return nodeInfo.ScriptPath;
        }
        return null;
    }

    /// <summary>
    /// Gets the type of a unique node (marked with unique_name_in_owner = true) by name.
    /// </summary>
    /// <param name="scenePath">Resource path of the scene.</param>
    /// <param name="nodeName">Name of the unique node (without % prefix).</param>
    /// <returns>The node type if found, or null.</returns>
    public string? GetUniqueNodeType(string scenePath, string nodeName)
    {
        if (!_sceneCache.TryGetValue(scenePath, out var sceneInfo))
            return null;

        // Find unique node by name
        var node = sceneInfo.UniqueNodes.FirstOrDefault(n => n.Name == nodeName);
        if (node != null)
        {
            return node.ScriptTypeName ?? node.NodeType;
        }

        // Fallback: search by name in all nodes (for scenes where unique_name_in_owner wasn't parsed)
        var fallbackNode = sceneInfo.Nodes.FirstOrDefault(n => n.Name == nodeName);
        return fallbackNode != null ? (fallbackNode.ScriptTypeName ?? fallbackNode.NodeType) : null;
    }

    private GDSceneInfo ParseSceneFile(string content, string scenePath)
    {
        var fullPath = GetFullPath(scenePath);
        var sceneInfo = new GDSceneInfo { ScenePath = scenePath, FullPath = fullPath };
        var nodes = new List<GDNodeTypeInfo>();

        // Split content into lines for line number tracking
        var lines = content.Split('\n');

        // Parse external resources (scripts)
        // Godot 4 format: [ext_resource type="Script" uid="..." path="res://..." id="..."]
        // Attributes can be in any order, so we need flexible parsing
        var extResources = new Dictionary<string, string>();
        var extResBlockRegex = new Regex(@"\[ext_resource\s+([^\]]+)\]", RegexOptions.Multiline);
        var pathRegex = new Regex(@"path=""([^""]+)""");
        var idRegex = new Regex(@"\sid=""([^""]+)""");  // Note: \s to avoid matching 'uid'

        foreach (Match blockMatch in extResBlockRegex.Matches(content))
        {
            var block = blockMatch.Value;
            var pathMatch = pathRegex.Match(block);
            var idMatch = idRegex.Match(block);

            if (pathMatch.Success && idMatch.Success)
            {
                var path = pathMatch.Groups[1].Value;
                var id = idMatch.Groups[1].Value;
                if (path.EndsWith(".gd"))
                {
                    extResources[id] = path;
                }
            }
        }

        // Parse sub-resources (embedded scripts)
        var subResources = new Dictionary<string, string>();
        var subResRegex = new Regex(@"\[sub_resource\s+type=""GDScript""\s+id=""([^""]+)""\]", RegexOptions.Multiline);
        foreach (Match match in subResRegex.Matches(content))
        {
            var id = match.Groups[1].Value;
            subResources[id] = "embedded";
        }

        // Parse nodes with line numbers
        var nodeRegex = new Regex(@"\[node\s+name=""([^""]+)""(?:\s+type=""([^""]+)"")?(?:\s+parent=""([^""]*)"")?\]", RegexOptions.Multiline);
        var rootPath = "";

        foreach (Match match in nodeRegex.Matches(content))
        {
            var name = match.Groups[1].Value;
            var type = match.Groups[2].Success ? match.Groups[2].Value : "Node";
            var parent = match.Groups[3].Success ? match.Groups[3].Value : "";

            // Calculate line number from match position
            var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;

            string path;
            if (string.IsNullOrEmpty(parent))
            {
                // Root node
                path = ".";
                rootPath = name;
            }
            else if (parent == ".")
            {
                // Direct child of root
                path = name;
            }
            else
            {
                // Nested child
                path = $"{parent}/{name}";
            }

            var nodeInfo = new GDNodeTypeInfo
            {
                Name = name,
                Path = path,
                NodeType = type,
                LineNumber = lineNumber,
                ParentPath = parent,
                SceneFullPath = fullPath,
                OriginalLine = lineNumber <= lines.Length ? lines[lineNumber - 1].Trim() : null
            };

            nodes.Add(nodeInfo);
        }

        // Parse script assignments - find which node each script belongs to by position
        var scriptRegex = new Regex(@"script\s*=\s*ExtResource\(\s*""([^""]+)""\s*\)", RegexOptions.Multiline);
        var nodeMatches = nodeRegex.Matches(content).Cast<Match>().ToList();

        foreach (Match scriptMatch in scriptRegex.Matches(content))
        {
            var scriptPos = scriptMatch.Index;
            var scriptRef = scriptMatch.Groups[1].Value;

            if (!extResources.TryGetValue(scriptRef, out var scriptPath))
                continue;

            // Find which node this script belongs to by finding the last node defined before this script
            int ownerNodeIndex = -1;
            for (int i = 0; i < nodeMatches.Count; i++)
            {
                if (nodeMatches[i].Index < scriptPos)
                {
                    // Check if next node is after script (meaning this node owns the script)
                    if (i + 1 >= nodeMatches.Count || nodeMatches[i + 1].Index > scriptPos)
                    {
                        ownerNodeIndex = i;
                        break;
                    }
                }
            }

            if (ownerNodeIndex >= 0 && ownerNodeIndex < nodes.Count)
            {
                nodes[ownerNodeIndex].ScriptPath = scriptPath;
                nodes[ownerNodeIndex].ScriptTypeName = GetTypeNameFromScriptPath(scriptPath);
                sceneInfo.ScriptToNodePath[scriptPath] = nodes[ownerNodeIndex].Path;
            }
        }

        // Parse unique nodes (marked with unique_name_in_owner = true)
        var uniqueNodeRegex = new Regex(@"unique_name_in_owner\s*=\s*true", RegexOptions.Multiline);
        var uniqueNodes = new List<GDNodeTypeInfo>();

        foreach (Match uniqueMatch in uniqueNodeRegex.Matches(content))
        {
            var uniquePos = uniqueMatch.Index;

            // Find which node this belongs to by finding the last node defined before this attribute
            for (int i = nodeMatches.Count - 1; i >= 0; i--)
            {
                if (nodeMatches[i].Index < uniquePos)
                {
                    // Check if next node is after unique attribute (meaning this node owns it)
                    if (i + 1 >= nodeMatches.Count || nodeMatches[i + 1].Index > uniquePos)
                    {
                        if (i < nodes.Count)
                        {
                            nodes[i].IsUnique = true;
                            uniqueNodes.Add(nodes[i]);
                        }
                        break;
                    }
                }
            }
        }

        sceneInfo.Nodes = nodes;
        sceneInfo.UniqueNodes = uniqueNodes;

        // Parse signal connections
        sceneInfo.SignalConnections = ParseSignalConnections(content, nodes);

        return sceneInfo;
    }

    /// <summary>
    /// Parses signal connections from scene file content.
    /// Format: [connection signal="pressed" from="Button" to="." method="_on_button_pressed"]
    /// </summary>
    private List<GDSignalConnectionInfo> ParseSignalConnections(string content, List<GDNodeTypeInfo> nodes)
    {
        var connections = new List<GDSignalConnectionInfo>();

        // Match [connection signal="..." from="..." to="..." method="..."]
        var connectionRegex = new Regex(
            @"\[connection\s+" +
            @"signal=""([^""]+)""\s+" +
            @"from=""([^""]+)""\s+" +
            @"to=""([^""]+)""\s+" +
            @"method=""([^""]+)""" +
            @"[^\]]*\]",
            RegexOptions.Multiline);

        foreach (Match match in connectionRegex.Matches(content))
        {
            var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
            var fromNode = match.Groups[2].Value;

            // Resolve source node type
            string? sourceNodeType = null;
            var sourceNode = nodes.FirstOrDefault(n =>
                n.Path == fromNode ||
                n.Name == fromNode ||
                (fromNode == "." && n.Path == "."));

            if (sourceNode != null)
            {
                sourceNodeType = sourceNode.ScriptTypeName ?? sourceNode.NodeType;
            }

            connections.Add(new GDSignalConnectionInfo
            {
                SignalName = match.Groups[1].Value,
                FromNode = fromNode,
                ToNode = match.Groups[3].Value,
                Method = match.Groups[4].Value,
                LineNumber = lineNumber,
                SourceNodeType = sourceNodeType
            });
        }

        return connections;
    }

    private string GetTypeNameFromScriptPath(string scriptPath)
    {
        // Extract type name from script path
        // e.g., "res://scripts/player.gd" -> "Player" (if class_name is used)
        // Otherwise use filename: "player"
        var fileName = Path.GetFileNameWithoutExtension(scriptPath);
        return ToPascalCase(fileName);
    }

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split('_', '-', ' ');
        return string.Concat(words.Select(w =>
            string.IsNullOrEmpty(w) ? "" :
            char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w.Substring(1).ToLowerInvariant() : "")));
    }

    private string GetFullPath(string resPath)
    {
        if (resPath.StartsWith("res://"))
        {
            return Path.Combine(_projectPath, resPath.Substring(6).Replace('/', Path.DirectorySeparatorChar));
        }
        return Path.Combine(_projectPath, resPath);
    }

    // IGDRuntimeProvider implementation

    public bool IsKnownType(string typeName)
    {
        // Scene types are resolved through node paths, not type names
        return false;
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        return null;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        return null;
    }

    public string? GetBaseType(string typeName)
    {
        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        return null;
    }

    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        return null;
    }

    public bool IsBuiltIn(string identifier)
    {
        return false;
    }

    public IEnumerable<string> GetAllTypes()
    {
        // Scene types are resolved through node paths, not as standalone types.
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Clears all cached scene data.
    /// </summary>
    public void ClearCache()
    {
        _sceneCache.Clear();
        _nodePathCache.Clear();
    }

    /// <summary>
    /// Finds all scenes that use a specific script.
    /// </summary>
    /// <param name="scriptPath">Path to the script (can be resource path like "res://scripts/player.gd" or full filesystem path).</param>
    /// <returns>Tuples of (scene resource path, node path within scene).</returns>
    public IEnumerable<(string scenePath, string nodePath)> GetScenesForScript(string scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath))
            yield break;

        // Normalize to resource path for comparison
        var resourcePath = ToResourcePath(scriptPath);

        foreach (var scene in _sceneCache.Values)
        {
            if (scene.ScriptToNodePath.TryGetValue(resourcePath, out var nodePath))
            {
                yield return (scene.ScenePath, nodePath);
            }

            // Also check nodes directly
            foreach (var node in scene.Nodes)
            {
                if (node.ScriptPath == resourcePath)
                {
                    yield return (scene.ScenePath, node.Path);
                }
            }
        }
    }

    /// <summary>
    /// Gets a node by name with its line number information.
    /// </summary>
    public GDNodeTypeInfo? GetNodeByName(string scenePath, string nodeName)
    {
        if (!_sceneCache.TryGetValue(scenePath, out var sceneInfo))
            return null;

        return sceneInfo.Nodes.FirstOrDefault(n => n.Name == nodeName);
    }

    /// <summary>
    /// Gets all nodes that have a specific node as part of their parent path.
    /// Used for finding references that need updating when a node is renamed.
    /// </summary>
    public IEnumerable<GDNodeTypeInfo> GetNodesWithParentContaining(string scenePath, string nodeName)
    {
        if (!_sceneCache.TryGetValue(scenePath, out var sceneInfo))
            yield break;

        foreach (var node in sceneInfo.Nodes)
        {
            if (string.IsNullOrEmpty(node.ParentPath))
                continue;

            // Check if parent path contains the node name
            if (node.ParentPath == nodeName ||
                node.ParentPath.StartsWith(nodeName + "/") ||
                node.ParentPath.Contains("/" + nodeName + "/") ||
                node.ParentPath.EndsWith("/" + nodeName))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Gets the SceneInfo for a scene path.
    /// </summary>
    public GDSceneInfo? GetSceneInfo(string scenePath)
    {
        _sceneCache.TryGetValue(scenePath, out var info);
        return info;
    }

    /// <summary>
    /// Gets all cached scenes.
    /// </summary>
    public IEnumerable<GDSceneInfo> AllScenes => _sceneCache.Values;

    /// <summary>
    /// Gets all signal connections for a scene.
    /// </summary>
    public IEnumerable<GDSignalConnectionInfo> GetSignalConnections(string scenePath)
    {
        if (_sceneCache.TryGetValue(scenePath, out var info))
            return info.SignalConnections;
        return Array.Empty<GDSignalConnectionInfo>();
    }

    /// <summary>
    /// Gets signal connections where the target method matches the specified name.
    /// Used to validate that the method exists and matches signal signature.
    /// </summary>
    public IEnumerable<GDSignalConnectionInfo> GetConnectionsToMethod(string scenePath, string methodName)
    {
        return GetSignalConnections(scenePath)
            .Where(c => c.Method == methodName);
    }

    /// <summary>
    /// Gets all signal connections that use a specific signal across all loaded scenes.
    /// </summary>
    public IEnumerable<(string scenePath, GDSignalConnectionInfo connection)> GetConnectionsBySignal(string signalName)
    {
        foreach (var scene in _sceneCache.Values)
        {
            foreach (var conn in scene.SignalConnections)
            {
                if (conn.SignalName == signalName)
                    yield return (scene.ScenePath, conn);
            }
        }
    }

    /// <summary>
    /// Gets all signal connections targeting methods in scripts that match the given script path.
    /// </summary>
    /// <param name="scriptPath">Path to the script.</param>
    /// <param name="methodName">Method name to search for.</param>
    /// <returns>Signal connections targeting this method.</returns>
    public IEnumerable<GDSignalConnectionInfo> GetSignalConnectionsForScriptMethod(string scriptPath, string methodName)
    {
        if (string.IsNullOrEmpty(scriptPath) || string.IsNullOrEmpty(methodName))
            yield break;

        var resourcePath = ToResourcePath(scriptPath);

        foreach (var scene in _sceneCache.Values)
        {
            // Collect node paths that use this script
            var nodePathsWithScript = new HashSet<string>();
            foreach (var node in scene.Nodes)
            {
                if (node.ScriptPath == resourcePath)
                {
                    nodePathsWithScript.Add(node.Path);
                }
            }

            if (nodePathsWithScript.Count == 0)
                continue;

            // Find connections targeting any of these nodes
            foreach (var conn in scene.SignalConnections)
            {
                if (conn.Method == methodName && nodePathsWithScript.Contains(conn.ToNode))
                {
                    yield return conn;
                }
            }
        }
    }

    /// <summary>
    /// Gets the full filesystem path from a resource path.
    /// </summary>
    public string GetFullPathFromResource(string resPath)
    {
        return GetFullPath(resPath);
    }

    /// <summary>
    /// Reloads all scenes in the project.
    /// </summary>
    public void ReloadAllScenes()
    {
        ClearCache();

        if (!_fileSystem.DirectoryExists(_projectPath))
            return;

        var sceneFiles = _fileSystem.GetFiles(_projectPath, "*.tscn", recursive: true);
        foreach (var scenePath in sceneFiles)
        {
            var relativePath = "res://" + scenePath
                .Substring(_projectPath.Length)
                .TrimStart(Path.DirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');

            LoadScene(relativePath);
        }
    }

    #region FileSystemWatcher

    /// <summary>
    /// Enables file system watching for scene file changes.
    /// </summary>
    public void EnableFileWatcher()
    {
        if (_sceneWatcher != null) return;
        if (!_fileSystem.DirectoryExists(_projectPath)) return;

        // Capture initial snapshots
        foreach (var scene in AllScenes)
        {
            CaptureSnapshot(scene.ScenePath);
        }

        _sceneWatcher = new FileSystemWatcher(_projectPath, "*.tscn")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _sceneWatcher.Changed += OnSceneFileChanged;
        _sceneWatcher.Created += OnSceneFileCreated;
        _sceneWatcher.Deleted += OnSceneFileDeleted;
        _sceneWatcher.Renamed += OnSceneFileRenamed;

        _logger.Debug($"Scene FileSystemWatcher enabled with {_snapshots.Count} initial snapshots");
    }

    /// <summary>
    /// Disables file system watching.
    /// </summary>
    public void DisableFileWatcher()
    {
        if (_sceneWatcher == null) return;

        _sceneWatcher.EnableRaisingEvents = false;
        _sceneWatcher.Changed -= OnSceneFileChanged;
        _sceneWatcher.Created -= OnSceneFileCreated;
        _sceneWatcher.Deleted -= OnSceneFileDeleted;
        _sceneWatcher.Renamed -= OnSceneFileRenamed;
        _sceneWatcher.Dispose();
        _sceneWatcher = null;

        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        _logger.Debug("Scene FileSystemWatcher disabled");
    }

    /// <summary>
    /// Marks that we're about to write files ourselves.
    /// Subsequent file change events within 2 seconds will be ignored.
    /// </summary>
    public void MarkOwnWrite()
    {
        _lastOwnWrite = DateTime.UtcNow;
        _logger.Debug("Marked own write");
    }

    /// <summary>
    /// Refreshes snapshots for all scenes.
    /// </summary>
    public void RefreshAllSnapshots()
    {
        _snapshots.Clear();
        ReloadAllScenes();

        foreach (var scene in AllScenes)
        {
            CaptureSnapshot(scene.ScenePath);
        }

        _logger.Debug($"Refreshed all scene snapshots ({_snapshots.Count} scenes)");
    }

    private void OnSceneFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
            return;

        // Ignore own writes
        if ((DateTime.UtcNow - _lastOwnWrite).TotalSeconds < IgnoreOwnWriteSeconds)
        {
            _logger.Debug($"Ignoring own write to {e.Name}");
            return;
        }

        _logger.Debug($"Scene changed: {e.Name}");

        var resourcePath = ToResourcePath(e.FullPath);
        _pendingChanges.Enqueue(resourcePath);
        ResetDebounceTimer();
    }

    private void OnSceneFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.Debug($"Scene created: {e.Name}");

        var resourcePath = ToResourcePath(e.FullPath);
        LoadScene(resourcePath);
        CaptureSnapshot(resourcePath);

        var sceneInfo = GetSceneInfo(resourcePath);
        SceneChanged?.Invoke(this, new GDSceneChangedEventArgs(resourcePath, e.FullPath, sceneInfo));
    }

    private void OnSceneFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.Debug($"Scene deleted: {e.Name}");

        var resourcePath = ToResourcePath(e.FullPath);
        _snapshots.Remove(resourcePath);
        _sceneCache.Remove(resourcePath);

        SceneChanged?.Invoke(this, new GDSceneChangedEventArgs(resourcePath, e.FullPath, null));
    }

    private void OnSceneFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.Debug($"Scene renamed: {e.OldName} -> {e.Name}");

        var oldResourcePath = ToResourcePath(e.OldFullPath);
        var newResourcePath = ToResourcePath(e.FullPath);

        _snapshots.Remove(oldResourcePath);
        _sceneCache.Remove(oldResourcePath);

        LoadScene(newResourcePath);
        CaptureSnapshot(newResourcePath);

        var sceneInfo = GetSceneInfo(newResourcePath);
        SceneChanged?.Invoke(this, new GDSceneChangedEventArgs(newResourcePath, e.FullPath, sceneInfo));
    }

    private void ResetDebounceTimer()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(ProcessPendingChanges, null, DebounceDelayMs, System.Threading.Timeout.Infinite);
        }
    }

    private void ProcessPendingChanges(object? state)
    {
        try
        {
            var scenes = new HashSet<string>();
            while (_pendingChanges.TryDequeue(out var path))
            {
                scenes.Add(path);
            }

            foreach (var scenePath in scenes)
            {
                ProcessSceneChange(scenePath);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing scene changes: {ex.Message}");
        }
    }

    private void ProcessSceneChange(string scenePath)
    {
        _logger.Debug($"Processing scene change: {scenePath}");

        // Get old snapshot
        if (!_snapshots.TryGetValue(scenePath, out var oldSnapshot))
        {
            _logger.Debug($"No previous snapshot for {scenePath}, creating new one");
            LoadScene(scenePath);
            CaptureSnapshot(scenePath);
            return;
        }

        // Reload scene from disk
        LoadScene(scenePath);
        var newScene = GetSceneInfo(scenePath);

        if (newScene == null)
        {
            _logger.Debug($"Failed to load scene {scenePath}");
            return;
        }

        // Detect renames by comparing line numbers
        var renames = DetectRenames(oldSnapshot, newScene);

        // Update snapshot with new state
        CaptureSnapshot(scenePath);

        // Fire scene changed event
        var fullPath = GetFullPath(scenePath);
        SceneChanged?.Invoke(this, new GDSceneChangedEventArgs(scenePath, fullPath, newScene));

        // Fire rename event if renames detected
        if (renames.Count > 0)
        {
            _logger.Debug($"Detected {renames.Count} rename(s) in {scenePath}");
            NodeRenameDetected?.Invoke(this, new GDNodeRenameDetectedEventArgs(scenePath, fullPath, renames));
        }
    }

    private List<GDDetectedNodeRename> DetectRenames(GDSceneSnapshot oldSnapshot, GDSceneInfo newScene)
    {
        var renames = new List<GDDetectedNodeRename>();

        // Build lookup for new scene by line number
        var newByLine = newScene.Nodes.ToDictionary(n => n.LineNumber);

        foreach (var (lineNum, oldNode) in oldSnapshot.NodesByLine)
        {
            if (newByLine.TryGetValue(lineNum, out var newNode))
            {
                // Same line exists - check if name changed
                if (oldNode.Name != newNode.Name)
                {
                    renames.Add(new GDDetectedNodeRename
                    {
                        OldName = oldNode.Name,
                        NewName = newNode.Name,
                        LineNumber = lineNum,
                        GDScriptReferenceCount = 0 // Will be set by consumer
                    });

                    _logger.Debug($"Detected rename at line {lineNum}: {oldNode.Name} -> {newNode.Name}");
                }
            }
        }

        return renames;
    }

    private void CaptureSnapshot(string scenePath)
    {
        var scene = GetSceneInfo(scenePath);
        if (scene == null)
        {
            _logger.Debug($"Cannot capture snapshot for {scenePath} - scene not loaded");
            return;
        }

        var snapshot = new GDSceneSnapshot
        {
            ScenePath = scenePath,
            CapturedAt = DateTime.UtcNow,
            NodesByLine = scene.Nodes.ToDictionary(
                n => n.LineNumber,
                n => new GDNodeSnapshotInfo
                {
                    Name = n.Name,
                    Path = n.Path,
                    ParentPath = n.ParentPath,
                    LineNumber = n.LineNumber
                })
        };

        _snapshots[scenePath] = snapshot;
        _logger.Debug($"Captured snapshot for {scenePath} with {snapshot.NodesByLine.Count} nodes");
    }

    /// <summary>
    /// Converts a full filesystem path to a Godot resource path (res://...).
    /// If the path is already a resource path or not within the project, returns it unchanged.
    /// </summary>
    /// <param name="fullPath">Full filesystem path or resource path.</param>
    /// <returns>Resource path (res://...) or the original path if conversion is not possible.</returns>
    public string ToResourcePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return fullPath;

        // Already a resource path
        if (fullPath.StartsWith("res://"))
            return fullPath;

        var projectPath = _projectPath.Replace('\\', '/').TrimEnd('/');
        var normalizedPath = fullPath.Replace('\\', '/');

        if (normalizedPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + normalizedPath.Substring(projectPath.Length).TrimStart('/');
        }

        return fullPath;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisableFileWatcher();
                _sceneCache.Clear();
                _nodePathCache.Clear();
                _snapshots.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Information about a parsed scene file.
/// </summary>
public class GDSceneInfo
{
    public string ScenePath { get; init; } = "";
    public string FullPath { get; set; } = "";
    public IReadOnlyList<GDNodeTypeInfo> Nodes { get; set; } = Array.Empty<GDNodeTypeInfo>();

    /// <summary>
    /// Nodes marked with unique_name_in_owner = true (accessible via %NodeName).
    /// </summary>
    public IReadOnlyList<GDNodeTypeInfo> UniqueNodes { get; set; } = Array.Empty<GDNodeTypeInfo>();

    /// <summary>
    /// Maps script resource paths to the node paths that use them.
    /// </summary>
    public Dictionary<string, string> ScriptToNodePath { get; } = new();

    /// <summary>
    /// Signal connections defined in this scene file.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionInfo> SignalConnections { get; set; } = Array.Empty<GDSignalConnectionInfo>();
}

/// <summary>
/// Information about a signal connection in a scene file.
/// </summary>
public class GDSignalConnectionInfo
{
    /// <summary>
    /// Signal name (e.g., "pressed").
    /// </summary>
    public string SignalName { get; set; } = "";

    /// <summary>
    /// Node path emitting the signal (e.g., "Button").
    /// </summary>
    public string FromNode { get; set; } = "";

    /// <summary>
    /// Node path receiving the signal (e.g., ".").
    /// </summary>
    public string ToNode { get; set; } = "";

    /// <summary>
    /// Method name to call (e.g., "_on_button_pressed").
    /// </summary>
    public string Method { get; set; } = "";

    /// <summary>
    /// Line number (1-based) in the .tscn file.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Type of the node emitting the signal (resolved from scene nodes).
    /// </summary>
    public string? SourceNodeType { get; set; }
}

/// <summary>
/// Information about a node in a scene.
/// </summary>
public class GDNodeTypeInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string NodeType { get; set; } = "Node";
    public string? ScriptPath { get; set; }
    public string? ScriptTypeName { get; set; }

    /// <summary>
    /// Line number (1-based) where this node is defined in the scene file.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Parent path as declared in the scene file.
    /// </summary>
    public string? ParentPath { get; set; }

    /// <summary>
    /// Full path to the scene file on disk.
    /// </summary>
    public string? SceneFullPath { get; set; }

    /// <summary>
    /// The original line content from the scene file.
    /// </summary>
    public string? OriginalLine { get; set; }

    /// <summary>
    /// Whether this node has unique_name_in_owner = true (accessible via %NodeName).
    /// </summary>
    public bool IsUnique { get; set; }
}

/// <summary>
/// Snapshot of a scene's state for tracking changes.
/// Used to detect node renames by comparing before/after states.
/// </summary>
public class GDSceneSnapshot
{
    /// <summary>
    /// Resource path of the scene (e.g., "res://scenes/main.tscn").
    /// </summary>
    public string ScenePath { get; init; } = "";

    /// <summary>
    /// When this snapshot was captured.
    /// </summary>
    public DateTime CapturedAt { get; init; }

    /// <summary>
    /// Nodes indexed by line number (stable identifier across edits).
    /// </summary>
    public Dictionary<int, GDNodeSnapshotInfo> NodesByLine { get; init; } = new();
}

/// <summary>
/// Minimal node information stored in a snapshot.
/// </summary>
public class GDNodeSnapshotInfo
{
    /// <summary>
    /// Node name (e.g., "Player").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Full path in the scene hierarchy (e.g., "Enemy/Spawner").
    /// </summary>
    public string Path { get; init; } = "";

    /// <summary>
    /// Parent path as declared in the scene file.
    /// </summary>
    public string? ParentPath { get; init; }

    /// <summary>
    /// Line number (1-based) in the .tscn file.
    /// </summary>
    public int LineNumber { get; init; }
}
