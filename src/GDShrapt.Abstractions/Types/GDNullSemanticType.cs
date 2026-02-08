using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents the null type.
/// Singleton instance.
/// </summary>
public sealed class GDNullSemanticType : GDSemanticType
{
    /// <summary>
    /// Gets the singleton instance of null type.
    /// </summary>
    public static GDNullSemanticType Instance { get; } = new();

    private GDNullSemanticType() { }

    public override string DisplayName => "null";

    public override bool IsNullable => true;

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other.IsVariant)
            return true;

        if (other.IsNullable)
            return true;

        if (other is GDUnionSemanticType union)
            return union.Types.Any(t => t is GDNullSemanticType);

        if (other is GDSimpleSemanticType simple && provider != null)
        {
            var baseType = simple.TypeName;
            while (!string.IsNullOrEmpty(baseType))
            {
                if (baseType == "Object" || baseType == "RefCounted" || baseType == "Resource" || baseType == "Node")
                    return true;
                baseType = provider.GetBaseType(baseType);
            }
        }

        return false;
    }
}
