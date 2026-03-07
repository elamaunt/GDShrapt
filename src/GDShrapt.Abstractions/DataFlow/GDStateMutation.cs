namespace GDShrapt.Abstractions;

public enum GDStateMutationKind
{
    AddChild,
    RemoveChild,
    QueueFree,
    Reparent,
    PropertySet,
    CollisionLayerChange,
    CollisionMaskChange,
    MethodCall
}

/// <summary>
/// Immutable record of a single object state mutation.
/// </summary>
public sealed class GDStateMutation
{
    public GDStateMutationKind Kind { get; }
    public GDFlowLocation Location { get; }

    public string? NodePath { get; }
    public string? NodeType { get; }
    public GDSceneSnapshot? AddedSceneSnapshot { get; }

    public string? PropertyName { get; }
    public GDAbstractValue? NewValue { get; }

    public GDCollisionLayerState? NewCollisionState { get; }

    public bool IsConditional { get; }

    public GDStateMutation(
        GDStateMutationKind kind,
        GDFlowLocation location,
        bool isConditional = false,
        string? nodePath = null,
        string? nodeType = null,
        GDSceneSnapshot? addedSceneSnapshot = null,
        string? propertyName = null,
        GDAbstractValue? newValue = null,
        GDCollisionLayerState? newCollisionState = null)
    {
        Kind = kind;
        Location = location;
        IsConditional = isConditional;
        NodePath = nodePath;
        NodeType = nodeType;
        AddedSceneSnapshot = addedSceneSnapshot;
        PropertyName = propertyName;
        NewValue = newValue;
        NewCollisionState = newCollisionState;
    }

    public override string ToString() => $"[{Kind}] at {Location}";
}
