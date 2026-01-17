using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides type information from the current project's GDScript files.
/// Tracks class names, methods, properties, and signals from project scripts.
/// </summary>
public class GDProjectTypesProvider : IGDRuntimeProvider
{
    private readonly IGDScriptProvider _scriptProvider;
    private readonly Dictionary<string, GDProjectTypeInfo> _typeCache = new();
    // Index by script path (for extends "res://path/to/script.gd" support)
    private readonly Dictionary<string, string> _pathToTypeName = new(StringComparer.OrdinalIgnoreCase);

    public GDProjectTypesProvider(IGDScriptProvider scriptProvider)
    {
        _scriptProvider = scriptProvider ?? throw new ArgumentNullException(nameof(scriptProvider));
    }

    /// <summary>
    /// Rebuilds the type cache from the script provider.
    /// Should be called when scripts are added, removed, or modified.
    /// </summary>
    public void RebuildCache()
    {
        _typeCache.Clear();
        _pathToTypeName.Clear();

        foreach (var scriptInfo in _scriptProvider.Scripts)
        {
            if (scriptInfo.Class == null)
                continue;

            var typeName = scriptInfo.TypeName;
            if (string.IsNullOrEmpty(typeName))
                continue;

            var typeInfo = BuildTypeInfo(scriptInfo);
            _typeCache[typeName] = typeInfo;

            // Index by script path for "extends 'res://path/to/script.gd'" support
            if (!string.IsNullOrEmpty(scriptInfo.FullPath))
            {
                // Store both the full path and the res:// path
                _pathToTypeName[scriptInfo.FullPath] = typeName;

                // Also store the res:// path if available
                var resPath = scriptInfo.ResPath;
                if (!string.IsNullOrEmpty(resPath))
                {
                    _pathToTypeName[resPath] = typeName;
                    // Also without quotes (in case BuildName returns "res://..." with quotes)
                    _pathToTypeName[$"\"{resPath}\""] = typeName;
                }
            }
        }
    }

    private GDProjectTypeInfo BuildTypeInfo(IGDScriptInfo scriptInfo)
    {
        var classDecl = scriptInfo.Class!;

        // Check if class is marked @abstract
        var isClassAbstract = classDecl.CustomAttributes
            .Any(attr => attr.Attribute?.IsAbstract() == true);

        var info = new GDProjectTypeInfo
        {
            Name = scriptInfo.TypeName ?? "",
            ScriptPath = scriptInfo.FullPath,
            BaseTypeName = classDecl.Extends?.Type?.BuildName(),
            IsAbstract = isClassAbstract
        };

        // Extract members
        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDMethodDeclaration method when method.Identifier != null:
                    // Check if method is marked @abstract
                    var isMethodAbstract = method.AttributesDeclaredBefore
                        .Any(attr => attr.Attribute?.IsAbstract() == true);

                    info.Methods[method.Identifier.Sequence] = new GDProjectMethodInfo
                    {
                        Name = method.Identifier.Sequence,
                        ReturnTypeName = method.ReturnType?.BuildName() ?? "Variant",
                        IsStatic = method.IsStatic,
                        IsAbstract = isMethodAbstract,
                        Parameters = method.Parameters?
                            .Where(p => p.Identifier != null)
                            .Select(p => new GDProjectParameterInfo
                            {
                                Name = p.Identifier!.Sequence,
                                TypeName = p.Type?.BuildName() ?? "Variant",
                                HasDefaultValue = p.DefaultValue != null
                            })
                            .ToList() ?? new()
                    };
                    break;

                case GDVariableDeclaration variable when variable.Identifier != null:
                    info.Properties[variable.Identifier.Sequence] = new GDProjectPropertyInfo
                    {
                        Name = variable.Identifier.Sequence,
                        TypeName = variable.Type?.BuildName() ?? "Variant",
                        IsConstant = variable.ConstKeyword != null,
                        IsStatic = variable.StaticKeyword != null
                    };
                    break;

                case GDSignalDeclaration signal when signal.Identifier != null:
                    info.Signals[signal.Identifier.Sequence] = new GDProjectSignalInfo
                    {
                        Name = signal.Identifier.Sequence,
                        Parameters = signal.Parameters?
                            .Where(p => p.Identifier != null)
                            .Select(p => new GDProjectParameterInfo
                            {
                                Name = p.Identifier!.Sequence,
                                TypeName = p.Type?.BuildName() ?? "Variant"
                            })
                            .ToList() ?? new()
                    };
                    break;

                case GDEnumDeclaration enumDecl when enumDecl.Identifier != null:
                    info.Enums[enumDecl.Identifier.Sequence] = new GDProjectEnumInfo
                    {
                        Name = enumDecl.Identifier.Sequence,
                        Values = enumDecl.Values?
                            .Where(v => v.Identifier != null)
                            .ToDictionary(v => v.Identifier!.Sequence, v => 0) ?? new()
                    };
                    break;

                case GDInnerClassDeclaration innerClass when innerClass.Identifier != null:
                    info.InnerClasses.Add(innerClass.Identifier.Sequence);
                    break;
            }
        }

        return info;
    }

    public bool IsKnownType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // Try direct lookup first (class_name)
        if (_typeCache.ContainsKey(typeName))
            return true;

        // Try path-based lookup (extends "res://path/to/script.gd")
        return _pathToTypeName.ContainsKey(typeName);
    }

    /// <summary>
    /// Resolves a type name that might be a path to the actual class name.
    /// Handles both class_name and path-based extends.
    /// </summary>
    private string? ResolveTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Direct class_name lookup
        if (_typeCache.ContainsKey(typeName))
            return typeName;

        // Path-based lookup (extends "res://path/to/script.gd")
        if (_pathToTypeName.TryGetValue(typeName, out var resolvedName))
            return resolvedName;

        return null;
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Resolve path-based type names to class_name
        var resolvedName = ResolveTypeName(typeName);
        if (resolvedName == null || !_typeCache.TryGetValue(resolvedName, out var projectType))
            return null;

        var members = GetAllMembers(projectType);

        return new GDRuntimeTypeInfo(projectType.Name, projectType.BaseTypeName)
        {
            Members = members,
            IsAbstract = projectType.IsAbstract
        };
    }

    private List<GDRuntimeMemberInfo> GetAllMembers(GDProjectTypeInfo typeInfo)
    {
        var members = new List<GDRuntimeMemberInfo>();

        foreach (var method in typeInfo.Methods.Values)
        {
            var (minArgs, maxArgs) = CalculateArgConstraints(method.Parameters);
            members.Add(GDRuntimeMemberInfo.Method(
                method.Name,
                method.ReturnTypeName,
                minArgs,
                maxArgs,
                isVarArgs: false,
                isStatic: method.IsStatic,
                isAbstract: method.IsAbstract));
        }

        foreach (var prop in typeInfo.Properties.Values)
        {
            if (prop.IsConstant)
                members.Add(GDRuntimeMemberInfo.Constant(prop.Name, prop.TypeName));
            else
                members.Add(GDRuntimeMemberInfo.Property(prop.Name, prop.TypeName, prop.IsStatic));
        }

        foreach (var signal in typeInfo.Signals.Values)
        {
            members.Add(GDRuntimeMemberInfo.Signal(signal.Name));
        }

        foreach (var enumInfo in typeInfo.Enums.Values)
        {
            members.Add(GDRuntimeMemberInfo.Constant(enumInfo.Name, enumInfo.Name));
        }

        return members;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        var (member, _) = GetMemberWithDeclaringType(typeName, memberName);
        return member;
    }

    /// <summary>
    /// Gets a member and the type that declares it.
    /// Walks up the inheritance chain to find inherited members.
    /// </summary>
    /// <param name="typeName">The type to search in.</param>
    /// <param name="memberName">The member name to find.</param>
    /// <returns>A tuple of (member info, declaring type name) or (null, null) if not found.</returns>
    public (GDRuntimeMemberInfo? Member, string? DeclaringTypeName) GetMemberWithDeclaringType(string typeName, string memberName)
    {
        return GetMemberWithDeclaringTypeInternal(typeName, memberName, new HashSet<string>());
    }

    private (GDRuntimeMemberInfo? Member, string? DeclaringTypeName) GetMemberWithDeclaringTypeInternal(
        string typeName, string memberName, HashSet<string> visited)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return (null, null);

        // Resolve path-based type names to class_name
        var resolvedName = ResolveTypeName(typeName);
        if (resolvedName == null || !_typeCache.TryGetValue(resolvedName, out var projectType))
            return (null, null);

        // Prevent infinite recursion by tracking visited types
        if (!visited.Add(resolvedName))
            return (null, null);

        // Use resolved name for return value (class_name, not path)
        typeName = resolvedName;

        // Check methods
        if (projectType.Methods.TryGetValue(memberName, out var method))
        {
            var memberInfo = GDRuntimeMemberInfo.Method(
                method.Name,
                method.ReturnTypeName,
                method.Parameters.Count,
                method.Parameters.Count,
                isVarArgs: false,
                isStatic: method.IsStatic,
                isAbstract: method.IsAbstract);
            return (memberInfo, typeName);
        }

        // Check properties
        if (projectType.Properties.TryGetValue(memberName, out var property))
        {
            GDRuntimeMemberInfo memberInfo;
            if (property.IsConstant)
                memberInfo = GDRuntimeMemberInfo.Constant(property.Name, property.TypeName);
            else
                memberInfo = GDRuntimeMemberInfo.Property(property.Name, property.TypeName, property.IsStatic);
            return (memberInfo, typeName);
        }

        // Check signals
        if (projectType.Signals.TryGetValue(memberName, out var signal))
        {
            return (GDRuntimeMemberInfo.Signal(signal.Name), typeName);
        }

        // Check enums
        if (projectType.Enums.TryGetValue(memberName, out var enumInfo))
        {
            return (GDRuntimeMemberInfo.Constant(enumInfo.Name, enumInfo.Name), typeName);
        }

        // Check base type - propagate the declaring type from base
        if (!string.IsNullOrEmpty(projectType.BaseTypeName))
        {
            return GetMemberWithDeclaringTypeInternal(projectType.BaseTypeName, memberName, visited);
        }

        return (null, null);
    }

    public string? GetBaseType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Resolve path-based type names to class_name
        var resolvedName = ResolveTypeName(typeName);
        if (resolvedName != null && _typeCache.TryGetValue(resolvedName, out var projectType))
        {
            return projectType.BaseTypeName;
        }

        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        if (sourceType == targetType)
            return true;

        // Check inheritance chain with cycle detection
        var visited = new HashSet<string>();
        var current = sourceType;
        while (!string.IsNullOrEmpty(current))
        {
            if (!visited.Add(current))
                return false; // Cycle detected

            if (current == targetType)
                return true;

            current = GetBaseType(current);
        }

        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        // Project types don't provide global functions
        return null;
    }

    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        // Check if it's a global class (class_name declaration)
        return GetTypeInfo(className);
    }

    public bool IsBuiltIn(string identifier)
    {
        // Project types are not built-ins
        return false;
    }

    /// <summary>
    /// Gets the script info for a type name.
    /// </summary>
    public IGDScriptInfo? GetScriptInfoForType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        return _scriptProvider.GetScriptByTypeName(typeName);
    }

    /// <summary>
    /// Gets method information with full details.
    /// </summary>
    public GDProjectMethodInfo? GetMethodInfo(string typeName, string methodName)
    {
        if (_typeCache.TryGetValue(typeName, out var typeInfo))
        {
            if (typeInfo.Methods.TryGetValue(methodName, out var method))
            {
                return method;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the type of a member (property or variable) in the specified type.
    /// </summary>
    public string? GetMemberType(string typeName, string memberName)
    {
        return GetMemberTypeInternal(typeName, memberName, new HashSet<string>());
    }

    private string? GetMemberTypeInternal(string typeName, string memberName, HashSet<string> visited)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        // Resolve path-based type names to class_name
        var resolvedName = ResolveTypeName(typeName);
        if (resolvedName == null || !_typeCache.TryGetValue(resolvedName, out var projectType))
            return null;

        // Prevent infinite recursion by tracking visited types
        if (!visited.Add(resolvedName))
            return null;

        // Check properties
        if (projectType.Properties.TryGetValue(memberName, out var property))
        {
            return property.TypeName;
        }

        // Check base type
        if (!string.IsNullOrEmpty(projectType.BaseTypeName))
        {
            return GetMemberTypeInternal(projectType.BaseTypeName, memberName, visited);
        }

        return null;
    }

    public IEnumerable<string> GetAllTypes()
    {
        return _typeCache.Keys;
    }

    /// <summary>
    /// Calculates MinArgs and MaxArgs from project parameter information.
    /// Note: GDScript does not support variadic parameters in user-defined functions.
    /// </summary>
    private static (int MinArgs, int MaxArgs) CalculateArgConstraints(List<GDProjectParameterInfo> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return (0, 0);

        int minArgs = parameters.Count(p => !p.HasDefaultValue);
        int maxArgs = parameters.Count;

        return (minArgs, maxArgs);
    }
}

/// <summary>
/// Type information for a project script.
/// </summary>
public class GDProjectTypeInfo
{
    public string Name { get; init; } = "";
    public string? ScriptPath { get; init; }
    public string? BaseTypeName { get; init; }
    public bool IsAbstract { get; init; }
    public Dictionary<string, GDProjectMethodInfo> Methods { get; } = new();
    public Dictionary<string, GDProjectPropertyInfo> Properties { get; } = new();
    public Dictionary<string, GDProjectSignalInfo> Signals { get; } = new();
    public Dictionary<string, GDProjectEnumInfo> Enums { get; } = new();
    public List<string> InnerClasses { get; } = new();
}

public class GDProjectMethodInfo
{
    public string Name { get; init; } = "";
    public string ReturnTypeName { get; init; } = "Variant";
    public bool IsStatic { get; init; }
    public bool IsAbstract { get; init; }
    public List<GDProjectParameterInfo> Parameters { get; init; } = new();
}

public class GDProjectPropertyInfo
{
    public string Name { get; init; } = "";
    public string TypeName { get; init; } = "Variant";
    public bool IsConstant { get; init; }
    public bool IsStatic { get; init; }
}

public class GDProjectSignalInfo
{
    public string Name { get; init; } = "";
    public List<GDProjectParameterInfo> Parameters { get; init; } = new();
}

public class GDProjectParameterInfo
{
    public string Name { get; init; } = "";
    public string TypeName { get; init; } = "Variant";
    public bool HasDefaultValue { get; init; }
}

public class GDProjectEnumInfo
{
    public string Name { get; init; } = "";
    public Dictionary<string, int> Values { get; init; } = new();
}
