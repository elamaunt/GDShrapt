using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Canonical mapping of packed array types to their element types.
/// Eliminates duplicated switch/dictionary definitions across the codebase.
/// </summary>
public static class GDPackedArrayTypes
{
    /// <summary>
    /// Maps packed array type names to their element types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ElementTypes = new Dictionary<string, string>
    {
        ["PackedByteArray"] = "int",
        ["PackedInt32Array"] = "int",
        ["PackedInt64Array"] = "int",
        ["PackedFloat32Array"] = "float",
        ["PackedFloat64Array"] = "float",
        ["PackedStringArray"] = "String",
        ["PackedVector2Array"] = "Vector2",
        ["PackedVector3Array"] = "Vector3",
        ["PackedVector4Array"] = "Vector4",
        ["PackedColorArray"] = "Color",
    };

    /// <summary>
    /// Gets the element type for a packed array type.
    /// Returns null if the type is not a packed array.
    /// </summary>
    public static string? GetElementType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;
        return ElementTypes.TryGetValue(typeName, out var result) ? result : null;
    }

    /// <summary>
    /// Checks whether the type is a packed array type.
    /// </summary>
    public static bool IsPackedArray(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName) && ElementTypes.ContainsKey(typeName);
    }
}
