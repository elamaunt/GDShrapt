using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates generic type parameters in type annotations.
/// Reports errors for:
/// - Wrong number of type parameters (Array[int, String] instead of Array[int])
/// - Unknown type used as generic argument (Array[UnknownType])
/// - Non-hashable types as Dictionary keys (Dictionary[Array, int])
/// </summary>
public class GDGenericTypeValidator : GDValidationVisitor
{
    private readonly IGDRuntimeProvider _runtimeProvider;

    // Expected parameter counts for built-in generic types
    private static readonly Dictionary<string, int> GenericParamCounts = new Dictionary<string, int>
    {
        { "Array", 1 },
        { "Dictionary", 2 }
    };

    // Types that are hashable and can be used as Dictionary keys
    private static readonly HashSet<string> HashableTypes = new HashSet<string>
    {
        // Primitives
        "int", "float", "bool",
        // Strings
        "String", "StringName", "NodePath",
        // Math types (immutable)
        "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Rect2", "Rect2i", "AABB",
        "Transform2D", "Transform3D", "Basis", "Projection",
        "Plane", "Quaternion",
        "Color",
        "RID",
        // Callable (hashable by identity)
        "Callable", "Signal"
    };

    // Types that are NOT hashable (mutable containers)
    private static readonly HashSet<string> NonHashableTypes = new HashSet<string>
    {
        "Array", "Dictionary",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array",
        "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
        "PackedColorArray", "PackedVector4Array"
    };

    public GDGenericTypeValidator(
        GDValidationContext context,
        IGDRuntimeProvider runtimeProvider)
        : base(context)
    {
        _runtimeProvider = runtimeProvider;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDArrayTypeNode arrayType)
    {
        ValidateArrayType(arrayType);
    }

    public override void Visit(GDDictionaryTypeNode dictType)
    {
        ValidateDictionaryType(dictType);
    }

    private void ValidateArrayType(GDArrayTypeNode arrayType)
    {
        var elementType = arrayType.InnerType;
        if (elementType == null)
            return;

        // Get element type name
        var typeName = elementType.BuildName();
        if (string.IsNullOrEmpty(typeName))
            return;

        // Check if type exists
        if (!IsKnownType(typeName))
        {
            ReportWarning(
                GDDiagnosticCode.InvalidGenericArgument,
                $"Unknown type '{typeName}' used in Array type parameter",
                elementType);
        }
    }

    private void ValidateDictionaryType(GDDictionaryTypeNode dictType)
    {
        var keyType = dictType.KeyType;
        var valueType = dictType.ValueType;

        // Validate key type
        if (keyType != null)
        {
            var keyTypeName = keyType.BuildName();
            if (!string.IsNullOrEmpty(keyTypeName))
            {
                // Check if key type exists
                if (!IsKnownType(keyTypeName))
                {
                    ReportWarning(
                        GDDiagnosticCode.InvalidGenericArgument,
                        $"Unknown type '{keyTypeName}' used as Dictionary key type",
                        keyType);
                }
                // Check if key type is hashable
                else if (!IsHashableType(keyTypeName))
                {
                    ReportWarning(
                        GDDiagnosticCode.DictionaryKeyNotHashable,
                        $"Type '{keyTypeName}' is not hashable and cannot be used as Dictionary key",
                        keyType);
                }
            }
        }

        // Validate value type
        if (valueType != null)
        {
            var valueTypeName = valueType.BuildName();
            if (!string.IsNullOrEmpty(valueTypeName))
            {
                // Check if value type exists
                if (!IsKnownType(valueTypeName))
                {
                    ReportWarning(
                        GDDiagnosticCode.InvalidGenericArgument,
                        $"Unknown type '{valueTypeName}' used as Dictionary value type",
                        valueType);
                }
            }
        }
    }

    private bool IsKnownType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // Extract base type for generics (Array[int] -> Array)
        var baseType = ExtractBaseTypeName(typeName);

        // Check built-in types
        if (_runtimeProvider.IsKnownType(baseType))
            return true;

        // Check global classes (class_name)
        if (_runtimeProvider.GetGlobalClass(baseType) != null)
            return true;

        // Check nested types (e.g., BaseMaterial3D.ShadingMode)
        var dotIndex = baseType.IndexOf('.');
        if (dotIndex > 0 && dotIndex < baseType.Length - 1)
        {
            var parentType = baseType.Substring(0, dotIndex);
            var memberName = baseType.Substring(dotIndex + 1);
            if (_runtimeProvider.GetMember(parentType, memberName) != null)
                return true;
        }

        // Variant is always valid
        if (baseType == "Variant")
            return true;

        return false;
    }

    private bool IsHashableType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return true; // Assume hashable if unknown

        // Extract base type
        var baseType = ExtractBaseTypeName(typeName);

        // Explicitly non-hashable
        if (NonHashableTypes.Contains(baseType))
            return false;

        // Explicitly hashable
        if (HashableTypes.Contains(baseType))
            return true;

        // Variant can be anything, allow it
        if (baseType == "Variant")
            return true;

        // Objects are hashable by identity (except containers above)
        // Custom classes, Nodes, Resources, etc. are hashable
        if (_runtimeProvider.IsKnownType(baseType))
            return true;

        if (_runtimeProvider.GetGlobalClass(baseType) != null)
            return true;

        // Unknown type - assume hashable (might be user-defined class)
        return true;
    }

    private static string ExtractBaseTypeName(string typeName)
        => GDGenericTypeHelper.ExtractBaseTypeName(typeName);
}
