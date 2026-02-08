using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Centralized constants for well-known GDScript and Godot type names.
/// Eliminates hardcoded strings across the semantic analysis codebase.
/// </summary>
internal static class GDWellKnownTypes
{
    public const string Variant = "Variant";
    public const string Object = "Object";
    public const string RefCounted = "RefCounted";
    public const string Resource = "Resource";
    public const string Node = "Node";
    public const string GDScriptPseudoType = "@GDScript";

    public const string Void = "void";
    public const string Null = "null";
    public const string Self = "self";

    public static class Numeric
    {
        public const string Int = "int";
        public const string Float = "float";
        public const string Bool = "bool";
    }

    public static class Strings
    {
        public const string String = "String";
        public const string StringName = "StringName";
    }

    public static class Containers
    {
        public const string Array = "Array";
        public const string Dictionary = "Dictionary";
    }

    public static class Vectors
    {
        public const string Vector2 = "Vector2";
        public const string Vector3 = "Vector3";
        public const string Vector4 = "Vector4";
        public const string Vector2i = "Vector2i";
        public const string Vector3i = "Vector3i";
        public const string Vector4i = "Vector4i";
    }

    public static class Other
    {
        public const string Color = "Color";
        public const string Callable = "Callable";
        public const string Signal = "Signal";
        public const string Error = "Error";
        public const string NodePath = "NodePath";
        public const string Range = "Range";
        public const string RID = "RID";
        public const string Nil = "Nil";
    }

    public static class Geometry
    {
        public const string Rect2 = "Rect2";
        public const string Rect2i = "Rect2i";
        public const string AABB = "AABB";
        public const string Transform2D = "Transform2D";
        public const string Transform3D = "Transform3D";
        public const string Basis = "Basis";
        public const string Projection = "Projection";
        public const string Quaternion = "Quaternion";
        public const string Plane = "Plane";
    }

    public static class Nodes
    {
        public const string Node2D = "Node2D";
    }

    public static class PackedArrays
    {
        public const string PackedByteArray = "PackedByteArray";
        public const string PackedInt32Array = "PackedInt32Array";
        public const string PackedInt64Array = "PackedInt64Array";
        public const string PackedFloat32Array = "PackedFloat32Array";
        public const string PackedFloat64Array = "PackedFloat64Array";
        public const string PackedStringArray = "PackedStringArray";
        public const string PackedVector2Array = "PackedVector2Array";
        public const string PackedVector3Array = "PackedVector3Array";
        public const string PackedVector4Array = "PackedVector4Array";
        public const string PackedColorArray = "PackedColorArray";
    }

    /// <summary>
    /// Maps packed array type names to their element types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PackedArrayElementTypes = GDPackedArrayTypes.ElementTypes;

    /// <summary>
    /// Maps integer vector types to their float equivalents.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> IntVectorToFloatVector = new Dictionary<string, string>
    {
        [Vectors.Vector2i] = Vectors.Vector2,
        [Vectors.Vector3i] = Vectors.Vector3,
        [Vectors.Vector4i] = Vectors.Vector4,
    };

    /// <summary>
    /// Maps built-in GDScript identifiers to their types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> BuiltinIdentifierTypes = new Dictionary<string, string>
    {
        ["true"] = Numeric.Bool,
        ["false"] = Numeric.Bool,
        ["null"] = Null,
        ["PI"] = Numeric.Float,
        ["TAU"] = Numeric.Float,
        ["INF"] = Numeric.Float,
        ["NAN"] = Numeric.Float,
    };

    public static bool IsNumericType(string typeName) => typeName is Numeric.Int or Numeric.Float;

    public static bool IsStringType(string typeName) => typeName is Strings.String or Strings.StringName;

    public static bool IsVectorType(string typeName) =>
        typeName is Vectors.Vector2 or Vectors.Vector3 or Vectors.Vector4
               or Vectors.Vector2i or Vectors.Vector3i or Vectors.Vector4i;

    public static bool IsContainerType(string typeName) => typeName is Containers.Array or Containers.Dictionary;

    public static bool IsPrimitiveType(string typeName) =>
        typeName is Numeric.Int or Numeric.Float or Numeric.Bool or Strings.String or Void;

    public static bool IsBuiltInType(string typeName) => BuiltInTypes.Contains(typeName);

    private static readonly HashSet<string> BuiltInTypes = new()
    {
        Numeric.Int, Numeric.Float, Numeric.Bool, Strings.String, Void, Variant,
        Containers.Array, Containers.Dictionary, Other.Callable, Other.Signal, Other.NodePath, Strings.StringName,
        Vectors.Vector2, Vectors.Vector2i, Vectors.Vector3, Vectors.Vector3i, Vectors.Vector4, Vectors.Vector4i,
        Geometry.Rect2, Geometry.Rect2i, Geometry.AABB, Geometry.Transform2D, Geometry.Transform3D,
        Geometry.Basis, Geometry.Projection, Geometry.Quaternion, Geometry.Plane,
        Other.Color, Other.RID, Object, Other.Nil,
        PackedArrays.PackedByteArray, PackedArrays.PackedInt32Array, PackedArrays.PackedInt64Array,
        PackedArrays.PackedFloat32Array, PackedArrays.PackedFloat64Array,
        PackedArrays.PackedStringArray, PackedArrays.PackedVector2Array, PackedArrays.PackedVector3Array,
        PackedArrays.PackedColorArray, PackedArrays.PackedVector4Array,
    };
}
