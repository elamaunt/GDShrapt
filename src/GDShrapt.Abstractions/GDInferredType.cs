using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a type inferred during semantic analysis.
/// Unlike GDTypeNode (AST parser class), this is used for analysis results
/// and supports union types like Array[int|String].
/// </summary>
public class GDInferredType
{
    /// <summary>
    /// The base type name (e.g., "int", "Array", "Dictionary", "Node").
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Element type for generic containers (Array, PackedArray).
    /// For Array[int|String], this contains the union "int|String".
    /// </summary>
    public GDUnionType? ElementType { get; set; }

    /// <summary>
    /// Key type for Dictionary.
    /// </summary>
    public GDUnionType? KeyType { get; set; }

    /// <summary>
    /// Value type for Dictionary.
    /// </summary>
    public GDUnionType? ValueType { get; set; }

    /// <summary>
    /// Whether this type is an array type.
    /// </summary>
    public bool IsArray => TypeName == "Array" || IsPackedArray;

    /// <summary>
    /// Whether this is a packed array type.
    /// </summary>
    public bool IsPackedArray => TypeName?.StartsWith("Packed") == true && TypeName?.EndsWith("Array") == true;

    /// <summary>
    /// Whether this type is a Dictionary type.
    /// </summary>
    public bool IsDictionary => TypeName == "Dictionary";

    /// <summary>
    /// Whether this type is a generic container (has element type parameter).
    /// </summary>
    public bool IsGenericContainer => IsArray || IsDictionary;

    /// <summary>
    /// Whether this is a union type (multiple possible types).
    /// </summary>
    public bool IsUnion => ElementType?.IsUnion == true;

    /// <summary>
    /// Whether this type represents a numeric type (int or float).
    /// </summary>
    public bool IsNumeric => TypeName == "int" || TypeName == "float";

    /// <summary>
    /// Gets the full type name including generic parameters.
    /// </summary>
    public string FullTypeName
    {
        get
        {
            if (string.IsNullOrEmpty(TypeName))
                return "Variant";

            if (IsDictionary)
            {
                var keyType = KeyType?.UnionTypeName ?? "Variant";
                var valueType = ValueType?.UnionTypeName ?? "Variant";
                if (keyType == "Variant" && valueType == "Variant")
                    return "Dictionary";
                return $"Dictionary[{keyType}, {valueType}]";
            }

            if (IsArray && ElementType != null && !ElementType.IsEmpty)
            {
                var elemType = ElementType.UnionTypeName;
                if (elemType != "Variant")
                    return $"Array[{elemType}]";
            }

            return TypeName;
        }
    }

    /// <summary>
    /// Creates a simple type (non-generic).
    /// </summary>
    public static GDInferredType Simple(string typeName)
    {
        return new GDInferredType { TypeName = typeName };
    }

    /// <summary>
    /// Creates an array type with the given element type.
    /// </summary>
    public static GDInferredType Array(string elementType = null)
    {
        var result = new GDInferredType { TypeName = "Array" };
        if (!string.IsNullOrEmpty(elementType))
        {
            result.ElementType = new GDUnionType();
            result.ElementType.AddType(elementType);
        }
        return result;
    }

    /// <summary>
    /// Creates an array type with a union element type.
    /// </summary>
    public static GDInferredType ArrayWithUnion(IEnumerable<string> elementTypes)
    {
        var result = new GDInferredType
        {
            TypeName = "Array",
            ElementType = new GDUnionType()
        };
        foreach (var t in elementTypes)
            result.ElementType.AddType(t);
        return result;
    }

    /// <summary>
    /// Creates an array type with a union element type.
    /// </summary>
    public static GDInferredType ArrayWithUnion(params string[] elementTypes)
    {
        return ArrayWithUnion((IEnumerable<string>)elementTypes);
    }

    /// <summary>
    /// Creates a Dictionary type.
    /// </summary>
    public static GDInferredType Dictionary(string keyType = null, string valueType = null)
    {
        var result = new GDInferredType { TypeName = "Dictionary" };
        if (!string.IsNullOrEmpty(keyType))
        {
            result.KeyType = new GDUnionType();
            result.KeyType.AddType(keyType);
        }
        if (!string.IsNullOrEmpty(valueType))
        {
            result.ValueType = new GDUnionType();
            result.ValueType.AddType(valueType);
        }
        return result;
    }

    /// <summary>
    /// Combines two array types. Used for array addition (Array[A] + Array[B]).
    /// </summary>
    public static GDInferredType CombineArrays(GDInferredType left, GDInferredType right)
    {
        if (left == null || right == null)
            return GDInferredType.Array();

        if (!left.IsArray || !right.IsArray)
            return null;

        var leftElem = left.ElementType;
        var rightElem = right.ElementType;

        // Both untyped → untyped
        if ((leftElem == null || leftElem.IsEmpty) && (rightElem == null || rightElem.IsEmpty))
            return GDInferredType.Array();

        // One untyped → untyped
        if (leftElem == null || leftElem.IsEmpty || rightElem == null || rightElem.IsEmpty)
            return GDInferredType.Array();

        // Same single type → preserve
        if (leftElem.IsSingleType && rightElem.IsSingleType &&
            leftElem.UnionTypeName == rightElem.UnionTypeName)
        {
            return GDInferredType.Array(leftElem.UnionTypeName);
        }

        // Numeric widening: int + float → float
        if (leftElem.IsSingleType && rightElem.IsSingleType)
        {
            var leftTypeName = leftElem.UnionTypeName;
            var rightTypeName = rightElem.UnionTypeName;

            if ((leftTypeName == "int" || leftTypeName == "float") &&
                (rightTypeName == "int" || rightTypeName == "float"))
            {
                var resultType = (leftTypeName == "float" || rightTypeName == "float") ? "float" : "int";
                return GDInferredType.Array(resultType);
            }
        }

        // Create union of all element types
        var result = new GDInferredType
        {
            TypeName = "Array",
            ElementType = new GDUnionType()
        };

        // Add all types from left
        foreach (var t in leftElem.Types)
            result.ElementType.AddType(t);

        // Add all types from right
        foreach (var t in rightElem.Types)
            result.ElementType.AddType(t);

        return result;
    }

    public override string ToString() => FullTypeName;
}
