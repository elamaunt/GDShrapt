using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Base class for semantic type representation.
/// Unlike GDTypeNode (AST node), this represents types in the semantic model
/// and can express types that don't have direct AST representation (unions, callables with return types).
/// </summary>
public abstract class GDSemanticType
{
    /// <summary>
    /// Gets the display name for this type.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets whether this type is assignable to another type.
    /// </summary>
    /// <param name="other">The target type to check assignment to.</param>
    /// <param name="provider">Runtime provider for type hierarchy information.</param>
    /// <returns>True if this type can be assigned to the target type.</returns>
    public abstract bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider);

    /// <summary>
    /// Gets whether this type is a Variant type.
    /// </summary>
    public virtual bool IsVariant => false;

    /// <summary>
    /// Gets whether this type is a nullable type (can be null).
    /// </summary>
    public virtual bool IsNullable => false;

    /// <summary>
    /// Gets whether this type is a union type.
    /// </summary>
    public virtual bool IsUnion => false;

    /// <summary>
    /// Creates a GDTypeNode representation of this type, if possible.
    /// Returns null for types that cannot be represented as AST nodes (unions).
    /// </summary>
    public virtual GDTypeNode? ToTypeNode() => null;

    public override string ToString() => DisplayName;

    /// <summary>
    /// Creates a semantic type from a type name string.
    /// </summary>
    public static GDSemanticType FromTypeName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return GDVariantSemanticType.Instance;

        if (typeName == "null")
            return GDNullSemanticType.Instance;

        // Check for union types (e.g., "int|String")
        if (typeName.Contains('|'))
        {
            var parts = typeName.Split('|');
            var types = parts
                .Select(p => FromTypeName(p.Trim()))
                .ToList();
            return new GDUnionSemanticType(types);
        }

        return new GDSimpleSemanticType(typeName);
    }

    /// <summary>
    /// Creates a union of two types.
    /// If both types are the same, returns the single type.
    /// </summary>
    public static GDSemanticType CreateUnion(GDSemanticType type1, GDSemanticType type2)
    {
        if (type1.DisplayName == type2.DisplayName)
            return type1;

        // If either is Variant, result is Variant
        if (type1.IsVariant || type2.IsVariant)
            return GDVariantSemanticType.Instance;

        var types = new List<GDSemanticType>();

        // Flatten nested unions
        if (type1 is GDUnionSemanticType union1)
            types.AddRange(union1.Types);
        else
            types.Add(type1);

        if (type2 is GDUnionSemanticType union2)
            types.AddRange(union2.Types);
        else
            types.Add(type2);

        // Remove duplicates by display name
        var distinct = types
            .GroupBy(t => t.DisplayName)
            .Select(g => g.First())
            .ToList();

        if (distinct.Count == 1)
            return distinct[0];

        return new GDUnionSemanticType(distinct);
    }
}
