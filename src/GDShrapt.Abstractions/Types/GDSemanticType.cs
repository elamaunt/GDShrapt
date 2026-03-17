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
    /// Gets whether this type is a Callable type.
    /// </summary>
    public virtual bool IsCallable => false;

    /// <summary>
    /// Gets whether this type is a Signal type.
    /// </summary>
    public virtual bool IsSignal => false;

    /// <summary>
    /// Gets whether this type is a union type.
    /// </summary>
    public virtual bool IsUnion => false;

    /// <summary>
    /// Gets whether this type is a numeric type (int or float).
    /// </summary>
    public virtual bool IsNumeric => false;

    /// <summary>
    /// Gets whether this type is a string-like type (String, StringName, NodePath).
    /// </summary>
    public virtual bool IsString => false;

    /// <summary>
    /// Gets whether this type is a bool type.
    /// </summary>
    public virtual bool IsBool => false;

    /// <summary>
    /// Gets whether this type is the null type.
    /// </summary>
    public virtual bool IsNull => false;

    /// <summary>
    /// Gets whether this type is a GDScript value type (never null).
    /// Includes all built-in types: numeric, string, bool, containers, vectors, etc.
    /// Object subclasses return false (they can be null).
    /// </summary>
    public virtual bool IsValueType => false;

    /// <summary>
    /// Checks whether this type matches a given type name.
    /// </summary>
    public virtual bool IsType(string typeName) => DisplayName == typeName;

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

        // Check for union types (e.g., "int | String")
        if (GDGenericTypeHelper.IsUnionType(typeName))
        {
            var parts = GDGenericTypeHelper.SplitUnionTypes(typeName);
            var types = parts
                .Select(p => FromRuntimeTypeName(p))
                .ToList();
            return new GDUnionSemanticType(types);
        }

        // Parse container types structurally
        if (GDGenericTypeHelper.IsGenericArrayType(typeName))
        {
            var inner = GDGenericTypeHelper.ExtractArrayElementType(typeName);
            return new GDContainerSemanticType(isDictionary: false,
                elementType: FromRuntimeTypeName(inner));
        }

        if (GDGenericTypeHelper.IsGenericDictionaryType(typeName))
        {
            var (key, val) = GDGenericTypeHelper.ExtractDictionaryTypes(typeName);
            if (!string.IsNullOrEmpty(key))
            {
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
            GDCallExpression call => InferFromCallExpression(call),
            _ => GDVariantSemanticType.Instance
        };
    }

    private static GDSemanticType InferFromCallExpression(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && char.IsUpper(name[0]))
                return new GDSimpleSemanticType(name);
        }

        return GDVariantSemanticType.Instance;
    }

    /// <summary>
    /// Creates a union of two types.
    /// If both types are the same, returns the single type.
    /// </summary>
    public static GDSemanticType CreateUnion(GDSemanticType type1, GDSemanticType type2)
    {
        if (type1.Equals(type2))
            return type1;

        if (type1.IsVariant)
            return type1;
        if (type2.IsVariant)
            return type2;

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

        var distinct = types
            .GroupBy(t => t)
            .Select(g => g.First())
            .ToList();

        if (distinct.Count == 1)
            return distinct[0];

        return new GDUnionSemanticType(distinct);
    }
}
