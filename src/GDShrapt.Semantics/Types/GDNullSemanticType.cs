using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

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
        // null is assignable to Variant
        if (other.IsVariant)
            return true;

        // null is assignable to nullable types
        if (other.IsNullable)
            return true;

        // null is assignable to union types that contain null
        if (other is GDUnionSemanticType union)
            return union.Types.Any(t => t is GDNullSemanticType);

        // null is assignable to reference types (Object hierarchy)
        if (other is GDSimpleSemanticType simple && provider != null)
        {
            // Check if type inherits from Object (reference type)
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
