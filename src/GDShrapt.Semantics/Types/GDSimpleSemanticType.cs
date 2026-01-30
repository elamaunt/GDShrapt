using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a simple (non-union) type like int, String, Node, Array[int], etc.
/// </summary>
public class GDSimpleSemanticType : GDSemanticType
{
    /// <summary>
    /// Gets the type name.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the optional AST node representing this type.
    /// </summary>
    public GDTypeNode? AstNode { get; }

    public override string DisplayName => TypeName;

    public GDSimpleSemanticType(string typeName, GDTypeNode? astNode = null)
    {
        TypeName = typeName ?? "Variant";
        AstNode = astNode;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        // Anything is assignable to Variant
        if (other.IsVariant)
            return true;

        // Same type
        if (other is GDSimpleSemanticType simple && simple.TypeName == TypeName)
            return true;

        // Check union - if this type is one of the union members
        if (other is GDUnionSemanticType union)
            return union.Types.Any(t => IsAssignableTo(t, provider));

        // Check inheritance hierarchy
        if (provider != null && other is GDSimpleSemanticType targetSimple)
        {
            return IsSubtypeOf(TypeName, targetSimple.TypeName, provider);
        }

        return false;
    }

    private static bool IsSubtypeOf(string subType, string superType, IGDRuntimeProvider provider)
    {
        if (subType == superType)
            return true;

        var current = subType;
        while (!string.IsNullOrEmpty(current))
        {
            if (current == superType)
                return true;

            current = provider.GetBaseType(current);
        }

        return false;
    }

    public override GDTypeNode? ToTypeNode()
    {
        if (AstNode != null)
            return AstNode;

        // Cannot create GDTypeNode for generic types with union parameters
        if (TypeName.Contains('|'))
            return null;

        // For simple types, we could potentially create a GDTypeNode
        // but this requires proper AST construction which is complex
        return null;
    }

    public override bool Equals(object? obj)
    {
        return obj is GDSimpleSemanticType other && TypeName == other.TypeName;
    }

    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }
}
