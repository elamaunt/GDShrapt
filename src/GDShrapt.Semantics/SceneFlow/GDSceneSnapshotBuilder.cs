using System.Collections.Generic;
using GDFlowSceneSnapshot = GDShrapt.Abstractions.GDSceneSnapshot;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Builds immutable data-flow GDSceneSnapshot from GDSceneInfo.
/// Snapshots are cached per scene path.
/// </summary>
internal static class GDSceneSnapshotBuilder
{
    /// <summary>
    /// Builds a data-flow GDSceneSnapshot from a GDSceneInfo.
    /// </summary>
    public static GDFlowSceneSnapshot? Build(GDSceneInfo? sceneInfo)
    {
        if (sceneInfo == null || sceneInfo.Nodes.Count == 0)
            return null;

        var rootNode = sceneInfo.Nodes[0];
        var rootType = rootNode.ScriptTypeName ?? rootNode.NodeType;

        var entries = new List<GDSceneNodeEntry>(sceneInfo.Nodes.Count);
        foreach (var node in sceneInfo.Nodes)
        {
            GDCollisionLayerState? collisionLayers = null;
            if (node.CollisionLayer != 0 || node.CollisionMask != 0)
                collisionLayers = new GDCollisionLayerState(node.CollisionLayer, node.CollisionMask);

            entries.Add(new GDSceneNodeEntry(
                nodePath: node.Path ?? node.Name,
                nodeType: node.NodeType,
                scriptType: node.ScriptTypeName,
                presence: GDSnapshotNodePresence.AlwaysPresent,
                collisionLayers: collisionLayers));
        }

        return new GDFlowSceneSnapshot(sceneInfo.ScenePath, rootType, entries);
    }
}
