using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Inferred element type for a container (Array or Dictionary).
/// Uses GDUnionType for element types to leverage Union type infrastructure.
/// </summary>
public class GDContainerElementType
{
    /// <summary>
    /// Union type of container elements (for Array) or values (for Dictionary).
    /// </summary>
    public GDUnionType ElementUnionType { get; } = new();

    /// <summary>
    /// Union type of keys (for Dictionary). Null for Array.
    /// </summary>
    public GDUnionType? KeyUnionType { get; set; }

    /// <summary>
    /// Whether this is a Dictionary (vs Array).
    /// </summary>
    public bool IsDictionary { get; set; }

    /// <summary>
    /// Effective element type (single type, common base, or Variant).
    /// </summary>
    public string EffectiveElementType => ElementUnionType.EffectiveType;

    /// <summary>
    /// Effective key type for Dictionary.
    /// </summary>
    public string? EffectiveKeyType => KeyUnionType?.EffectiveType;

    /// <summary>
    /// Overall confidence (delegated to ElementUnionType).
    /// </summary>
    public bool AllHighConfidence => ElementUnionType.AllHighConfidence;

    /// <summary>
    /// Whether the container is homogeneous (single element type).
    /// </summary>
    public bool IsHomogeneous => ElementUnionType.IsSingleType;

    /// <summary>
    /// Whether we have any element type information.
    /// </summary>
    public bool HasElementTypes => !ElementUnionType.IsEmpty;

    public override string ToString()
    {
        if (IsDictionary)
        {
            var keyType = KeyUnionType?.UnionTypeName ?? "Variant";
            var valueType = ElementUnionType.UnionTypeName;
            return $"Dictionary[{keyType}, {valueType}]";
        }
        else
        {
            var elementType = ElementUnionType.UnionTypeName;
            return elementType == "Variant" ? "Array" : $"Array[{elementType}]";
        }
    }

    #region Static Factory Methods

    /// <summary>
    /// Creates a GDContainerElementType from a GDTypeNode (AST type from parser).
    /// Returns null if the type is not a container type.
    /// </summary>
    public static GDContainerElementType? FromTypeNode(GDTypeNode? typeNode)
    {
        if (typeNode == null)
            return null;

        if (typeNode is GDArrayTypeNode arrayNode)
        {
            var result = new GDContainerElementType { IsDictionary = false };
            if (arrayNode.InnerType != null)
            {
                var innerTypeName = arrayNode.InnerType.BuildName();
                if (!string.IsNullOrEmpty(innerTypeName))
                    result.ElementUnionType.AddType(innerTypeName);
            }
            return result;
        }

        if (typeNode is GDDictionaryTypeNode dictNode)
        {
            var result = new GDContainerElementType { IsDictionary = true };
            if (dictNode.KeyType != null)
            {
                result.KeyUnionType = new GDUnionType();
                var keyTypeName = dictNode.KeyType.BuildName();
                if (!string.IsNullOrEmpty(keyTypeName))
                    result.KeyUnionType.AddType(keyTypeName);
            }
            if (dictNode.ValueType != null)
            {
                var valueTypeName = dictNode.ValueType.BuildName();
                if (!string.IsNullOrEmpty(valueTypeName))
                    result.ElementUnionType.AddType(valueTypeName);
            }
            return result;
        }

        return null;
    }

    /// <summary>
    /// Combines two array types. Used for array addition (Array[A] + Array[B]).
    /// Returns Array with union element type (Array[A|B]).
    /// </summary>
    public static GDContainerElementType? CombineArrays(
        GDContainerElementType? left,
        GDContainerElementType? right)
    {
        if (left == null || right == null)
            return new GDContainerElementType { IsDictionary = false };

        if (left.IsDictionary || right.IsDictionary)
            return null;

        var result = new GDContainerElementType { IsDictionary = false };

        // Both untyped → untyped
        if (left.ElementUnionType.IsEmpty && right.ElementUnionType.IsEmpty)
            return result;

        // One untyped → untyped Array
        if (left.ElementUnionType.IsEmpty || right.ElementUnionType.IsEmpty)
            return result;

        // Same single type → preserve
        if (left.ElementUnionType.IsSingleType && right.ElementUnionType.IsSingleType)
        {
            var leftType = left.ElementUnionType.EffectiveType;
            var rightType = right.ElementUnionType.EffectiveType;

            if (leftType == rightType)
            {
                result.ElementUnionType.AddType(leftType);
                return result;
            }

            // Numeric widening: int + float → float
            if ((leftType == "int" || leftType == "float") &&
                (rightType == "int" || rightType == "float"))
            {
                var widenedType = (leftType == "float" || rightType == "float") ? "float" : "int";
                result.ElementUnionType.AddType(widenedType);
                return result;
            }
        }

        // Union all types from both arrays
        foreach (var t in left.ElementUnionType.Types)
            result.ElementUnionType.AddType(t);
        foreach (var t in right.ElementUnionType.Types)
            result.ElementUnionType.AddType(t);

        return result;
    }

    #endregion
}
