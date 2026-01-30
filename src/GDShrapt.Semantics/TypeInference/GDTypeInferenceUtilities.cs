using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Static utility methods for type inference operations.
/// Internal helper for TypeInference analyzers.
/// </summary>
internal static class GDTypeInferenceUtilities
{
    /// <summary>
    /// Creates a simple type node from a type name string.
    /// </summary>
    public static GDTypeNode CreateSimpleType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Check for generic types like Array[T] or Dictionary[K, V]
        var bracketIdx = typeName.IndexOf('[');
        if (bracketIdx > 0 && typeName.EndsWith("]"))
        {
            var baseName = typeName.Substring(0, bracketIdx);
            var innerContent = typeName.Substring(bracketIdx + 1, typeName.Length - bracketIdx - 2);

            // Handle Array[T]
            if (baseName == "Array")
            {
                var innerType = CreateSimpleType(innerContent);
                return new GDArrayTypeNode { InnerType = innerType };
            }

            // Handle Dictionary[K, V]
            if (baseName == "Dictionary")
            {
                // Split by comma, respecting nested generics
                var parts = SplitGenericArgs(innerContent);
                if (parts.Length == 2)
                {
                    var keyType = CreateSimpleType(parts[0].Trim());
                    var valueType = CreateSimpleType(parts[1].Trim());
                    return new GDDictionaryTypeNode { KeyType = keyType, ValueType = valueType };
                }
            }
        }

        return new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };
    }

    /// <summary>
    /// Creates a Variant type node.
    /// </summary>
    public static GDTypeNode CreateVariantTypeNode()
    {
        return new GDSingleTypeNode { Type = new GDType { Sequence = "Variant" } };
    }

    /// <summary>
    /// Gets the element type for packed array types.
    /// </summary>
    public static string? GetPackedArrayElementType(string packedArrayType)
    {
        return packedArrayType switch
        {
            "PackedByteArray" => "int",
            "PackedInt32Array" => "int",
            "PackedInt64Array" => "int",
            "PackedFloat32Array" => "float",
            "PackedFloat64Array" => "float",
            "PackedStringArray" => "String",
            "PackedVector2Array" => "Vector2",
            "PackedVector3Array" => "Vector3",
            "PackedColorArray" => "Color",
            _ => null
        };
    }

    /// <summary>
    /// Gets the element type for a collection type (Array, Dictionary, PackedArrays, etc.).
    /// Uses structured type parsing instead of string manipulation.
    /// </summary>
    /// <param name="collectionType">The collection type name (e.g., "Array[String]", "Dictionary[int, String]", "PackedByteArray").</param>
    /// <returns>The element type, or "Variant" for untyped collections, or null if not a collection.</returns>
    public static string? GetCollectionElementType(string? collectionType)
    {
        if (string.IsNullOrEmpty(collectionType))
            return null;

        // Use structured parsing for generic types
        var typeNode = CreateSimpleType(collectionType);
        if (typeNode is GDArrayTypeNode arrayNode)
            return arrayNode.InnerType?.BuildName() ?? "Variant";

        if (typeNode is GDDictionaryTypeNode dictNode)
            return dictNode.ValueType?.BuildName() ?? "Variant";

        // PackedArrays
        var packedElement = GetPackedArrayElementType(collectionType);
        if (packedElement != null)
            return packedElement;

        // Untyped containers
        if (collectionType is "Array" or "Dictionary")
            return "Variant";

        // Range iteration
        if (collectionType is "int" or "Range")
            return "int";

        // String iteration returns String (single character)
        if (collectionType is "String")
            return "String";

        return null;
    }

    /// <summary>
    /// Checks if the operator is an assignment operator.
    /// </summary>
    public static bool IsAssignmentOperator(GDDualOperatorType opType)
    {
        return opType switch
        {
            GDDualOperatorType.Assignment => true,
            GDDualOperatorType.AddAndAssign => true,
            GDDualOperatorType.SubtractAndAssign => true,
            GDDualOperatorType.MultiplyAndAssign => true,
            GDDualOperatorType.DivideAndAssign => true,
            GDDualOperatorType.ModAndAssign => true,
            GDDualOperatorType.BitwiseAndAndAssign => true,
            GDDualOperatorType.BitwiseOrAndAssign => true,
            GDDualOperatorType.PowerAndAssign => true,
            GDDualOperatorType.BitShiftLeftAndAssign => true,
            GDDualOperatorType.BitShiftRightAndAssign => true,
            GDDualOperatorType.XorAndAssign => true,
            _ => false
        };
    }

    /// <summary>
    /// Infers the type of a number literal.
    /// </summary>
    public static string InferNumberType(GDNumberExpression numExpr)
    {
        var num = numExpr?.Number;
        if (num == null)
            return "int";

        var seq = num.Sequence;
        if (seq != null && (seq.Contains('.') || seq.Contains('e') || seq.Contains('E')))
            return "float";

        return "int";
    }

    /// <summary>
    /// Gets the root variable name from an expression.
    /// For example, for "foo.bar[0]" returns "foo".
    /// </summary>
    public static string GetRootVariableName(GDExpression expr)
    {
        return expr switch
        {
            GDIdentifierExpression identExpr => identExpr.Identifier?.Sequence,
            GDMemberOperatorExpression memberExpr => GetRootVariableName(memberExpr.CallerExpression),
            GDIndexerExpression indexerExpr => GetRootVariableName(indexerExpr.CallerExpression),
            GDBracketExpression bracketExpr => GetRootVariableName(bracketExpr.InnerExpression),
            _ => null
        };
    }

    /// <summary>
    /// Gets the index of a node in an expressions list.
    /// </summary>
    public static int GetIndexInList(GDExpressionsList list, GDNode node)
    {
        if (list == null)
            return -1;

        int index = 0;
        foreach (var item in list)
        {
            if (item == node)
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Checks if a string is a valid identifier.
    /// </summary>
    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Must start with letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;

        // Rest must be letters, digits, or underscores
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Splits generic type arguments, respecting nested brackets.
    /// For example: "int, Array[String]" -> ["int", "Array[String]"]
    /// </summary>
    private static string[] SplitGenericArgs(string content)
    {
        var parts = new System.Collections.Generic.List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (char c in content)
        {
            if (c == '[')
            {
                depth++;
                current.Append(c);
            }
            else if (c == ']')
            {
                depth--;
                current.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts.ToArray();
    }
}
