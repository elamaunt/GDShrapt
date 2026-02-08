using System;
using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Single implementation of member resolution with inheritance chain traversal.
/// Used by all type services instead of duplicated implementations.
/// </summary>
internal class GDMemberResolver
{
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDMemberResolver(IGDRuntimeProvider? runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Finds a member in a type, traversing the inheritance chain if necessary.
    /// Normalizes generic types (e.g., Array[int] -> Array) for member lookup.
    /// </summary>
    /// <param name="typeName">The type to search in</param>
    /// <param name="memberName">The member name to find</param>
    /// <returns>Member info, or null if not found</returns>
    public GDRuntimeMemberInfo? FindMember(string? typeName, string? memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        if (_runtimeProvider == null)
            return null;

        return TraverseInheritanceChainWithNormalization(typeName,
            current => _runtimeProvider.GetMember(current, memberName));
    }

    /// <summary>
    /// Gets the base type for a given type.
    /// </summary>
    /// <param name="typeName">The type to get the base of</param>
    /// <returns>Base type name, or null if no base type</returns>
    public string? GetBaseType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        return _runtimeProvider?.GetBaseType(typeName);
    }

    /// <summary>
    /// Checks if a type is known to the runtime provider.
    /// </summary>
    public bool IsKnownType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return _runtimeProvider?.IsKnownType(typeName) ?? false;
    }

    /// <summary>
    /// Gets the runtime provider used for type resolution.
    /// </summary>
    public IGDRuntimeProvider? RuntimeProvider => _runtimeProvider;

    /// <summary>
    /// Traverses the inheritance chain, calling the action for each type.
    /// Stops and returns when action returns a non-null result.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="typeName">The starting type</param>
    /// <param name="action">Action to execute for each type in the chain</param>
    /// <returns>First non-null result from action, or null if not found</returns>
    public T? TraverseInheritanceChain<T>(string? typeName, Func<string, T?> action)
        where T : class
    {
        if (string.IsNullOrEmpty(typeName) || _runtimeProvider == null)
            return null;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var result = action(current);
            if (result != null)
                return result;

            current = _runtimeProvider.GetBaseType(current);
        }

        return null;
    }

    /// <summary>
    /// Traverses the inheritance chain with generic type normalization.
    /// Normalizes types like Array[int] -> Array for member lookup.
    /// </summary>
    public T? TraverseInheritanceChainWithNormalization<T>(string? typeName, Func<string, T?> action)
        where T : class
    {
        if (string.IsNullOrEmpty(typeName) || _runtimeProvider == null)
            return null;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrEmpty(current))
        {
            // Normalize generic types: Array[int] -> Array, Dictionary[String, int] -> Dictionary
            var normalizedType = ExtractBaseTypeName(current);

            // Prevent infinite loop on cyclic inheritance
            if (!visited.Add(normalizedType))
                return null;

            var result = action(normalizedType);
            if (result != null)
                return result;

            current = _runtimeProvider.GetBaseType(normalizedType);
        }

        return null;
    }

    /// <summary>
    /// Extracts the base type name from a generic type.
    /// For example: "Array[int]" -> "Array", "Dictionary[String, int]" -> "Dictionary"
    /// </summary>
    public static string ExtractBaseTypeName(string? typeName)
    {
        return GDGenericTypeHelper.ExtractBaseTypeName(typeName ?? string.Empty);
    }

    /// <summary>
    /// Traverses the inheritance chain for value types (int, bool, etc.).
    /// </summary>
    public T? TraverseInheritanceChainValue<T>(string? typeName, Func<string, T?> action)
        where T : struct
    {
        if (string.IsNullOrEmpty(typeName) || _runtimeProvider == null)
            return null;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var result = action(current);
            if (result.HasValue)
                return result;

            current = _runtimeProvider.GetBaseType(current);
        }

        return null;
    }

    /// <summary>
    /// Finds the type that declares a specific member.
    /// </summary>
    /// <param name="typeName">The type to start searching from</param>
    /// <param name="memberName">The member name to find</param>
    /// <returns>The declaring type name, or the original type if not found</returns>
    public string FindDeclaringType(string? typeName, string? memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return typeName ?? string.Empty;

        if (_runtimeProvider == null)
            return typeName;

        var declaringType = TraverseInheritanceChain(typeName, current =>
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(current);
            if (typeInfo?.Members != null)
            {
                foreach (var member in typeInfo.Members)
                {
                    if (member.Name == memberName)
                        return current;
                }
            }
            return null;
        });

        return declaringType ?? typeName;
    }

    /// <summary>
    /// Checks if a source type is assignable to a target type.
    /// </summary>
    public bool IsAssignableTo(string? sourceType, string? targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        if (sourceType == targetType)
            return true;

        return _runtimeProvider?.IsAssignableTo(sourceType, targetType) ?? false;
    }
}
