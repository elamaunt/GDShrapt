using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Provides type information from the current Godot project's GDScript files.
/// Tracks class names, methods, properties, and signals from project scripts.
/// </summary>
internal class ProjectTypesProvider : IGDRuntimeProvider
{
    private readonly GDProjectMap _projectMap;
    private readonly Dictionary<string, ProjectTypeInfo> _typeCache = new();

    public ProjectTypesProvider(GDProjectMap projectMap)
    {
        _projectMap = projectMap ?? throw new ArgumentNullException(nameof(projectMap));
    }

    /// <summary>
    /// Rebuilds the type cache from the project map.
    /// Should be called when scripts are added, removed, or modified.
    /// </summary>
    public void RebuildCache()
    {
        _typeCache.Clear();

        if (_projectMap.Scripts == null)
            return;

        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (scriptMap.Class == null)
                continue;

            var typeName = scriptMap.TypeName;
            if (string.IsNullOrEmpty(typeName))
                continue;

            var typeInfo = BuildTypeInfo(scriptMap);
            _typeCache[typeName] = typeInfo;
        }
    }

    private ProjectTypeInfo BuildTypeInfo(GDScriptMap scriptMap)
    {
        var classDecl = scriptMap.Class;
        var info = new ProjectTypeInfo
        {
            Name = scriptMap.TypeName,
            ScriptPath = scriptMap.Reference?.FullPath,
            BaseTypeName = classDecl.Extends?.Type?.BuildName()
        };

        // Extract members
        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDMethodDeclaration method when method.Identifier != null:
                    info.Methods[method.Identifier.Sequence] = new ProjectMethodInfo
                    {
                        Name = method.Identifier.Sequence,
                        ReturnTypeName = method.ReturnType?.BuildName() ?? "Variant",
                        IsStatic = method.IsStatic,
                        Parameters = method.Parameters?
                            .Where(p => p.Identifier != null)
                            .Select(p => new ProjectParameterInfo
                            {
                                Name = p.Identifier!.Sequence,
                                TypeName = p.Type?.BuildName() ?? "Variant",
                                HasDefaultValue = p.DefaultValue != null
                            })
                            .ToList() ?? new()
                    };
                    break;

                case GDVariableDeclaration variable when variable.Identifier != null:
                    info.Properties[variable.Identifier.Sequence] = new ProjectPropertyInfo
                    {
                        Name = variable.Identifier.Sequence,
                        TypeName = variable.Type?.BuildName() ?? "Variant",
                        IsConstant = variable.ConstKeyword != null,
                        IsStatic = variable.StaticKeyword != null
                    };
                    break;

                case GDSignalDeclaration signal when signal.Identifier != null:
                    info.Signals[signal.Identifier.Sequence] = new ProjectSignalInfo
                    {
                        Name = signal.Identifier.Sequence,
                        Parameters = signal.Parameters?
                            .Where(p => p.Identifier != null)
                            .Select(p => new ProjectParameterInfo
                            {
                                Name = p.Identifier!.Sequence,
                                TypeName = p.Type?.BuildName() ?? "Variant"
                            })
                            .ToList() ?? new()
                    };
                    break;

                case GDEnumDeclaration enumDecl when enumDecl.Identifier != null:
                    info.Enums[enumDecl.Identifier.Sequence] = new ProjectEnumInfo
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

    public GDRuntimeTypeInfo GetTypeInfo(string typeName)
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

    private List<GDRuntimeMemberInfo> GetAllMembers(ProjectTypeInfo typeInfo)
    {
        var members = new List<GDRuntimeMemberInfo>();

        foreach (var method in typeInfo.Methods.Values)
        {
            members.Add(GDRuntimeMemberInfo.Method(
                method.Name,
                method.ReturnTypeName,
                method.Parameters.Count,
                method.Parameters.Count));
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

    public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
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
                method.Parameters.Count);
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

    public string GetBaseType(string typeName)
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

    public GDRuntimeFunctionInfo GetGlobalFunction(string name)
    {
        // Project types don't provide global functions
        return null;
    }

    public GDRuntimeTypeInfo GetGlobalClass(string className)
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
    /// Gets the script map for a type name.
    /// </summary>
    public GDScriptMap? GetScriptMapForType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        return _projectMap.GetScriptMapByTypeName(typeName);
    }

    /// <summary>
    /// Gets method information with full details.
    /// </summary>
    public ProjectMethodInfo? GetMethodInfo(string typeName, string methodName)
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
internal class ProjectTypeInfo
{
    public string Name { get; init; } = "";
    public string? ScriptPath { get; init; }
    public string? BaseTypeName { get; init; }
    public Dictionary<string, ProjectMethodInfo> Methods { get; } = new();
    public Dictionary<string, ProjectPropertyInfo> Properties { get; } = new();
    public Dictionary<string, ProjectSignalInfo> Signals { get; } = new();
    public Dictionary<string, ProjectEnumInfo> Enums { get; } = new();
    public List<string> InnerClasses { get; } = new();
}

internal class ProjectMethodInfo
{
    public string Name { get; init; } = "";
    public string ReturnTypeName { get; init; } = "Variant";
    public bool IsStatic { get; init; }
    public List<ProjectParameterInfo> Parameters { get; init; } = new();
}

internal class ProjectPropertyInfo
{
    public string Name { get; init; } = "";
    public string TypeName { get; init; } = "Variant";
    public bool IsConstant { get; init; }
    public bool IsStatic { get; init; }
}

internal class ProjectSignalInfo
{
    public string Name { get; init; } = "";
    public List<ProjectParameterInfo> Parameters { get; init; } = new();
}

internal class ProjectParameterInfo
{
    public string Name { get; init; } = "";
    public string TypeName { get; init; } = "Variant";
    public bool HasDefaultValue { get; init; }
}

internal class ProjectEnumInfo
{
    public string Name { get; init; } = "";
    public Dictionary<string, int> Values { get; init; } = new();
}
