using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Finds all references to node paths in GDScript files and scene files.
/// Used for the rename node path refactoring feature.
/// </summary>
public class GDNodePathReferenceFinder
{
    private readonly GDScriptProject _project;
    private readonly GDSceneTypesProvider? _sceneProvider;

    public GDNodePathReferenceFinder(GDScriptProject project)
    {
        _project = project;
        _sceneProvider = project.SceneTypesProvider;
    }

    /// <summary>
    /// Finds all GDScript references to a specific node name.
    /// Searches through $NodePath expressions and get_node() calls.
    /// </summary>
    /// <param name="nodeName">The node name to search for.</param>
    /// <returns>List of references found in GDScript files.</returns>
    public IEnumerable<GDNodePathReference> FindGDScriptReferences(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
            yield break;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.Class == null)
                continue;

            // Find all path expressions in the script ($Node)
            foreach (var pathExpr in script.Class.AllNodes.OfType<GDGetNodeExpression>())
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
                            yield return new GDNodePathReference
                            {
                                Type = GDNodePathReference.RefType.GDScript,
                                FilePath = script.Reference.FullPath,
                                ResourcePath = script.Reference.ResourcePath,
                                LineNumber = specifier.StartLine + 1,
                                NodePath = nodeName,
                                SegmentIndex = segmentIndex,
                                PathSpecifier = specifier,
                                ScriptReference = script.Reference,
                                DisplayContext = GetLineContext(script, specifier.StartLine)
                            };
                        }
                        segmentIndex++;
                    }
                }
            }

            // Also check get_node() calls with string arguments
            foreach (var call in script.Class.AllNodes.OfType<GDCallExpression>())
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
                            yield return new GDNodePathReference
                            {
                                Type = GDNodePathReference.RefType.GDScript,
                                FilePath = script.Reference.FullPath,
                                ResourcePath = script.Reference.ResourcePath,
                                LineNumber = strExpr.StartLine + 1,
                                NodePath = nodeName,
                                SegmentIndex = i,
                                ScriptReference = script.Reference,
                                DisplayContext = GetLineContext(script, strExpr.StartLine)
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
    public IEnumerable<GDNodePathReference> FindSceneReferences(string scenePath, string nodeName)
    {
        if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(nodeName) || _sceneProvider == null)
            yield break;

        var sceneInfo = _sceneProvider.GetSceneInfo(scenePath);
        if (sceneInfo == null)
            yield break;

        // Find the node with this name
        var node = sceneInfo.Nodes.FirstOrDefault(n => n.Name == nodeName);
        if (node != null)
        {
            yield return new GDNodePathReference
            {
                Type = GDNodePathReference.RefType.SceneNodeName,
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
            yield return new GDNodePathReference
            {
                Type = GDNodePathReference.RefType.SceneParentPath,
                FilePath = sceneInfo.FullPath,
                ResourcePath = scenePath,
                LineNumber = childNode.LineNumber,
                NodePath = childNode.ParentPath ?? "",
                SegmentIndex = GetSegmentIndex(childNode.ParentPath, nodeName),
                DisplayContext = childNode.OriginalLine ?? $"[node ... parent=\"{childNode.ParentPath}\"]"
            };
        }
    }

    /// <summary>
    /// Gets all scenes that use a specific script.
    /// </summary>
    public IEnumerable<string> GetScenesForScript(GDScriptFile script)
    {
        if (script?.Reference == null || _sceneProvider == null)
            yield break;

        var scriptPath = script.Reference.ResourcePath;
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
    public IEnumerable<GDNodePathReference> FindAllReferences(IEnumerable<string> scenePaths, string nodeName)
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

    /// <summary>
    /// Finds all references (both GDScript and scene) to a node name.
    /// </summary>
    public IEnumerable<GDNodePathReference> FindAllReferences(string nodeName)
    {
        // Get GDScript references
        foreach (var reference in FindGDScriptReferences(nodeName))
        {
            yield return reference;
        }

        if (_sceneProvider == null)
            yield break;

        // Get scene references from all scenes
        foreach (var scene in _sceneProvider.AllScenes)
        {
            foreach (var reference in FindSceneReferences(scene.ScenePath, nodeName))
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

    private int GetSegmentIndex(string? path, string nodeName)
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

    private string GetLineContext(GDScriptFile script, int lineIndex)
    {
        if (script?.Class == null)
            return "";

        try
        {
            // Get the line from the class source
            var source = script.Class.ToString();
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
