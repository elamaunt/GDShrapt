using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a type conflict detected in data flow analysis.
/// </summary>
public sealed class GDTypeConflict
{
    public GDTypeConflictKind Kind { get; }
    public GDSemanticType FlowingType { get; }
    public GDSemanticType DeclaredType { get; }
    public GDTypeOrigin Origin { get; }
    public string Message { get; }

    public GDTypeConflict(
        GDTypeConflictKind kind,
        GDSemanticType flowingType,
        GDSemanticType declaredType,
        GDTypeOrigin origin,
        string message)
    {
        Kind = kind;
        FlowingType = flowingType;
        DeclaredType = declaredType;
        Origin = origin;
        Message = message;
    }

    public override string ToString() => $"[{Kind}] {Message}";
}
