using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates indexer expressions (array[key], dict[key]) using semantic model.
/// Reports errors for:
/// - Indexing on non-indexable types (int, float, bool, etc.)
/// - Key type mismatch (string key for int-indexed array)
/// </summary>
public class GDIndexerValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;

    // Types that support integer indexing
    private static readonly HashSet<string> IntegerIndexableTypes = new HashSet<string>
    {
        "Array", "String", "StringName",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array",
        "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
        "PackedColorArray", "PackedVector4Array"
    };

    // Types that support any key type (Dictionary, Variant)
    private static readonly HashSet<string> AnyKeyIndexableTypes = new HashSet<string>
    {
        "Dictionary", "Variant"
    };

    // Types that are never indexable
    private static readonly HashSet<string> NonIndexableTypes = new HashSet<string>
    {
        "int", "float", "bool", "void", "null"
    };

    public GDIndexerValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel)
        : base(context)
    {
        _semanticModel = semanticModel;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDIndexerExpression indexer)
    {
        var callerExpr = indexer.CallerExpression;
        var keyExpr = indexer.InnerExpression;

        if (callerExpr == null || keyExpr == null)
            return;

        // Get the type of the container being indexed
        var callerTypeInfo = _semanticModel.TypeSystem.GetType(callerExpr);
        if (callerTypeInfo.IsVariant)
            return;
        var callerType = callerTypeInfo.DisplayName;

        // Extract base type for generics (Array[int] -> Array)
        var baseType = ExtractBaseTypeName(callerType);

        // Check if type is indexable
        if (NonIndexableTypes.Contains(baseType))
        {
            ReportError(
                GDDiagnosticCode.NotIndexable,
                $"Type '{callerType}' does not support indexing",
                indexer);
            return;
        }

        // For typed expressions, check key type
        if (!string.IsNullOrEmpty(baseType) && baseType != "Variant")
        {
            ValidateKeyType(indexer, callerType, baseType, keyExpr);
        }
    }

    private void ValidateKeyType(GDIndexerExpression indexer, string callerType, string baseType, GDExpression keyExpr)
    {
        // Get the type of the key expression
        var keyTypeInfo = _semanticModel.TypeSystem.GetType(keyExpr);
        if (keyTypeInfo.IsVariant)
            return;
        var keyType = keyTypeInfo.DisplayName;
        if (keyType == "Unknown")
            return;

        // Integer-indexed types (Array, String, Packed*Array)
        if (IntegerIndexableTypes.Contains(baseType))
        {
            if (keyType != "int" && keyType != "float") // float is auto-converted to int
            {
                ReportWarning(
                    GDDiagnosticCode.IndexerKeyTypeMismatch,
                    $"Type '{callerType}' expects integer index, got '{keyType}'",
                    indexer);
            }
            return;
        }

        // Dictionary with typed keys: Dictionary[KeyType, ValueType]
        if (baseType == "Dictionary" && callerType.Contains("["))
        {
            var expectedKeyType = ExtractDictionaryKeyType(callerType);
            if (!string.IsNullOrEmpty(expectedKeyType) && expectedKeyType != "Variant")
            {
                if (!AreTypesCompatible(keyType, expectedKeyType))
                {
                    ReportWarning(
                        GDDiagnosticCode.IndexerKeyTypeMismatch,
                        $"Dictionary expects key of type '{expectedKeyType}', got '{keyType}'",
                        indexer);
                }
            }
            return;
        }

        // Typed Array: Array[ElementType]
        if (baseType == "Array" && callerType.Contains("["))
        {
            // Typed arrays still use integer indices
            if (keyType != "int" && keyType != "float")
            {
                ReportWarning(
                    GDDiagnosticCode.IndexerKeyTypeMismatch,
                    $"Array expects integer index, got '{keyType}'",
                    indexer);
            }
            return;
        }

        // Any-key indexable types (Dictionary, Variant) - allow any key
        if (AnyKeyIndexableTypes.Contains(baseType))
        {
            return;
        }

        // For other types (Node, RefCounted, etc.) check if they have operator[]
        // Most Godot types don't support indexing unless they inherit from a collection
        if (!IsKnownIndexableType(baseType))
        {
            // Only report if we're confident the type doesn't support indexing
            // Skip for custom classes which might have get() implemented
            var runtimeProvider = Context.RuntimeProvider;
            if (runtimeProvider != null)
            {
                var typeInfo = runtimeProvider.GetGlobalClass(baseType);
                if (typeInfo == null)
                {
                    // Built-in type that's not in our indexable lists
                    var baseTypeOfType = runtimeProvider.GetBaseType(baseType);
                    if (baseTypeOfType != null && !IsInheritingFromIndexable(baseType, runtimeProvider))
                    {
                        ReportWarning(
                            GDDiagnosticCode.NotIndexable,
                            $"Type '{callerType}' may not support indexing",
                            indexer);
                    }
                }
            }
        }
    }

    private bool IsKnownIndexableType(string typeName)
    {
        return IntegerIndexableTypes.Contains(typeName) ||
               AnyKeyIndexableTypes.Contains(typeName);
    }

    private bool IsInheritingFromIndexable(string typeName, IGDRuntimeProvider runtimeProvider)
    {
        var current = typeName;
        var visited = new HashSet<string>();

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            if (IsKnownIndexableType(current))
                return true;

            current = runtimeProvider.GetBaseType(current);
        }

        return false;
    }

    /// <summary>
    /// Extracts the key type from a typed Dictionary.
    /// For example: "Dictionary[String, int]" -> "String"
    /// </summary>
    private static string? ExtractDictionaryKeyType(string typeName)
    {
        var bracketStart = typeName.IndexOf('[');
        if (bracketStart < 0)
            return null;

        var bracketEnd = typeName.LastIndexOf(']');
        if (bracketEnd <= bracketStart)
            return null;

        var typeParams = typeName.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);

        // Find the comma that separates key and value types
        // Need to handle nested generics: Dictionary[Array[int], String]
        var depth = 0;
        for (int i = 0; i < typeParams.Length; i++)
        {
            var c = typeParams[i];
            if (c == '[') depth++;
            else if (c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                return typeParams.Substring(0, i).Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the base type name from a generic type.
    /// For example: "Array[int]" -> "Array", "Dictionary[String, int]" -> "Dictionary"
    /// </summary>
    private static string ExtractBaseTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var bracketIndex = typeName.IndexOf('[');
        if (bracketIndex > 0)
            return typeName.Substring(0, bracketIndex);

        return typeName;
    }

    private bool AreTypesCompatible(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return true;

        if (sourceType == targetType)
            return true;

        if (targetType == "Variant")
            return true;

        // Use semantic model for detailed check
        return _semanticModel.AreTypesCompatible(sourceType, targetType);
    }
}
