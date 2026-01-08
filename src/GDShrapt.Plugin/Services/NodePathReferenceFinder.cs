using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin;

/// <summary>
/// Finds all references to node paths in GDScript files and scene files.
/// Used for the rename node path refactoring feature.
/// </summary>
internal class NodePathReferenceFinder
{
    private readonly GDProjectMap _projectMap;
    private readonly GDSceneTypesProvider _sceneProvider;

    public NodePathReferenceFinder(GDProjectMap projectMap, GDSceneTypesProvider sceneProvider)
    {
        _projectMap = projectMap;
        _sceneProvider = sceneProvider;
    }

    /// <summary>
    /// Finds all GDScript references to a specific node name.
    /// Searches through $NodePath expressions and get_node() calls.
    /// </summary>
    /// <param name="nodeName">The node name to search for.</param>
    /// <returns>List of references found in GDScript files.</returns>
    public IEnumerable<NodePathReference> FindGDScriptReferences(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
            yield break;

        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (scriptMap.Class == null)
                continue;

            // Find all path expressions in the script
            foreach (var pathExpr in scriptMap.Class.AllNodes.OfType<GDGetNodeExpression>())
            {
                var pathList = pathExpr.Path;
                if (pathList == null)
                    continue;

                // Check each segment of the path
                int segmentIndex = 0;
                foreach (var layer in pathList.OfType<GDLayersList>())
                {
                    foreach (var specifier in layer.OfType<GDPathSpecifier>())
                    {
                        if (specifier.Type == GDPathSpecifierType.Identifier &&
                            specifier.IdentifierValue == nodeName)
                        {
                            yield return new NodePathReference
                            {
                                Type = NodePathReference.RefType.GDScript,
                                FilePath = scriptMap.Reference?.FullPath,
                                ResourcePath = scriptMap.Reference?.ResourcePath,
                                LineNumber = specifier.StartLine + 1,
                                NodePath = nodeName,
                                SegmentIndex = segmentIndex,
                                PathSpecifier = specifier,
                                ScriptMap = scriptMap,
                                DisplayContext = GetLineContext(scriptMap, specifier.StartLine)
                            };
                        }
                        segmentIndex++;
                    }
                }
            }

            // Also check get_node() calls with string arguments
            foreach (var call in scriptMap.Class.AllNodes.OfType<GDCallExpression>())
            {
                if (!IsGetNodeCall(call))
                    continue;

                // Check the first argument (should be a string)
                var args = call.Parameters?.ToList();
                if (args == null || args.Count == 0)
                    continue;

                var firstArg = args[0];
                if (firstArg is GDStringExpression strExpr)
                {
                    var pathStr = strExpr.String?.Sequence;
                    if (string.IsNullOrEmpty(pathStr))
                        continue;

                    // Check if node name is in the path
                    var parts = pathStr.Split('/');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i] == nodeName)
                        {
                            yield return new NodePathReference
                            {
                                Type = NodePathReference.RefType.GDScript,
                                FilePath = scriptMap.Reference?.FullPath,
                                ResourcePath = scriptMap.Reference?.ResourcePath,
                                LineNumber = strExpr.StartLine + 1,
                                NodePath = nodeName,
                                SegmentIndex = i,
                                ScriptMap = scriptMap,
                                DisplayContext = GetLineContext(scriptMap, strExpr.StartLine)
                            };
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds all references to a node name in a scene file.
    /// Includes both node name declarations and parent path references.
    /// </summary>
    /// <param name="scenePath">Resource path of the scene.</param>
    /// <param name="nodeName">The node name to search for.</param>
    /// <returns>List of references found in the scene file.</returns>
    public IEnumerable<NodePathReference> FindSceneReferences(string scenePath, string nodeName)
    {
        if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(nodeName))
            yield break;

        var sceneInfo = _sceneProvider.GetSceneInfo(scenePath);
        if (sceneInfo == null)
            yield break;

        // Find the node with this name
        var node = sceneInfo.Nodes.FirstOrDefault(n => n.Name == nodeName);
        if (node != null)
        {
            yield return new NodePathReference
            {
                Type = NodePathReference.RefType.SceneNodeName,
                FilePath = sceneInfo.FullPath,
                ResourcePath = scenePath,
                LineNumber = node.LineNumber,
                NodePath = nodeName,
                SegmentIndex = 0,
                DisplayContext = node.OriginalLine ?? $"[node name=\"{nodeName}\" ...]"
            };
        }

        // Find all nodes that have this node in their parent path
        foreach (var childNode in _sceneProvider.GetNodesWithParentContaining(scenePath, nodeName))
        {
            yield return new NodePathReference
            {
                Type = NodePathReference.RefType.SceneParentPath,
                FilePath = sceneInfo.FullPath,
                ResourcePath = scenePath,
                LineNumber = childNode.LineNumber,
                NodePath = childNode.ParentPath,
                SegmentIndex = GetSegmentIndex(childNode.ParentPath, nodeName),
                DisplayContext = childNode.OriginalLine ?? $"[node ... parent=\"{childNode.ParentPath}\"]"
            };
        }
    }

    /// <summary>
    /// Gets all scenes that use a specific script.
    /// </summary>
    public IEnumerable<string> GetScenesForScript(GDScriptMap scriptMap)
    {
        if (scriptMap?.Reference == null)
            yield break;

        var scriptPath = scriptMap.Reference.ResourcePath;
        if (string.IsNullOrEmpty(scriptPath))
            yield break;

        foreach (var (scenePath, _) in _sceneProvider.GetScenesForScript(scriptPath))
        {
            yield return scenePath;
        }
    }

    /// <summary>
    /// Finds all references (both GDScript and scene) to a node name within specific scenes.
    /// </summary>
    public IEnumerable<NodePathReference> FindAllReferences(IEnumerable<string> scenePaths, string nodeName)
    {
        // Get GDScript references
        foreach (var reference in FindGDScriptReferences(nodeName))
        {
            yield return reference;
        }

        // Get scene references
        foreach (var scenePath in scenePaths.Distinct())
        {
            foreach (var reference in FindSceneReferences(scenePath, nodeName))
            {
                yield return reference;
            }
        }
    }

    private bool IsGetNodeCall(GDCallExpression call)
    {
        // Check for get_node(), get_node_or_null(), find_node()
        if (call.CallerExpression is GDIdentifierExpression idExpr)
        {
            var name = idExpr.Identifier?.Sequence;
            return name == "get_node" || name == "get_node_or_null" || name == "find_node";
        }

        // Check for self.get_node() etc.
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            var name = memberExpr.Identifier?.Sequence;
            return name == "get_node" || name == "get_node_or_null" || name == "find_node";
        }

        return false;
    }

    private int GetSegmentIndex(string path, string nodeName)
    {
        if (string.IsNullOrEmpty(path))
            return 0;

        var parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == nodeName)
                return i;
        }
        return 0;
    }

    private string GetLineContext(GDScriptMap scriptMap, int lineIndex)
    {
        if (scriptMap?.Class == null)
            return "";

        try
        {
            // Get the line from the class source
            var source = scriptMap.Class.ToString();
            var lines = source.Split('\n');
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                return lines[lineIndex].Trim();
            }
        }
        catch
        {
            // Ignore errors
        }

        return "";
    }
}
