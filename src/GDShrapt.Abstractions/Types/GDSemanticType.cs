using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

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
    /// Gets whether this type is an Array type.
    /// </summary>
    public virtual bool IsArray => false;

    /// <summary>
    /// Gets whether this type is a Dictionary type.
    /// </summary>
    public virtual bool IsDictionary => false;

    /// <summary>
    /// Gets whether this type is a container type (Array or Dictionary).
    /// </summary>
    public virtual bool IsContainer => false;

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
    /// Creates a semantic type from an AST type node.
    /// Directly converts GDArrayTypeNode/GDDictionaryTypeNode to GDContainerSemanticType
    /// without string serialization.
    /// </summary>
    public static GDSemanticType FromTypeNode(GDTypeNode? typeNode)
    {
        if (typeNode == null)
            return GDVariantSemanticType.Instance;

        if (typeNode is GDArrayTypeNode arrayNode)
        {
            var elem = FromTypeNode(arrayNode.InnerType);
            return new GDContainerSemanticType(isDictionary: false, elementType: elem);
        }

        if (typeNode is GDDictionaryTypeNode dictNode)
        {
            var key = FromTypeNode(dictNode.KeyType);
            var val = FromTypeNode(dictNode.ValueType);
            return new GDContainerSemanticType(isDictionary: true, elementType: val, keyType: key);
        }

        var name = typeNode.BuildName();
        if (string.IsNullOrEmpty(name) || name == "Variant")
            return GDVariantSemanticType.Instance;
        if (name == "null")
            return GDNullSemanticType.Instance;

        return new GDSimpleSemanticType(name, typeNode);
    }

    /// <summary>
    /// Converts a runtime type name string to GDSemanticType.
    /// Use only at the boundary with IGDRuntimeProvider / TypesMap / GDFlowState data.
    /// For AST types, use FromTypeNode() instead.
    /// </summary>
    public static GDSemanticType FromRuntimeTypeName(string? typeName)
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
                .Select(p => FromRuntimeTypeName(p.Trim()))
                .ToList();
            return new GDUnionSemanticType(types);
        }

        // Parse container types structurally
        if (typeName.StartsWith("Array[") && typeName.EndsWith("]"))
        {
            var inner = typeName.Substring(6, typeName.Length - 7);
            return new GDContainerSemanticType(isDictionary: false,
                elementType: FromRuntimeTypeName(inner));
        }

        if (typeName.StartsWith("Dictionary[") && typeName.EndsWith("]"))
        {
            var inner = typeName.Substring(11, typeName.Length - 12);
            var commaIndex = GDGenericTypeHelper.FindTopLevelComma(inner);
            if (commaIndex > 0)
            {
                var key = inner.Substring(0, commaIndex).Trim();
                var val = inner.Substring(commaIndex + 1).Trim();
                return new GDContainerSemanticType(isDictionary: true,
                    elementType: FromRuntimeTypeName(val),
                    keyType: FromRuntimeTypeName(key));
            }
        }

        return new GDSimpleSemanticType(typeName);
    }

    /// <summary>
    /// Infers a semantic type from an initializer expression using AST pattern matching.
    /// Handles literals (numbers, strings, bools) and container constructors (arrays, dictionaries).
    /// Returns Variant for unrecognized expressions.
    /// </summary>
    public static GDSemanticType InferFromInitializer(GDExpression? initializer)
    {
        return initializer switch
        {
            null => GDVariantSemanticType.Instance,
            GDArrayInitializerExpression => new GDSimpleSemanticType("Array"),
            GDDictionaryInitializerExpression => new GDSimpleSemanticType("Dictionary"),
            GDNumberExpression num => num.Number?.ResolveNumberType() switch
            {
                GDNumberType.LongDecimal or GDNumberType.LongBinary or GDNumberType.LongHexadecimal
                    => new GDSimpleSemanticType("int"),
                GDNumberType.Double => new GDSimpleSemanticType("float"),
                _ => GDVariantSemanticType.Instance
            },
            GDStringExpression => new GDSimpleSemanticType("String"),
            GDBoolExpression => new GDSimpleSemanticType("bool"),
            _ => GDVariantSemanticType.Instance
        };
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
