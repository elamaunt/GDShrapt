using GDShrapt.Reader;
using GDShrapt.TypesMap;
using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, GDTypeData> _typeCache = new();
    private readonly ConcurrentDictionary<string, byte> _knownTypes = new(); // byte as placeholder for thread-safe set

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
            _knownTypes.TryAdd(gdScriptName, 0);

            // Take the first type data for each GDScript name
            if (kvp.Value.Count > 0)
            {
                _typeCache.TryAdd(gdScriptName, kvp.Value.Values.First());
            }
        }

        // Add all GlobalTypes from TypesMap (includes primitives, PackedArray types, Array, Dictionary, etc.)
        if (_assemblyData?.GlobalData?.GlobalTypes != null)
        {
            foreach (var globalType in _assemblyData.GlobalData.GlobalTypes.Keys)
            {
                _knownTypes.TryAdd(globalType, 0);
            }
        }
    }

    public bool IsKnownType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return _knownTypes.ContainsKey(typeName) || _typeCache.ContainsKey(typeName);
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

        // Check built-in type properties first (Vector2.x, Color.r, etc.)
        var builtinMember = GetBuiltinTypeMember(typeName, memberName);
        if (builtinMember != null)
            return builtinMember;

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
            var returnType = ConvertCSharpGenericToGDScript(
                method.GDScriptReturnTypeName,
                method.CSharpReturnTypeFullName) ?? "Variant";
            var memberInfo = GDRuntimeMemberInfo.Method(
                method.GDScriptName,
                returnType,
                minArgs,
                maxArgs,
                isVarArgs);
            // Assign parameters with callable metadata
            memberInfo.Parameters = CreateParameterList(method.Parameters);
            // Assign type inference metadata
            memberInfo.ReturnTypeRole = method.ReturnTypeRole;
            memberInfo.MergeTypeStrategy = method.MergeTypeStrategy;
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

    /// <summary>
    /// Looks up properties and methods for built-in value types from TypesMap data.
    /// These types have struct-like properties (x, y, z, r, g, b, etc.) that are essential for GDScript.
    /// </summary>
    private GDRuntimeMemberInfo? GetBuiltinTypeMember(string typeName, string memberName)
    {
        if (_assemblyData?.TypeDatas == null)
            return null;

        // Try to find type data by GDScript name (case-insensitive lookup)
        if (!_assemblyData.TypeDatas.TryGetValue(typeName, out var typeVariants))
            return null;

        // Get the first type data variant
        var typeData = typeVariants.Values.FirstOrDefault();
        if (typeData == null)
            return null;

        // Check methods
        if (typeData.MethodDatas?.TryGetValue(memberName, out var methods) == true && methods.Count > 0)
        {
            var method = methods[0];
            var (minArgs, maxArgs, isVarArgs) = CalculateArgConstraints(method.Parameters);
            var returnType = ConvertCSharpGenericToGDScript(
                method.GDScriptReturnTypeName,
                method.CSharpReturnTypeFullName) ?? "Variant";
            var memberInfo = GDRuntimeMemberInfo.Method(
                method.GDScriptName,
                returnType,
                minArgs,
                isVarArgs ? int.MaxValue : maxArgs,
                isVarArgs);
            memberInfo.Parameters = CreateParameterList(method.Parameters);
            // Assign type inference metadata
            memberInfo.ReturnTypeRole = method.ReturnTypeRole;
            memberInfo.MergeTypeStrategy = method.MergeTypeStrategy;
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

        // Check constants
        if (typeData.Constants?.TryGetValue(memberName, out var constant) == true)
        {
            return GDRuntimeMemberInfo.Constant(
                constant.GDScriptName ?? memberName,
                constant.CSharpValueTypeName ?? "Variant");
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

        // Variant as source can be assigned to any type (runtime type check will occur)
        // This is common in GDScript where untyped variables (Variant) are passed to typed contexts
        if (sourceType == "Variant")
            return true;

        // Numeric promotion
        if (sourceType == "int" && targetType == "float")
            return true;

        // String <-> StringName implicit conversion
        if ((sourceType == "String" && targetType == "StringName") ||
            (sourceType == "StringName" && targetType == "String"))
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

            // min(a, b, ...) - variadic, returns common type of args (int+int→int, int+float→float)
            "min" => GDRuntimeFunctionInfo.VarArgs("min", 2, "Variant", "common_arg"),

            // max(a, b, ...) - variadic, returns common type of args (int+int→int, int+float→float)
            "max" => GDRuntimeFunctionInfo.VarArgs("max", 2, "Variant", "common_arg"),

            // mini(a, b, ...) - variadic integer minimum, returns int
            "mini" => GDRuntimeFunctionInfo.VarArgs("mini", 2, "int"),

            // maxi(a, b, ...) - variadic integer maximum, returns int
            "maxi" => GDRuntimeFunctionInfo.VarArgs("maxi", 2, "int"),

            // minf(a, b) - float minimum, returns float
            "minf" => GDRuntimeFunctionInfo.Exact("minf", 2, "float"),

            // maxf(a, b) - float maximum, returns float
            "maxf" => GDRuntimeFunctionInfo.Exact("maxf", 2, "float"),

            // clamp(value, min, max) - returns type of first arg
            "clamp" => GDRuntimeFunctionInfo.Exact("clamp", 3, "Variant", "first_arg"),

            // clampi(value, min, max) - returns int
            "clampi" => GDRuntimeFunctionInfo.Exact("clampi", 3, "int"),

            // clampf(value, min, max) - returns float
            "clampf" => GDRuntimeFunctionInfo.Exact("clampf", 3, "float"),

            // abs(x) - returns type of first arg (int→int, float→float)
            "abs" => GDRuntimeFunctionInfo.Exact("abs", 1, "Variant", "first_arg"),

            // absi(x) - returns int
            "absi" => GDRuntimeFunctionInfo.Exact("absi", 1, "int"),

            // absf(x) - returns float
            "absf" => GDRuntimeFunctionInfo.Exact("absf", 1, "float"),

            // sign(x) - returns int (always -1, 0, or 1)
            "sign" => GDRuntimeFunctionInfo.Exact("sign", 1, "int"),

            // lerp(a, b, weight) - returns common type of a and b (ignoring weight)
            "lerp" => GDRuntimeFunctionInfo.Exact("lerp", 3, "Variant", "common_two"),

            // str(value, ...) - variadic, returns String. Accepts 0 or more args: str() returns ""
            "str" => GDRuntimeFunctionInfo.VarArgs("str", 0, "String"),

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
    /// Checks if a type is a builtin value type that can never be null.
    /// In Godot 4, this includes: int, float, bool, String, Vector2/3/4, Color,
    /// Transform2D/3D, Array, Dictionary, PackedArrays, etc.
    /// Uses GDTypeData.IsBuiltin from TypesMap for accurate detection.
    /// </summary>
    public bool IsBuiltinType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // Handle generic types: Array[int] -> Array, Dictionary[String,int] -> Dictionary
        var baseType = typeName;
        var bracketIndex = typeName.IndexOf('[');
        if (bracketIndex > 0)
            baseType = typeName.Substring(0, bracketIndex);

        // Check TypesMap cache (includes Vector2, Color, Array, Dictionary, etc.)
        if (_typeCache.TryGetValue(baseType, out var typeData))
            return typeData.IsBuiltin;

        // Check GlobalTypes (int, float, bool, String, etc. are value types)
        if (_assemblyData?.GlobalData?.GlobalTypes?.ContainsKey(baseType) == true)
            return true;

        return false;
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
        return _knownTypes.Keys;
    }

    /// <summary>
    /// Finds all types that have a specific method defined directly (not inherited).
    /// Used for duck typing inference to narrow down possible types.
    /// </summary>
    /// <param name="methodName">The method name to search for</param>
    /// <returns>List of type names that have this method</returns>
    public IReadOnlyList<string> FindTypesWithMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return Array.Empty<string>();

        var result = new List<string>();

        foreach (var kvp in _typeCache)
        {
            var typeName = kvp.Key;
            var typeData = kvp.Value;

            if (typeData.MethodDatas?.ContainsKey(methodName) == true)
            {
                result.Add(typeName);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds all types that have a specific property defined directly (not inherited).
    /// Used for duck typing inference to narrow down possible types.
    /// </summary>
    /// <param name="propertyName">The property name to search for</param>
    /// <returns>List of type names that have this property</returns>
    public IReadOnlyList<string> FindTypesWithProperty(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return Array.Empty<string>();

        var result = new List<string>();

        foreach (var kvp in _typeCache)
        {
            var typeName = kvp.Key;
            var typeData = kvp.Value;

            if (typeData.PropertyDatas?.ContainsKey(propertyName) == true)
            {
                result.Add(typeName);
            }
        }

        return result;
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
    /// Includes callable metadata for type inference (filter, map, reduce, etc.)
    /// </summary>
    private static IReadOnlyList<GDRuntimeParameterInfo>? CreateParameterList(GDParameterInfo[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return null;

        return parameters.Select(p => new GDRuntimeParameterInfo(
            p.CSharpName ?? "arg",
            p.GDScriptTypeName ?? "Variant",
            p.HasDefaultValue,
            p.IsParams,
            p.CallableReceivesType,
            p.CallableReturnsType,
            p.CallableParameterCount
        )).ToList();
    }

    /// <summary>
    /// Converts C# generic type notation to GDScript notation.
    /// E.g., "Array`1" with CSharpFullName "Godot.Collections.Array`1[[Godot.Node, ...]]" → "Array[Node]"
    /// </summary>
    internal static string? ConvertCSharpGenericToGDScript(string? gdScriptTypeName, string? csharpFullTypeName)
    {
        // If not a generic type (no backtick), return as-is
        // Note: backtick character is Unicode 0x60
        if (gdScriptTypeName == null)
            return gdScriptTypeName;

        // Check for backtick character (0x60)
        var backtickIndex = gdScriptTypeName.IndexOf('\u0060');
        if (backtickIndex < 0)
            return gdScriptTypeName;

        // Extract base type name (before backtick)
        var baseTypeName = gdScriptTypeName.Substring(0, backtickIndex);

        // Try to extract generic argument from C# full type name
        if (!string.IsNullOrEmpty(csharpFullTypeName))
        {
            var genericArg = ExtractGenericTypeArgument(csharpFullTypeName);
            if (!string.IsNullOrEmpty(genericArg))
            {
                return $"{baseTypeName}[{genericArg}]";
            }
        }

        // Fallback: return base type without generic parameter
        return baseTypeName;
    }

    /// <summary>
    /// Extracts the GDScript type name from a C# generic type full name.
    /// E.g., "Godot.Collections.Array`1[[Godot.Node, GodotSharp, ...]]" → "Node"
    /// </summary>
    private static string? ExtractGenericTypeArgument(string csharpFullTypeName)
    {
        // Pattern: TypeName`N[[GenericArg1, Assembly, ...], [GenericArg2, Assembly, ...]]
        // Find the first [[ which starts generic arguments
        var startIndex = csharpFullTypeName.IndexOf("[[", StringComparison.Ordinal);
        if (startIndex < 0)
            return null;

        startIndex += 2; // Skip "[["

        // Find the comma or ] that ends the type name
        var endIndex = csharpFullTypeName.IndexOfAny(new[] { ',', ']' }, startIndex);
        if (endIndex < 0)
            return null;

        var fullArgTypeName = csharpFullTypeName.Substring(startIndex, endIndex - startIndex);

        // Convert Godot full name to GDScript name
        // E.g., "Godot.Node" → "Node", "Godot.Vector2" → "Vector2"
        return ConvertCSharpTypeNameToGDScript(fullArgTypeName);
    }

    /// <summary>
    /// Converts a C# type name to its GDScript equivalent.
    /// E.g., "Godot.Node" → "Node", "System.Int32" → "int"
    /// </summary>
    private static string ConvertCSharpTypeNameToGDScript(string csharpTypeName)
    {
        // Handle common C# to GDScript mappings
        return csharpTypeName switch
        {
            "System.Int32" or "System.Int64" or "Int32" or "Int64" => "int",
            "System.Single" or "System.Double" or "Single" or "Double" => "float",
            "System.Boolean" or "Boolean" => "bool",
            "System.String" or "String" => "String",
            _ => ExtractSimpleTypeName(csharpTypeName)
        };
    }

    /// <summary>
    /// Extracts simple type name from full name.
    /// E.g., "Godot.Node" → "Node", "Godot.Collections.Array" → "Array"
    /// </summary>
    private static string ExtractSimpleTypeName(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < fullTypeName.Length - 1)
            return fullTypeName.Substring(lastDotIndex + 1);
        return fullTypeName;
    }
}
