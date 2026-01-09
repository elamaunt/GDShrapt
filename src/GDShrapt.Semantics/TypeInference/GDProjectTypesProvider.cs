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

        foreach (var scriptInfo in _scriptProvider.Scripts)
        {
            if (scriptInfo.Class == null)
                continue;

            var typeName = scriptInfo.TypeName;
            if (string.IsNullOrEmpty(typeName))
                continue;

            var typeInfo = BuildTypeInfo(scriptInfo);
            _typeCache[typeName] = typeInfo;
        }
    }

    private GDProjectTypeInfo BuildTypeInfo(IGDScriptInfo scriptInfo)
    {
        var classDecl = scriptInfo.Class!;
        var info = new GDProjectTypeInfo
        {
            Name = scriptInfo.TypeName ?? "",
            ScriptPath = scriptInfo.FullPath,
            BaseTypeName = classDecl.Extends?.Type?.BuildName()
        };

        // Extract members
        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDMethodDeclaration method when method.Identifier != null:
                    info.Methods[method.Identifier.Sequence] = new GDProjectMethodInfo
                    {
                        Name = method.Identifier.Sequence,
                        ReturnTypeName = method.ReturnType?.BuildName() ?? "Variant",
                        IsStatic = method.IsStatic,
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

        return _typeCache.ContainsKey(typeName);
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        if (!_typeCache.TryGetValue(typeName, out var projectType))
            return null;

        var members = GetAllMembers(projectType);

        return new GDRuntimeTypeInfo(projectType.Name, projectType.BaseTypeName)
        {
            Members = members
        };
    }

    private List<GDRuntimeMemberInfo> GetAllMembers(GDProjectTypeInfo typeInfo)
    {
        var members = new List<GDRuntimeMemberInfo>();

        foreach (var method in typeInfo.Methods.Values)
        {
            members.Add(GDRuntimeMemberInfo.Method(
                method.Name,
                method.ReturnTypeName,
                method.Parameters.Count,
                method.Parameters.Count,
                isVarArgs: false,
                isStatic: method.IsStatic));
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
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        if (!_typeCache.TryGetValue(typeName, out var projectType))
            return null;

        // Check methods
        if (projectType.Methods.TryGetValue(memberName, out var method))
        {
            return GDRuntimeMemberInfo.Method(
                method.Name,
                method.ReturnTypeName,
                method.Parameters.Count,
                method.Parameters.Count,
                isVarArgs: false,
                isStatic: method.IsStatic);
        }

        // Check properties
        if (projectType.Properties.TryGetValue(memberName, out var property))
        {
            if (property.IsConstant)
                return GDRuntimeMemberInfo.Constant(property.Name, property.TypeName);
            else
                return GDRuntimeMemberInfo.Property(property.Name, property.TypeName, property.IsStatic);
        }

        // Check signals
        if (projectType.Signals.TryGetValue(memberName, out var signal))
        {
            return GDRuntimeMemberInfo.Signal(signal.Name);
        }

        // Check enums
        if (projectType.Enums.TryGetValue(memberName, out var enumInfo))
        {
            return GDRuntimeMemberInfo.Constant(enumInfo.Name, enumInfo.Name);
        }

        // Check base type
        if (!string.IsNullOrEmpty(projectType.BaseTypeName))
        {
            return GetMember(projectType.BaseTypeName, memberName);
        }

        return null;
    }

    public string? GetBaseType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        if (_typeCache.TryGetValue(typeName, out var projectType))
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

        // Check inheritance chain
        var current = sourceType;
        while (!string.IsNullOrEmpty(current))
        {
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
}

/// <summary>
/// Type information for a project script.
/// </summary>
public class GDProjectTypeInfo
{
    public string Name { get; init; } = "";
    public string? ScriptPath { get; init; }
    public string? BaseTypeName { get; init; }
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
