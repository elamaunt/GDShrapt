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

        // Add primitive types that are not in TypeDatas
        foreach (var primitiveType in new[] { "void", "bool", "int", "float", "String", "Variant" })
        {
            _knownTypes.Add(primitiveType);
        }

        // Add all GlobalTypes from TypesMap (includes PackedArray types, Array, Dictionary, etc.)
        if (_assemblyData?.GlobalData?.GlobalTypes != null)
        {
            foreach (var globalType in _assemblyData.GlobalData.GlobalTypes.Keys)
            {
                _knownTypes.Add(globalType);
            }
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
                    var (minArgs, maxArgs, isVarArgs) = CalculateArgConstraints(method.Parameters);
                    members.Add(GDRuntimeMemberInfo.Method(
                        method.GDScriptName,
                        method.GDScriptReturnTypeName ?? "Variant",
                        minArgs,
                        maxArgs,
                        isVarArgs));
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
            var (minArgs, maxArgs, isVarArgs) = CalculateArgConstraints(method.Parameters);
            var memberInfo = GDRuntimeMemberInfo.Method(
                method.GDScriptName,
                method.GDScriptReturnTypeName ?? "Variant",
                minArgs,
                maxArgs,
                isVarArgs);
            // Assign parameters
            memberInfo.Parameters = CreateParameterList(method.Parameters);
            return memberInfo;
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
            var baseType = typeData.GDScriptBaseTypeName;
            // Prevent self-referential base type (Object -> Object creates infinite loop)
            if (baseType == typeName)
                return null;
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

        // Null can be assigned to any reference type
        if (sourceType == "null")
            return true;

        // Variant accepts anything
        if (targetType == "Variant")
            return true;

        // Numeric promotion
        if (sourceType == "int" && targetType == "float")
            return true;

        // Extract base type names for generics (Array[int] -> Array)
        var sourceBaseTypeName = ExtractBaseTypeName(sourceType);
        var targetBaseTypeName = ExtractBaseTypeName(targetType);

        // Generic type is assignable to its non-generic base (Array[int] -> Array)
        if (sourceBaseTypeName == targetBaseTypeName && sourceBaseTypeName != sourceType)
            return true;

        // Check inheritance chain with cycle protection
        var visited = new HashSet<string>();
        var currentType = sourceBaseTypeName;
        while (!string.IsNullOrEmpty(currentType) && visited.Add(currentType))
        {
            if (currentType == targetBaseTypeName)
                return true;

            var baseType = GetBaseType(currentType);

            // Stop if base type is the same as current (self-referential)
            if (baseType == currentType)
                break;

            currentType = baseType;
        }

        return false;
    }

    /// <summary>
    /// Extracts the base type name from a generic type.
    /// For example: "Array[int]" -> "Array", "Dictionary[String, int]" -> "Dictionary"
    /// </summary>
    private static string ExtractBaseTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var bracketIndex = typeName.IndexOf('[');
        if (bracketIndex > 0)
            return typeName.Substring(0, bracketIndex);

        return typeName;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Handle GDScript-specific functions that have different signatures than their C# counterparts
        var specialCase = GetSpecialCaseGlobalFunction(name);
        if (specialCase != null)
            return specialCase;

        if (_assemblyData?.GlobalData?.MethodDatas == null)
            return null;

        if (_assemblyData.GlobalData.MethodDatas.TryGetValue(name, out var methods) && methods.Count > 0)
        {
            var method = methods[0];
            // Consider ALL overloads to calculate the full range of acceptable argument counts
            var (minArgs, maxArgs, isVarArgs) = CalculateArgConstraintsFromOverloads(methods);
            var returnType = method.GDScriptReturnTypeName ?? "Variant";

            // Create parameter list from first overload
            var parameters = CreateParameterList(method.Parameters);

            GDRuntimeFunctionInfo funcInfo;
            if (isVarArgs)
            {
                funcInfo = GDRuntimeFunctionInfo.VarArgs(name, minArgs, returnType);
            }
            else if (minArgs == maxArgs)
            {
                funcInfo = GDRuntimeFunctionInfo.Exact(name, minArgs, returnType);
            }
            else
            {
                funcInfo = GDRuntimeFunctionInfo.Range(name, minArgs, maxArgs, returnType);
            }

            // Assign parameters
            funcInfo.Parameters = parameters;
            return funcInfo;
        }

        return null;
    }

    /// <summary>
    /// Handles GDScript-specific global functions that have different parameter signatures
    /// than their C# counterparts in the TypesMap.
    /// </summary>
    private static GDRuntimeFunctionInfo? GetSpecialCaseGlobalFunction(string name)
    {
        return name switch
        {
            // range(end), range(begin, end), range(begin, end, step)
            "range" => GDRuntimeFunctionInfo.Range("range", 1, 3, "Array"),

            // assert(condition), assert(condition, message)
            "assert" => GDRuntimeFunctionInfo.Range("assert", 1, 2, "void"),

            _ => null
        };
    }

    /// <summary>
    /// Calculates MinArgs, MaxArgs, and IsVarArgs from ALL method overloads.
    /// Takes the minimum MinArgs and maximum MaxArgs across all overloads.
    /// </summary>
    private static (int MinArgs, int MaxArgs, bool IsVarArgs) CalculateArgConstraintsFromOverloads(List<GDMethodData> methods)
    {
        if (methods == null || methods.Count == 0)
            return (0, 0, false);

        int overallMinArgs = int.MaxValue;
        int overallMaxArgs = int.MinValue;
        bool anyVarArgs = false;

        foreach (var method in methods)
        {
            var (minArgs, maxArgs, isVarArgs) = CalculateArgConstraints(method.Parameters);

            if (minArgs < overallMinArgs)
                overallMinArgs = minArgs;

            if (isVarArgs)
            {
                anyVarArgs = true;
            }
            else if (maxArgs > overallMaxArgs)
            {
                overallMaxArgs = maxArgs;
            }
        }

        // If any overload is varargs, the function supports unlimited args
        if (anyVarArgs)
            return (overallMinArgs, -1, true);

        // Handle edge case where no valid overloads were found
        if (overallMinArgs == int.MaxValue)
            overallMinArgs = 0;
        if (overallMaxArgs == int.MinValue)
            overallMaxArgs = 0;

        return (overallMinArgs, overallMaxArgs, false);
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

        // Check global enum constants (KEY_SPACE, KEY_ENTER, MOUSE_BUTTON_LEFT, etc.)
        if (_assemblyData?.GlobalData?.EnumsConstants?.ContainsKey(identifier) == true)
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

    /// <summary>
    /// Calculates MinArgs, MaxArgs, and IsVarArgs from parameter information.
    /// </summary>
    private static (int MinArgs, int MaxArgs, bool IsVarArgs) CalculateArgConstraints(GDParameterInfo[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return (0, 0, false);

        // Check if last parameter is variadic (params)
        bool isVarArgs = parameters[^1].IsParams;

        // MinArgs = count of parameters WITHOUT HasDefaultValue and not params
        int minArgs = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.IsParams)
                continue;
            if (param.HasDefaultValue)
                continue;
            minArgs++;
        }

        // MaxArgs = -1 if varargs, otherwise total count
        int maxArgs = isVarArgs ? -1 : parameters.Length;

        return (minArgs, maxArgs, isVarArgs);
    }

    /// <summary>
    /// Creates a list of GDRuntimeParameterInfo from GDParameterInfo array.
    /// </summary>
    private static IReadOnlyList<GDRuntimeParameterInfo>? CreateParameterList(GDParameterInfo[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return null;

        return parameters.Select(p => new GDRuntimeParameterInfo(
            p.CSharpName ?? "arg",
            p.GDScriptTypeName ?? "Variant",
            p.HasDefaultValue,
            p.IsParams
        )).ToList();
    }
}
