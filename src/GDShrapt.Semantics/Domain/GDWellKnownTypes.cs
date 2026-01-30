using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Well-known GDScript and Godot type names.
/// Centralizes type string literals to prevent typos and enable refactoring.
/// </summary>
internal static class GDWellKnownTypes
{
    // Core types
    public const string Variant = "Variant";
    public const string Void = "void";
    public const string Null = "null";

    // Object hierarchy
    public const string Object = "Object";
    public const string RefCounted = "RefCounted";

    // Node hierarchy
    public const string Node = "Node";
    public const string Node2D = "Node2D";
    public const string Node3D = "Node3D";
    public const string Control = "Control";

    // Built-in value types
    public const string Int = "int";
    public const string Float = "float";
    public const string Bool = "bool";
    public const string String = "String";
    public const string StringName = "StringName";

    // Container types
    public const string Array = "Array";
    public const string Dictionary = "Dictionary";

    // Vector types (float variants)
    public const string Vector2 = "Vector2";
    public const string Vector3 = "Vector3";
    public const string Vector4 = "Vector4";

    // Vector types (integer variants)
    public const string Vector2i = "Vector2i";
    public const string Vector3i = "Vector3i";
    public const string Vector4i = "Vector4i";

    // Transform types
    public const string Transform2D = "Transform2D";
    public const string Transform3D = "Transform3D";
    public const string Basis = "Basis";
    public const string Quaternion = "Quaternion";
    public const string Projection = "Projection";

    // Other math types
    public const string Color = "Color";
    public const string Rect2 = "Rect2";
    public const string Rect2i = "Rect2i";
    public const string AABB = "AABB";
    public const string Plane = "Plane";

    // Packed arrays
    public const string PackedByteArray = "PackedByteArray";
    public const string PackedInt32Array = "PackedInt32Array";
    public const string PackedInt64Array = "PackedInt64Array";
    public const string PackedFloat32Array = "PackedFloat32Array";
    public const string PackedFloat64Array = "PackedFloat64Array";
    public const string PackedStringArray = "PackedStringArray";
    public const string PackedVector2Array = "PackedVector2Array";
    public const string PackedVector3Array = "PackedVector3Array";
    public const string PackedColorArray = "PackedColorArray";

    // Special types
    public const string Callable = "Callable";
    public const string Signal = "Signal";
    public const string RID = "RID";
    public const string NodePath = "NodePath";

    /// <summary>
    /// Types that support iteration (for-in loops).
    /// </summary>
    public static readonly HashSet<string> IterableTypes = new()
    {
        Array, Dictionary, String,
        PackedByteArray, PackedInt32Array, PackedInt64Array,
        PackedFloat32Array, PackedFloat64Array, PackedStringArray,
        PackedVector2Array, PackedVector3Array, PackedColorArray
    };

    /// <summary>
    /// Types that support indexing with [].
    /// </summary>
    public static readonly HashSet<string> IndexableTypes = new()
    {
        Array, Dictionary, String,
        Vector2, Vector3, Vector4,
        Vector2i, Vector3i, Vector4i,
        PackedByteArray, PackedInt32Array, PackedInt64Array,
        PackedFloat32Array, PackedFloat64Array, PackedStringArray,
        PackedVector2Array, PackedVector3Array, PackedColorArray,
        Color, Basis, Transform2D, Transform3D, Projection
    };
}
