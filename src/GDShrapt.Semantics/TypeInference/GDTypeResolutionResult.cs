using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of type resolution.
/// </summary>
public class GDTypeResolutionResult
{
    public string TypeName { get; init; } = "Variant";
    public GDTypeNode? TypeNode { get; init; }
    public bool IsResolved { get; init; }
    public GDTypeSource Source { get; init; } = GDTypeSource.Unknown;

    public static GDTypeResolutionResult Unknown() => new()
    {
        TypeName = "Variant",
        IsResolved = false,
        Source = GDTypeSource.Unknown
    };
}

/// <summary>
/// Source of type information.
/// </summary>
public enum GDTypeSource
{
    Unknown,
    BuiltIn,
    GodotApi,
    Project,
    Scene,
    Inferred
}
