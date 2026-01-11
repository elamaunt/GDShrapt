using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Runtime provider that integrates with GDProjectMap for multi-file type resolution.
/// Combines GDShrapt.TypesMap for Godot built-in types with project script information.
/// </summary>
internal class GDProjectRuntimeProvider : IGDProjectRuntimeProvider
{
    private readonly GDProjectMap _projectMap;
    private readonly IGDRuntimeProvider _builtInProvider;

    public GDProjectRuntimeProvider(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
        _builtInProvider = GDDefaultRuntimeProvider.Instance;
    }

    #region IGDRuntimeProvider

    public bool IsKnownType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // Check built-in types first
        if (_builtInProvider.IsKnownType(typeName))
            return true;

        // Check project classes
        if (_projectMap?.GetScriptMapByTypeName(typeName) != null)
            return true;

        return false;
    }

    public GDRuntimeTypeInfo GetTypeInfo(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Try built-in types
        var builtIn = _builtInProvider.GetTypeInfo(typeName);
        if (builtIn != null)
            return builtIn;

        // Try project classes
        var scriptMap = _projectMap?.GetScriptMapByTypeName(typeName);
        if (scriptMap != null)
        {
            return new GDRuntimeTypeInfo(typeName, GetBaseTypeFromScript(scriptMap));
        }

        return null;
    }

    public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        // Try built-in types first
        var builtIn = _builtInProvider.GetMember(typeName, memberName);
        if (builtIn != null)
            return builtIn;

        // Try project classes
        var scriptMap = _projectMap?.GetScriptMapByTypeName(typeName);
        if (scriptMap?.Analyzer != null)
        {
            var symbol = scriptMap.Analyzer.FindSymbol(memberName);
            if (symbol != null)
            {
                return new GDRuntimeMemberInfo(memberName, ConvertSymbolKind(symbol.Kind), symbol.TypeName)
                {
                    IsStatic = symbol.IsStatic
                };
            }
        }

        return null;
    }

    public string GetBaseType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Try built-in types
        var builtIn = _builtInProvider.GetBaseType(typeName);
        if (builtIn != null)
            return builtIn;

        // Try project classes
        var scriptMap = _projectMap?.GetScriptMapByTypeName(typeName);
        if (scriptMap != null)
        {
            return GetBaseTypeFromScript(scriptMap);
        }

        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return true;

        if (sourceType == targetType)
            return true;

        // Check inheritance chain
        var currentType = sourceType;
        while (currentType != null)
        {
            if (currentType == targetType)
                return true;
            currentType = GetBaseType(currentType);
        }

        // Fall back to built-in
        return _builtInProvider.IsAssignableTo(sourceType, targetType);
    }

    public GDRuntimeFunctionInfo GetGlobalFunction(string functionName)
    {
        return _builtInProvider.GetGlobalFunction(functionName);
    }

    public GDRuntimeTypeInfo GetGlobalClass(string className)
    {
        if (string.IsNullOrEmpty(className))
            return null;

        // Try built-in first
        var builtIn = _builtInProvider.GetGlobalClass(className);
        if (builtIn != null)
            return builtIn;

        // Try project autoloads/singletons
        // TODO: implement autoload detection from project.godot

        return null;
    }

    public bool IsBuiltIn(string identifier)
    {
        return _builtInProvider.IsBuiltIn(identifier);
    }

    #endregion

    #region IGDProjectRuntimeProvider

    public GDScriptTypeInfo GetScriptType(string scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath))
            return null;

        var scriptMap = _projectMap?.GetScriptMapByResourcePath(scriptPath);
        if (scriptMap == null)
            return null;

        return BuildScriptTypeInfo(scriptMap);
    }

    public GDScriptTypeInfo GetProjectClass(string className)
    {
        if (string.IsNullOrEmpty(className))
            return null;

        var scriptMap = _projectMap?.GetScriptMapByTypeName(className);
        if (scriptMap == null)
            return null;

        return BuildScriptTypeInfo(scriptMap);
    }

    public IEnumerable<GDScriptTypeInfo> GetProjectClasses()
    {
        if (_projectMap == null)
            yield break;

        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (scriptMap.IsGlobal)
            {
                yield return BuildScriptTypeInfo(scriptMap);
            }
        }
    }

    public IEnumerable<GDAutoloadInfo> GetAutoloads()
    {
        // TODO: Parse project.godot for autoload section
        yield break;
    }

    public string GetPreloadType(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return null;

        // Check if it's a script
        if (resourcePath.EndsWith(".gd"))
        {
            var scriptMap = _projectMap?.GetScriptMapByResourcePath(resourcePath);
            if (scriptMap != null)
            {
                return scriptMap.TypeName;
            }
        }

        // Check common resource types by extension
        if (resourcePath.EndsWith(".tscn") || resourcePath.EndsWith(".scn"))
            return "PackedScene";
        if (resourcePath.EndsWith(".tres") || resourcePath.EndsWith(".res"))
            return "Resource";
        if (resourcePath.EndsWith(".png") || resourcePath.EndsWith(".jpg") ||
            resourcePath.EndsWith(".webp") || resourcePath.EndsWith(".svg"))
            return "Texture2D";
        if (resourcePath.EndsWith(".wav") || resourcePath.EndsWith(".ogg") ||
            resourcePath.EndsWith(".mp3"))
            return "AudioStream";

        return null;
    }

    public bool ResourceExists(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return false;

        // Convert res:// path to actual file path if needed
        var filePath = resourcePath;
        if (resourcePath.StartsWith("res://") && _projectMap?.ProjectPath != null)
        {
            filePath = System.IO.Path.Combine(_projectMap.ProjectPath, resourcePath.Substring(6).Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));
        }

        return System.IO.File.Exists(filePath);
    }

    public GDSignalInfo GetSignal(string typeName, string signalName)
    {
        if (string.IsNullOrEmpty(signalName))
            return null;

        // Check project scripts first
        if (!string.IsNullOrEmpty(typeName) && typeName != "self")
        {
            var scriptMap = _projectMap?.GetScriptMapByTypeName(typeName);
            if (scriptMap?.Analyzer != null)
            {
                var symbol = scriptMap.Analyzer.FindSymbol(signalName);
                if (symbol?.Kind == GDSymbolKind.Signal)
                {
                    return BuildSignalInfo(symbol);
                }
            }
        }

        // Check built-in types
        var typeInfo = _builtInProvider.GetTypeInfo(typeName);
        if (typeInfo?.Members != null)
        {
            var signalMember = typeInfo.Members.FirstOrDefault(m =>
                m.Kind == GDRuntimeMemberKind.Signal && m.Name == signalName);
            if (signalMember != null)
            {
                return new GDSignalInfo
                {
                    Name = signalName,
                    Parameters = signalMember.Parameters
                };
            }
        }

        return null;
    }

    public IEnumerable<GDSignalInfo> GetSignals(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            yield break;

        // Check project scripts
        var scriptMap = _projectMap?.GetScriptMapByTypeName(typeName);
        if (scriptMap?.Analyzer != null)
        {
            foreach (var symbol in scriptMap.Analyzer.Symbols)
            {
                if (symbol.Kind == GDSymbolKind.Signal)
                {
                    yield return BuildSignalInfo(symbol);
                }
            }
        }

        // Check built-in types
        var typeInfo = _builtInProvider.GetTypeInfo(typeName);
        if (typeInfo?.Members != null)
        {
            foreach (var member in typeInfo.Members)
            {
                if (member.Kind == GDRuntimeMemberKind.Signal)
                {
                    yield return new GDSignalInfo
                    {
                        Name = member.Name,
                        Parameters = member.Parameters
                    };
                }
            }
        }
    }

    public IEnumerable<GDSceneSignalConnection> GetSignalConnectionsForMethod(string scriptPath, string methodName)
    {
        // TODO: Implement scene-based signal connection lookup
        // This would require parsing .tscn files for [connection] blocks
        yield break;
    }

    #endregion

    #region Helpers

    private string GetBaseTypeFromScript(GDScriptMap scriptMap)
    {
        var extends = scriptMap.Class?.Extends;
        if (extends?.Type != null)
        {
            return extends.Type.BuildName();
        }
        return "RefCounted"; // Default base type
    }

    private GDScriptTypeInfo BuildScriptTypeInfo(GDScriptMap scriptMap)
    {
        var result = new GDScriptTypeInfo
        {
            ScriptPath = scriptMap.Reference?.FullPath,
            ClassName = scriptMap.IsGlobal ? scriptMap.TypeName : null,
            BaseType = GetBaseTypeFromScript(scriptMap)
        };

        // Build members from analyzer if available
        if (scriptMap.Analyzer != null)
        {
            var members = new List<GDRuntimeMemberInfo>();
            var methods = new List<GDMethodInfo>();
            var signals = new List<GDSignalInfo>();

            foreach (var symbol in scriptMap.Analyzer.Symbols)
            {
                switch (symbol.Kind)
                {
                    case GDSymbolKind.Method:
                        methods.Add(BuildMethodInfo(symbol));
                        break;
                    case GDSymbolKind.Signal:
                        signals.Add(BuildSignalInfo(symbol));
                        break;
                    case GDSymbolKind.Variable:
                    case GDSymbolKind.Constant:
                        members.Add(new GDRuntimeMemberInfo(symbol.Name, ConvertSymbolKind(symbol.Kind), symbol.TypeName));
                        break;
                }
            }

            result.Members = members;
            result.Methods = methods;
            result.Signals = signals;
        }

        return result;
    }

    private GDMethodInfo BuildMethodInfo(GDSymbol symbol)
    {
        var methodDecl = symbol.Declaration as GDMethodDeclaration;
        var parameters = new List<GDRuntimeParameterInfo>();

        if (methodDecl?.Parameters != null)
        {
            foreach (var param in methodDecl.Parameters)
            {
                parameters.Add(new GDRuntimeParameterInfo(
                    param.Identifier?.Sequence,
                    param.Type?.BuildName(),
                    param.DefaultValue != null));
            }
        }

        return new GDMethodInfo
        {
            Name = symbol.Name,
            ReturnType = methodDecl?.ReturnType?.BuildName() ?? "void",
            Parameters = parameters,
            IsStatic = symbol.IsStatic
        };
    }

    private GDSignalInfo BuildSignalInfo(GDSymbol symbol)
    {
        var signalDecl = symbol.Declaration as GDSignalDeclaration;
        var parameters = new List<GDRuntimeParameterInfo>();

        if (signalDecl?.Parameters != null)
        {
            foreach (var param in signalDecl.Parameters)
            {
                parameters.Add(new GDRuntimeParameterInfo(
                    param.Identifier?.Sequence,
                    param.Type?.BuildName()));
            }
        }

        return new GDSignalInfo
        {
            Name = symbol.Name,
            Parameters = parameters
        };
    }

    private GDRuntimeMemberKind ConvertSymbolKind(GDSymbolKind kind)
    {
        switch (kind)
        {
            case GDSymbolKind.Method:
                return GDRuntimeMemberKind.Method;
            case GDSymbolKind.Signal:
                return GDRuntimeMemberKind.Signal;
            case GDSymbolKind.Variable:
            case GDSymbolKind.Constant:
            case GDSymbolKind.Parameter:
                return GDRuntimeMemberKind.Property;
            default:
                return GDRuntimeMemberKind.Property;
        }
    }

    #endregion
}
