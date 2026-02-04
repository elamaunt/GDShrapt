using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Contains information about a type in the runtime environment.
/// </summary>
public class GDRuntimeTypeInfo
{
    /// <summary>
    /// The name of the type.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The base type name, or null if no base type.
    /// </summary>
    public string? BaseType { get; set; }

    /// <summary>
    /// True if this is a native/built-in type.
    /// </summary>
    public bool IsNative { get; set; }

    /// <summary>
    /// True if this is a singleton/autoload class.
    /// </summary>
    public bool IsSingleton { get; set; }

    /// <summary>
    /// True if this is a reference type (Resource, RefCounted).
    /// </summary>
    public bool IsRefCounted { get; set; }

    /// <summary>
    /// True if this is an abstract class (Godot 4.5+).
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Members of this type (methods, properties, signals, constants).
    /// </summary>
    public IReadOnlyList<GDRuntimeMemberInfo>? Members { get; set; }

    // ========================================
    // Type Traits
    // ========================================

    /// <summary>
    /// True if this is a numeric type (int or float).
    /// </summary>
    public bool IsNumeric { get; set; }

    /// <summary>
    /// True if this is a vector type (Vector2, Vector3, Vector4 and their integer variants).
    /// </summary>
    public bool IsVector { get; set; }

    /// <summary>
    /// True if this type supports iteration (for-in loops).
    /// </summary>
    public bool IsIterable { get; set; }

    /// <summary>
    /// True if this type supports indexing with [] operator.
    /// </summary>
    public bool IsIndexable { get; set; }

    /// <summary>
    /// True if this type can be null.
    /// False for value types (int, float, Vector*, etc.).
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// True if this is a container type (Array, Dictionary, or typed variants).
    /// </summary>
    public bool IsContainer { get; set; }

    /// <summary>
    /// True if this is a packed array type.
    /// </summary>
    public bool IsPackedArray { get; set; }

    /// <summary>
    /// True if this is a string-like type (String, StringName).
    /// </summary>
    public bool IsStringLike { get; set; }

    /// <summary>
    /// The float variant of an integer vector (e.g., "Vector2" for "Vector2i").
    /// Null if not applicable.
    /// </summary>
    public string? FloatVectorVariant { get; set; }

    /// <summary>
    /// The element type for packed arrays (e.g., "int" for PackedInt32Array).
    /// Null if not a packed array.
    /// </summary>
    public string? PackedElementType { get; set; }

    /// <summary>
    /// Creates a new type info with the given name.
    /// </summary>
    public GDRuntimeTypeInfo(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new type info.
    /// </summary>
    public GDRuntimeTypeInfo(string name, string? baseType, bool isNative = false)
    {
        Name = name;
        BaseType = baseType;
        IsNative = isNative;
    }

    public override string ToString() => Name;
}
