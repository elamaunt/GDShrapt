using GDShrapt.Reader;
using GDShrapt.TypesMap;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides Godot type information from GDShrapt.TypesMap for the validation/inference system.
/// Implements IGDRuntimeProvider to bridge TypesMap data with GDShrapt.Reader.
/// </summary>
public class GDGodotTypesProvider : IGDRuntimeProvider
{
    private readonly GDAssemblyData? _assemblyData;
    private readonly Dictionary<string, GDTypeData> _typeCache = new();
    private readonly HashSet<string> _knownTypes = new();

    public GDGodotTypesProvider()
    {
        _assemblyData = GDTypeHelper.ExtractTypeDatasFromManifest();
        BuildTypeCache();
    }

    public GDGodotTypesProvider(GDAssemblyData assemblyData)
    {
        _assemblyData = assemblyData ?? throw new ArgumentNullException(nameof(assemblyData));
        BuildTypeCache();
    }

    private void BuildTypeCache()
    {
        if (_assemblyData?.TypeDatas == null)
            return;

        foreach (var kvp in _assemblyData.TypeDatas)
        {
            var gdScriptName = kvp.Key;
            _knownTypes.Add(gdScriptName);

            // Take the first type data for each GDScript name
            if (kvp.Value.Count > 0)
            {
                _typeCache[gdScriptName] = kvp.Value.Values.First();
            }
        }

        // Add primitive types
        foreach (var primitiveType in new[] { "void", "bool", "int", "float", "String", "Variant" })
        {
            _knownTypes.Add(primitiveType);
        }
    }

    public bool IsKnownType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return _knownTypes.Contains(typeName) || _typeCache.ContainsKey(typeName);
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        if (!_typeCache.TryGetValue(typeName, out var typeData))
            return null;

        var members = GetMembersInfo(typeData);

        return new GDRuntimeTypeInfo(typeData.GDScriptName, typeData.GDScriptBaseTypeName, true)
        {
            Members = members
        };
    }

    private List<GDRuntimeMemberInfo> GetMembersInfo(GDTypeData typeData)
    {
        var members = new List<GDRuntimeMemberInfo>();

        if (typeData.MethodDatas != null)
        {
            foreach (var methodKvp in typeData.MethodDatas)
            {
                if (methodKvp.Value.Count > 0)
                {
                    var method = methodKvp.Value[0];
                    var paramCount = method.Parameters?.Length ?? 0;
                    members.Add(GDRuntimeMemberInfo.Method(
                        method.GDScriptName,
                        method.GDScriptReturnTypeName ?? "Variant",
                        paramCount,
                        paramCount));
                }
            }
        }

        if (typeData.PropertyDatas != null)
        {
            foreach (var propKvp in typeData.PropertyDatas)
            {
                members.Add(GDRuntimeMemberInfo.Property(
                    propKvp.Value.GDScriptName,
                    propKvp.Value.GDScriptTypeName ?? "Variant",
                    propKvp.Value.IsStatic));
            }
        }

        if (typeData.SignalDatas != null)
        {
            foreach (var signalKvp in typeData.SignalDatas)
            {
                members.Add(GDRuntimeMemberInfo.Signal(signalKvp.Value.GDScriptName));
            }
        }

        if (typeData.Constants != null)
        {
            foreach (var constKvp in typeData.Constants)
            {
                members.Add(GDRuntimeMemberInfo.Constant(
                    constKvp.Value.GDScriptName ?? constKvp.Key,
                    constKvp.Value.CSharpValueTypeName ?? "Variant"));
            }
        }

        return members;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        return GetMemberInternal(typeName, memberName, new HashSet<string>());
    }

    private GDRuntimeMemberInfo? GetMemberInternal(string typeName, string memberName, HashSet<string> visited)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        // Prevent infinite recursion by tracking visited types
        if (!visited.Add(typeName))
            return null;

        if (!_typeCache.TryGetValue(typeName, out var typeData))
        {
            // Try to find in base type hierarchy
            return GetMemberFromBaseTypeInternal(typeName, memberName, visited);
        }

        // Check methods
        if (typeData.MethodDatas?.TryGetValue(memberName, out var methods) == true && methods.Count > 0)
        {
            var method = methods[0];
            var paramCount = method.Parameters?.Length ?? 0;
            return GDRuntimeMemberInfo.Method(
                method.GDScriptName,
                method.GDScriptReturnTypeName ?? "Variant",
                paramCount,
                paramCount);
        }

        // Check properties
        if (typeData.PropertyDatas?.TryGetValue(memberName, out var property) == true)
        {
            return GDRuntimeMemberInfo.Property(
                property.GDScriptName,
                property.GDScriptTypeName ?? "Variant",
                property.IsStatic);
        }

        // Check signals
        if (typeData.SignalDatas?.TryGetValue(memberName, out var signal) == true)
        {
            return GDRuntimeMemberInfo.Signal(signal.GDScriptName);
        }

        // Check constants
        if (typeData.Constants?.TryGetValue(memberName, out var constant) == true)
        {
            return GDRuntimeMemberInfo.Constant(constant.GDScriptName ?? memberName, constant.CSharpValueTypeName ?? "Variant");
        }

        // Check enums
        if (typeData.Enums?.TryGetValue(memberName, out var enumInfo) == true)
        {
            return GDRuntimeMemberInfo.Constant(enumInfo.CSharpEnumName ?? memberName, "int");
        }

        // Try base type
        if (!string.IsNullOrEmpty(typeData.GDScriptBaseTypeName))
        {
            return GetMemberInternal(typeData.GDScriptBaseTypeName, memberName, visited);
        }

        return null;
    }

    private GDRuntimeMemberInfo? GetMemberFromBaseType(string typeName, string memberName)
    {
        return GetMemberFromBaseTypeInternal(typeName, memberName, new HashSet<string>());
    }

    private GDRuntimeMemberInfo? GetMemberFromBaseTypeInternal(string typeName, string memberName, HashSet<string> visited)
    {
        var typeInfo = GetTypeInfo(typeName);
        if (typeInfo?.BaseType != null)
        {
            return GetMemberInternal(typeInfo.BaseType, memberName, visited);
        }
        return null;
    }

    public string? GetBaseType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        if (_typeCache.TryGetValue(typeName, out var typeData))
        {
            return typeData.GDScriptBaseTypeName;
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

        // Null can be assigned to any reference type
        if (sourceType == "null")
            return true;

        // Variant accepts anything
        if (targetType == "Variant")
            return true;

        // Numeric promotion
        if (sourceType == "int" && targetType == "float")
            return true;

        // Check inheritance chain
        var currentType = sourceType;
        while (!string.IsNullOrEmpty(currentType))
        {
            if (currentType == targetType)
                return true;

            currentType = GetBaseType(currentType);
        }

        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        if (string.IsNullOrEmpty(name) || _assemblyData?.GlobalData?.MethodDatas == null)
            return null;

        if (_assemblyData.GlobalData.MethodDatas.TryGetValue(name, out var methods) && methods.Count > 0)
        {
            var method = methods[0];
            var paramCount = method.Parameters?.Length ?? 0;
            return GDRuntimeFunctionInfo.Exact(
                method.GDScriptName,
                paramCount,
                method.GDScriptReturnTypeName ?? "Variant");
        }

        return null;
    }

    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        if (string.IsNullOrEmpty(className))
            return null;

        // Check global types (singletons)
        if (_assemblyData?.GlobalData?.GlobalTypes?.TryGetValue(className, out var proxy) == true)
        {
            return new GDRuntimeTypeInfo(className)
            {
                IsSingleton = true
            };
        }

        // Check regular types
        return GetTypeInfo(className);
    }

    public bool IsBuiltIn(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        // Check global constants
        if (_assemblyData?.GlobalData?.Constants?.ContainsKey(identifier) == true)
            return true;

        // Check global types
        if (_assemblyData?.GlobalData?.GlobalTypes?.ContainsKey(identifier) == true)
            return true;

        // Built-in constants
        return identifier switch
        {
            "PI" or "TAU" or "INF" or "NAN" => true,
            "true" or "false" or "null" => true,
            "self" or "super" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets all available method overloads for a type member.
    /// </summary>
    public IReadOnlyList<GDMethodData>? GetMethodOverloads(string typeName, string methodName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodName))
            return null;

        if (_typeCache.TryGetValue(typeName, out var typeData))
        {
            if (typeData.MethodDatas?.TryGetValue(methodName, out var methods) == true)
            {
                return methods;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all signals for a type.
    /// </summary>
    public IReadOnlyDictionary<string, GDSignalData>? GetSignals(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        if (_typeCache.TryGetValue(typeName, out var typeData))
        {
            return typeData.SignalDatas;
        }

        return null;
    }

    /// <summary>
    /// Gets the C# type name for a GDScript type.
    /// </summary>
    public string? GetCSharpTypeName(string gdScriptTypeName)
    {
        if (string.IsNullOrEmpty(gdScriptTypeName))
            return null;

        if (_typeCache.TryGetValue(gdScriptTypeName, out var typeData))
        {
            return $"{typeData.CSharpNamespace}.{typeData.CSharpName}";
        }

        return null;
    }

    /// <summary>
    /// Gets all known type names from this provider.
    /// </summary>
    public IEnumerable<string> GetAllTypes()
    {
        return _knownTypes;
    }
}
