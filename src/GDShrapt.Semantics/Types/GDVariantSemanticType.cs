using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents the Variant type (any type).
/// Singleton instance.
/// </summary>
public sealed class GDVariantSemanticType : GDSemanticType
{
    /// <summary>
    /// Gets the singleton instance of Variant type.
    /// </summary>
    public static GDVariantSemanticType Instance { get; } = new();

    private GDVariantSemanticType() { }

    public override string DisplayName => "Variant";

    public override bool IsVariant => true;

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        // Variant is assignable to Variant
        if (other.IsVariant)
            return true;

        // Variant can potentially hold any value, so technically
        // it's assignable to any type (runtime check)
        // For static analysis, we're conservative and say true
        return true;
    }
}
