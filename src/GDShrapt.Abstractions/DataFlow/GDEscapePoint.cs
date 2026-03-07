namespace GDShrapt.Abstractions;

public enum GDEscapeKind
{
    PassedToUnresolvedFunction,
    ReturnedToUnknownCaller,
    PassedToNativeCode,
    ReflectionAccess
}

/// <summary>
/// Records where data escaped from analysis scope into unresolvable code.
/// </summary>
public sealed class GDEscapePoint
{
    public GDEscapeKind Kind { get; }
    public GDFlowLocation Location { get; }
    public string? Description { get; }

    public GDEscapePoint(GDEscapeKind kind, GDFlowLocation location, string? description = null)
    {
        Kind = kind;
        Location = location;
        Description = description;
    }

    public override string ToString() => $"[{Kind}] {Description ?? ""} at {Location}";
}
