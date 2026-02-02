using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Provides runtime type information for GDScript validation.
/// Implement this interface to provide type data from your runtime environment
/// (Godot, custom interpreter, or other software).
/// </summary>
public interface IGDRuntimeProvider
{
    /// <summary>
    /// Checks if a type name is known to the runtime.
    /// </summary>
    /// <param name="typeName">The type name to check (e.g., "Node2D", "Control")</param>
    /// <returns>True if the type exists in the runtime</returns>
    bool IsKnownType(string typeName);

    /// <summary>
    /// Gets detailed information about a type.
    /// </summary>
    /// <param name="typeName">The type name to look up</param>
    /// <returns>Type information, or null if not found</returns>
    GDRuntimeTypeInfo? GetTypeInfo(string typeName);

    /// <summary>
    /// Gets information about a member (method, property, signal) of a type.
    /// </summary>
    /// <param name="typeName">The type containing the member</param>
    /// <param name="memberName">The member name to look up</param>
    /// <returns>Member information, or null if not found</returns>
    GDRuntimeMemberInfo? GetMember(string typeName, string memberName);

    /// <summary>
    /// Gets the base type of a given type (for inheritance checking).
    /// </summary>
    /// <param name="typeName">The type to get the base of</param>
    /// <returns>Base type name, or null if no base type</returns>
    string? GetBaseType(string typeName);

    /// <summary>
    /// Checks if a source type can be assigned to a target type.
    /// </summary>
    /// <param name="sourceType">The type being assigned</param>
    /// <param name="targetType">The type being assigned to</param>
    /// <returns>True if assignment is valid</returns>
    bool IsAssignableTo(string sourceType, string targetType);

    /// <summary>
    /// Gets information about a global function (built-in or runtime).
    /// </summary>
    /// <param name="functionName">The function name to look up</param>
    /// <returns>Function information, or null if not found</returns>
    GDRuntimeFunctionInfo? GetGlobalFunction(string functionName);

    /// <summary>
    /// Gets information about a global class or singleton (e.g., Input, Engine, OS).
    /// </summary>
    /// <param name="className">The global class/singleton name</param>
    /// <returns>Type information, or null if not found</returns>
    GDRuntimeTypeInfo? GetGlobalClass(string className);

    /// <summary>
    /// Checks if an identifier is a known built-in constant, type, or global.
    /// </summary>
    /// <param name="identifier">The identifier to check</param>
    /// <returns>True if the identifier is a built-in</returns>
    bool IsBuiltIn(string identifier);

    /// <summary>
    /// Checks if a type is a builtin value type that can never be null.
    /// In Godot 4, this includes: int, float, bool, String, Vector2/3/4, Color,
    /// Transform2D/3D, Array, Dictionary, PackedArrays, etc.
    /// </summary>
    /// <param name="typeName">The type name to check</param>
    /// <returns>True if the type is a builtin value type (never null)</returns>
    bool IsBuiltinType(string typeName);

    /// <summary>
    /// Gets all known type names from this provider.
    /// Used for duck typing resolution to find compatible types.
    /// </summary>
    /// <returns>Enumerable of all known type names</returns>
    IEnumerable<string> GetAllTypes();

    /// <summary>
    /// Finds all types that have a specific method defined.
    /// Used for duck typing and Variant method inference.
    /// </summary>
    /// <param name="methodName">The method name to search for</param>
    /// <returns>List of type names that have this method</returns>
    IReadOnlyList<string> FindTypesWithMethod(string methodName);
}
