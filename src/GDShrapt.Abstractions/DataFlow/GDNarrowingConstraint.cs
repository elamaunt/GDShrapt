namespace GDShrapt.Abstractions;

public enum GDNarrowingKind
{
    IsCheck,
    NullCheck,
    AsCast,
    TypeOfCheck,
    AssertCheck,
    MatchPattern,
    HasMethodCheck,
    HasPropertyCheck,
    HasSignalCheck
}

/// <summary>
/// Records an active type narrowing constraint at a program point.
/// </summary>
public sealed class GDNarrowingConstraint
{
    public GDNarrowingKind Kind { get; }
    public GDSemanticType NarrowedToType { get; }
    public GDFlowLocation Location { get; }

    public GDNarrowingConstraint(GDNarrowingKind kind, GDSemanticType narrowedToType, GDFlowLocation location)
    {
        Kind = kind;
        NarrowedToType = narrowedToType;
        Location = location;
    }

    public override string ToString() => $"[{Kind}] → {NarrowedToType.DisplayName} at {Location}";
}
