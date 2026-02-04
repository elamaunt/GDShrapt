using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Composite runtime provider that combines multiple type sources.
/// Queries providers in order and returns first non-null result.
/// </summary>
public class GDCompositeRuntimeProvider : IGDRuntimeProvider
{
    private readonly IGDRuntimeProvider[] _providers;

    /// <summary>
    /// Gets the project types provider if available.
    /// </summary>
    public GDProjectTypesProvider? ProjectTypesProvider { get; }

    /// <summary>
    /// Gets the Godot types provider if available. Internal - external code should use IGDRuntimeProvider interface.
    /// </summary>
    internal GDGodotTypesProvider? GodotTypesProvider { get; }

    public GDCompositeRuntimeProvider(params IGDRuntimeProvider?[] providers)
    {
        _providers = providers.Where(p => p != null).Cast<IGDRuntimeProvider>().ToArray();

        // Store references to specific providers for direct access
        foreach (var provider in _providers)
        {
            if (provider is GDProjectTypesProvider projectProvider)
                ProjectTypesProvider = projectProvider;
            else if (provider is GDGodotTypesProvider godotProvider)
                GodotTypesProvider = godotProvider;
        }
    }

    public GDCompositeRuntimeProvider(
        GDGodotTypesProvider? godotTypesProvider,
        GDProjectTypesProvider? projectTypesProvider,
        GDAutoloadsProvider? autoloadsProvider,
        GDSceneTypesProvider? sceneTypesProvider)
    {
        // Include GDDefaultRuntimeProvider as fallback for built-in types (String, Array, Dictionary methods)
        var providers = new IGDRuntimeProvider?[] { godotTypesProvider, projectTypesProvider, autoloadsProvider, sceneTypesProvider, GDDefaultRuntimeProvider.Instance };
        _providers = providers.Where(p => p != null).Cast<IGDRuntimeProvider>().ToArray();

        // Store direct references
        GodotTypesProvider = godotTypesProvider;
        ProjectTypesProvider = projectTypesProvider;

        // Set composite provider for lazy return type inference
        projectTypesProvider?.SetCompositeProvider(this);
    }

    public bool IsKnownType(string typeName)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsKnownType(typeName))
                return true;
        }
        return false;
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        foreach (var provider in _providers)
        {
            var info = provider.GetTypeInfo(typeName);
            if (info != null)
                return info;
        }
        return null;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        foreach (var provider in _providers)
        {
            var member = provider.GetMember(typeName, memberName);
            if (member != null)
                return member;
        }
        return null;
    }

    public string? GetBaseType(string typeName)
    {
        foreach (var provider in _providers)
        {
            var baseType = provider.GetBaseType(typeName);
            if (!string.IsNullOrEmpty(baseType))
                return baseType;
        }
        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        // Same type
        if (sourceType == targetType)
            return true;

        // First, try each provider's built-in IsAssignableTo logic
        // This handles provider-specific rules (numeric conversion, String<->StringName, etc.)
        foreach (var provider in _providers)
        {
            if (provider.IsAssignableTo(sourceType, targetType))
                return true;
        }

        // If no provider matched, walk inheritance chain using GetBaseType from all providers
        // This handles cross-provider inheritance (e.g., SceneNodes [project] -> Node2D [Godot] -> Node [Godot])
        var visited = new HashSet<string>();
        var current = sourceType;
        while (!string.IsNullOrEmpty(current))
        {
            if (!visited.Add(current))
                return false; // Cycle detected

            if (current == targetType)
                return true;

            // Get base type from any provider
            current = GetBaseType(current);
        }

        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        foreach (var provider in _providers)
        {
            var func = provider.GetGlobalFunction(name);
            if (func != null)
                return func;
        }
        return null;
    }

    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        foreach (var provider in _providers)
        {
            var cls = provider.GetGlobalClass(className);
            if (cls != null)
                return cls;
        }
        return null;
    }

    public bool IsBuiltIn(string identifier)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsBuiltIn(identifier))
                return true;
        }
        return false;
    }

    public IEnumerable<string> GetAllTypes()
    {
        var types = new HashSet<string>();
        foreach (var provider in _providers)
        {
            foreach (var type in provider.GetAllTypes())
                types.Add(type);
        }
        return types;
    }

    public bool IsBuiltinType(string typeName)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsBuiltinType(typeName))
                return true;
        }
        return false;
    }

    public IReadOnlyList<string> FindTypesWithMethod(string methodName)
    {
        var types = new HashSet<string>();
        foreach (var provider in _providers)
        {
            foreach (var type in provider.FindTypesWithMethod(methodName))
                types.Add(type);
        }
        return types.ToList();
    }

    // Type Traits - composite implementations (query all providers)
    public bool IsNumericType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsNumericType(typeName))
                return true;
        return false;
    }

    public bool IsIterableType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsIterableType(typeName))
                return true;
        return false;
    }

    public bool IsIndexableType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsIndexableType(typeName))
                return true;
        return false;
    }

    public bool IsNullableType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsNullableType(typeName))
                return true;
        return false;
    }

    public bool IsVectorType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsVectorType(typeName))
                return true;
        return false;
    }

    public bool IsContainerType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsContainerType(typeName))
                return true;
        return false;
    }

    public bool IsPackedArrayType(string typeName)
    {
        foreach (var provider in _providers)
            if (provider.IsPackedArrayType(typeName))
                return true;
        return false;
    }

    public string? GetFloatVectorVariant(string integerVectorType)
    {
        foreach (var provider in _providers)
        {
            var result = provider.GetFloatVectorVariant(integerVectorType);
            if (result != null)
                return result;
        }
        return null;
    }

    public string? GetPackedArrayElementType(string packedArrayType)
    {
        foreach (var provider in _providers)
        {
            var result = provider.GetPackedArrayElementType(packedArrayType);
            if (result != null)
                return result;
        }
        return null;
    }

    public string? ResolveOperatorResult(string leftType, string operatorName, string rightType)
    {
        foreach (var provider in _providers)
        {
            var result = provider.ResolveOperatorResult(leftType, operatorName, rightType);
            if (result != null)
                return result;
        }
        return null;
    }

    public IReadOnlyList<string> GetTypesWithOperator(string operatorName)
    {
        var types = new HashSet<string>();
        foreach (var provider in _providers)
            foreach (var type in provider.GetTypesWithOperator(operatorName))
                types.Add(type);
        return types.ToList();
    }
}
