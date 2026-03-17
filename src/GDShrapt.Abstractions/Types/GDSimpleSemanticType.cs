using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

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

    public override bool IsArray => TypeName == "Array" || GDGenericTypeHelper.IsGenericArrayType(TypeName);

    public override bool IsDictionary => TypeName == "Dictionary" || GDGenericTypeHelper.IsGenericDictionaryType(TypeName);

    public override bool IsContainer => IsArray || IsDictionary;

    public override bool IsCallable => GDGenericTypeHelper.IsCallableType(TypeName);

    public override bool IsSignal => TypeName == "Signal";

    public override bool IsNumeric => TypeName == "int" || TypeName == "float";

    public override bool IsString => TypeName == "String" || TypeName == "StringName" || TypeName == "NodePath";

    public override bool IsBool => TypeName == "bool";

    public override bool IsType(string typeName) => TypeName == typeName;

    public override bool IsValueType =>
        IsNumeric || IsString || IsBool || IsContainer || IsCallable
        || TypeName is "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i"
            or "Color" or "Rect2" or "Rect2i" or "Transform2D" or "Transform3D"
            or "Basis" or "Quaternion" or "Plane" or "AABB" or "Projection"
            or "RID" or "Signal"
            or "PackedByteArray" or "PackedInt32Array" or "PackedInt64Array"
            or "PackedFloat32Array" or "PackedFloat64Array" or "PackedStringArray"
            or "PackedVector2Array" or "PackedVector3Array" or "PackedColorArray"
            or "PackedVector4Array";

    public GDSimpleSemanticType(string typeName, GDTypeNode? astNode = null)
    {
        TypeName = typeName ?? "Variant";
        AstNode = astNode;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        if (other.IsVariant)
            return true;

        if (other is GDSimpleSemanticType simple && simple.TypeName == TypeName)
            return true;

        if (other is GDUnionSemanticType union)
            return union.Types.Any(t => IsAssignableTo(t, provider));

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

        if (GDGenericTypeHelper.IsUnionType(TypeName))
            return null;

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
