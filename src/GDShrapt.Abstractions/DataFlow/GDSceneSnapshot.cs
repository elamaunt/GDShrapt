using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Node presence in a scene snapshot.
/// </summary>
public enum GDSnapshotNodePresence
{
    AlwaysPresent,
    ConditionallyPresent,
    MayBeAbsent,
    Unknown
}

/// <summary>
/// Immutable snapshot of a scene's node hierarchy.
/// Built from GDSceneInfo at instantiate() time.
/// </summary>
public sealed class GDSceneSnapshot
{
    public string ScenePath { get; }
    public string RootType { get; }
    public IReadOnlyList<GDSceneNodeEntry> Nodes { get; }

    public GDSceneSnapshot(string scenePath, string rootType, IReadOnlyList<GDSceneNodeEntry> nodes)
    {
        ScenePath = scenePath;
        RootType = rootType;
        Nodes = nodes;
    }

    public override string ToString() => $"Scene({ScenePath}, {Nodes.Count} nodes)";
}

/// <summary>
/// A single node in a scene snapshot.
/// </summary>
public sealed class GDSceneNodeEntry
{
    public string NodePath { get; }
    public string NodeType { get; }
    public string? ScriptType { get; }
    public GDSnapshotNodePresence Presence { get; }
    public GDCollisionLayerState? CollisionLayers { get; }
    public IReadOnlyDictionary<string, GDAbstractValue>? Properties { get; }

    public GDSceneNodeEntry(
        string nodePath,
        string nodeType,
        string? scriptType = null,
        GDSnapshotNodePresence presence = GDSnapshotNodePresence.AlwaysPresent,
        GDCollisionLayerState? collisionLayers = null,
        IReadOnlyDictionary<string, GDAbstractValue>? properties = null)
    {
        NodePath = nodePath;
        NodeType = nodeType;
        ScriptType = scriptType;
        Presence = presence;
        CollisionLayers = collisionLayers;
        Properties = properties;
    }

    public override string ToString() => $"{NodePath}: {ScriptType ?? NodeType} ({Presence})";
}
