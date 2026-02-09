using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Builds predicted runtime hierarchy for a scene with confidence levels.
/// </summary>
internal class GDSceneHierarchyPredictor
{
    private readonly GDSceneFlowGraph _graph;
    private readonly GDSceneTypesProvider? _sceneProvider;
    private readonly GDSceneFlowOptions _options;

    public GDSceneHierarchyPredictor(GDSceneFlowGraph graph, GDSceneTypesProvider? sceneProvider, GDSceneFlowOptions? options = null)
    {
        _graph = graph;
        _sceneProvider = sceneProvider;
        _options = options ?? new GDSceneFlowOptions();
    }

    public GDPredictedHierarchy Predict(string scenePath)
    {
        var visited = new HashSet<string>();
        var root = BuildPredictedTree(scenePath, ".", visited, 0);

        return new GDPredictedHierarchy
        {
            ScenePath = scenePath,
            Root = root
        };
    }

    public GDNodePresencePrediction CheckNodePath(string scenePath, string nodePath)
    {
        var hierarchy = Predict(scenePath);
        if (hierarchy.Root == null)
            return GDNodePresencePrediction.UnknownPath;

        var node = FindNode(hierarchy.Root, nodePath);
        return node?.Presence ?? GDNodePresencePrediction.UnknownPath;
    }

    public IReadOnlyList<GDPredictedNode> GetPossibleChildren(string scenePath, string parentNodePath)
    {
        var hierarchy = Predict(scenePath);
        if (hierarchy.Root == null)
            return Array.Empty<GDPredictedNode>();

        var parent = FindNode(hierarchy.Root, parentNodePath);
        return parent?.Children ?? (IReadOnlyList<GDPredictedNode>)Array.Empty<GDPredictedNode>();
    }

    private GDPredictedNode? BuildPredictedTree(string scenePath, string nodePath, HashSet<string> visited, int depth)
    {
        if (depth > _options.MaxSubSceneDepth)
            return null;

        var sceneNode = _graph.GetScene(scenePath);
        if (sceneNode?.SceneInfo == null)
            return null;

        // Find the node in the scene
        var staticNode = sceneNode.SceneInfo.Nodes.FirstOrDefault(n => n.Path == nodePath);
        if (staticNode == null && nodePath == ".")
        {
            // Root node
            staticNode = sceneNode.SceneInfo.Nodes.FirstOrDefault();
        }

        if (staticNode == null)
            return null;

        var presence = DeterminePresence(staticNode, sceneNode);

        var predicted = new GDPredictedNode
        {
            Name = staticNode.Name,
            Path = staticNode.Path,
            NodeType = staticNode.NodeType,
            ScriptTypeName = staticNode.ScriptTypeName,
            Presence = presence,
            IsSubSceneInstance = staticNode.IsSubSceneInstance,
            SubScenePath = staticNode.SubScenePath
        };

        // Add static children
        var children = sceneNode.SceneInfo.Nodes
            .Where(n => IsDirectChild(n, nodePath))
            .ToList();

        foreach (var child in children)
        {
            var childPredicted = BuildPredictedTree(scenePath, child.Path, visited, depth);
            if (childPredicted != null)
                predicted.Children.Add(childPredicted);
        }

        // Expand sub-scene if this is a sub-scene instance
        if (_options.ExpandSubScenes && staticNode.IsSubSceneInstance && !string.IsNullOrEmpty(staticNode.SubScenePath))
        {
            if (!visited.Contains(staticNode.SubScenePath))
            {
                visited.Add(staticNode.SubScenePath);

                // Load sub-scene if needed
                if (_sceneProvider != null && _sceneProvider.GetSceneInfo(staticNode.SubScenePath) == null)
                    _sceneProvider.LoadScene(staticNode.SubScenePath);

                var subSceneRoot = BuildPredictedTree(staticNode.SubScenePath, ".", visited, depth + 1);
                if (subSceneRoot != null)
                {
                    // Merge sub-scene children into this node
                    foreach (var subChild in subSceneRoot.Children)
                        predicted.Children.Add(subChild);
                }

                visited.Remove(staticNode.SubScenePath);
            }
        }

        // Add runtime nodes if enabled
        if (_options.IncludeRuntimeNodes)
        {
            AddRuntimeChildren(predicted, sceneNode, nodePath);
        }

        return predicted;
    }

    private GDNodePresencePrediction DeterminePresence(GDNodeTypeInfo node, GDSceneFlowNode sceneNode)
    {
        // Check if any runtime node removal targets this node
        var hasRemoval = sceneNode.RuntimeNodes.Any(rn =>
            rn.ParentNodePath == node.Name ||
            rn.ParentNodePath == node.Path);

        if (hasRemoval)
        {
            return new GDNodePresencePrediction
            {
                Status = GDNodePresenceStatus.MayBeAbsent,
                Confidence = GDTypeConfidence.Medium,
                Reason = "Node may be removed via queue_free/remove_child"
            };
        }

        return GDNodePresencePrediction.AlwaysPresentFromScene;
    }

    private void AddRuntimeChildren(GDPredictedNode parent, GDSceneFlowNode sceneNode, string nodePath)
    {
        foreach (var runtimeNode in sceneNode.RuntimeNodes)
        {
            // Skip removal entries
            if (runtimeNode.NodeType is "queue_free" or "remove_child" or "free")
                continue;

            if (runtimeNode.Confidence < _options.MinimumConfidence)
                continue;

            var presence = runtimeNode.IsConditional
                ? new GDNodePresencePrediction
                {
                    Status = GDNodePresenceStatus.ConditionallyPresent,
                    Confidence = runtimeNode.Confidence,
                    Reason = "Node added conditionally in code"
                }
                : new GDNodePresencePrediction
                {
                    Status = GDNodePresenceStatus.ConditionallyPresent,
                    Confidence = runtimeNode.Confidence,
                    Reason = "Node added dynamically in code"
                };

            var scenePath = runtimeNode.ScenePath;
            string nodeType = runtimeNode.NodeType ?? "Node";
            string? scriptTypeName = runtimeNode.ScriptTypeName;

            // Resolve root type from scene if available
            if (!string.IsNullOrEmpty(scenePath) && _sceneProvider != null)
            {
                if (_sceneProvider.GetSceneInfo(scenePath) == null)
                    _sceneProvider.LoadScene(scenePath);
                var rootType = _sceneProvider.GetRootNodeType(scenePath);
                if (!string.IsNullOrEmpty(rootType))
                    nodeType = rootType;
            }

            parent.Children.Add(new GDPredictedNode
            {
                Name = $"[runtime:{runtimeNode.SourceFile}:{runtimeNode.LineNumber}]",
                Path = $"{(nodePath == "." ? "" : nodePath + "/")}[runtime]",
                NodeType = nodeType,
                ScriptTypeName = scriptTypeName,
                Presence = presence,
                IsSubSceneInstance = !string.IsNullOrEmpty(scenePath),
                SubScenePath = scenePath
            });
        }
    }

    private static bool IsDirectChild(GDNodeTypeInfo node, string parentPath)
    {
        if (parentPath == ".")
            return node.ParentPath == "." || (string.IsNullOrEmpty(node.ParentPath) && node.Path != ".");

        return node.ParentPath == parentPath;
    }

    private static GDPredictedNode? FindNode(GDPredictedNode root, string path)
    {
        if (root.Path == path)
            return root;

        foreach (var child in root.Children)
        {
            var found = FindNode(child, path);
            if (found != null)
                return found;
        }

        return null;
    }
}
