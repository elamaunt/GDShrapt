namespace GDShrapt.Abstractions;

/// <summary>
/// Immutable origin record: where a type came from in the data flow.
/// Carries optional attached value and object state.
/// </summary>
public sealed class GDTypeOrigin
{
    public GDTypeOriginKind Kind { get; }
    public GDTypeOriginConfidence Confidence { get; }
    public GDFlowLocation Location { get; }
    public GDTypeOrigin? Upstream { get; }
    public string? Description { get; }
    public GDAbstractValue? Value { get; }
    public GDObjectState? ObjectState { get; }
    public GDEscapePoint? EscapePoint { get; }

    public GDTypeOrigin(
        GDTypeOriginKind kind,
        GDTypeOriginConfidence confidence,
        GDFlowLocation location,
        GDTypeOrigin? upstream = null,
        string? description = null,
        GDAbstractValue? value = null,
        GDObjectState? objectState = null,
        GDEscapePoint? escapePoint = null)
    {
        Kind = kind;
        Confidence = confidence;
        Location = location;
        Upstream = upstream;
        Description = description;
        Value = value;
        ObjectState = objectState;
        EscapePoint = escapePoint;
    }

    public override string ToString()
    {
        var desc = Description ?? Kind.ToString();
        return $"[{Confidence}] {desc} at {Location}";
    }
}
