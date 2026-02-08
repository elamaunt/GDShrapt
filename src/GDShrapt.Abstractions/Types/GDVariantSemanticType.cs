namespace GDShrapt.Abstractions;

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
        if (other.IsVariant)
            return true;

        return true;
    }
}
