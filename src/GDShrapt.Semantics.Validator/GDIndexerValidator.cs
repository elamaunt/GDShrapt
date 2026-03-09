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

        // Check if type is non-indexable via structural properties
        if (callerTypeInfo.IsNumeric || callerTypeInfo.IsBool || callerTypeInfo.IsType("void") || callerTypeInfo.IsType("null"))
        {
            ReportError(
                GDDiagnosticCode.NotIndexable,
                $"Type '{callerType}' does not support indexing",
                indexer);
            return;
        }

        // For typed expressions, check key type
        var baseType = callerTypeInfo is GDContainerSemanticType ct
            ? (ct.IsDictionary ? "Dictionary" : "Array")
            : callerType;
        if (!string.IsNullOrEmpty(baseType))
        {
            ValidateKeyType(indexer, callerType, baseType, keyExpr, callerTypeInfo);
        }
    }

    private void ValidateKeyType(GDIndexerExpression indexer, string callerType, string baseType, GDExpression keyExpr, GDSemanticType callerTypeInfo)
    {
        // Get the type of the key expression
        var keyTypeInfo = _semanticModel.TypeSystem.GetType(keyExpr);
        if (keyTypeInfo.IsVariant)
            return;
        var keyType = keyTypeInfo.DisplayName;
        if (keyTypeInfo.IsType("Unknown"))
            return;

        // Integer-indexed types (Array, String, Packed*Array)
        if (IntegerIndexableTypes.Contains(baseType))
        {
            if (!keyTypeInfo.IsType("int") && !keyTypeInfo.IsType("float")) // float is auto-converted to int
            {
                ReportWarning(
                    GDDiagnosticCode.IndexerKeyTypeMismatch,
                    $"Type '{callerType}' expects integer index, got '{keyType}'",
                    indexer);
            }
            return;
        }

        // Dictionary with typed keys: Dictionary[KeyType, ValueType]
        if (callerTypeInfo is GDContainerSemanticType dictContainer && dictContainer.IsDictionary)
        {
            var expectedKeyType = dictContainer.KeyType?.DisplayName;
            if (!string.IsNullOrEmpty(expectedKeyType) && !dictContainer.KeyType.IsVariant)
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
        if (callerTypeInfo is GDContainerSemanticType arrayContainer && arrayContainer.IsArray)
        {
            // Typed arrays still use integer indices
            if (!keyTypeInfo.IsType("int") && !keyTypeInfo.IsType("float"))
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

    private bool AreTypesCompatible(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return true;

        if (sourceType == targetType)
            return true;

        if (GDSemanticType.FromRuntimeTypeName(targetType).IsVariant)
            return true;

        // Use semantic model for detailed check
        return _semanticModel.AreTypesCompatible(sourceType, targetType);
    }
}
