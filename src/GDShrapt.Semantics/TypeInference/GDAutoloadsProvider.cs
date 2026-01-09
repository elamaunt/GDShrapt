using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Runtime provider that resolves autoloaded singletons as global classes.
/// Autoloads are defined in project.godot [autoload] section.
/// </summary>
public class GDAutoloadsProvider : IGDRuntimeProvider
{
    private readonly Dictionary<string, GDAutoloadEntry> _autoloads;
    private readonly IGDScriptProvider? _scriptProvider;
    private readonly Dictionary<string, GDRuntimeTypeInfo> _typeCache = new();

    /// <summary>
    /// Creates an autoloads provider.
    /// </summary>
    /// <param name="autoloads">Autoload entries from project.godot.</param>
    /// <param name="scriptProvider">Script provider for resolving script types (optional).</param>
    public GDAutoloadsProvider(IEnumerable<GDAutoloadEntry> autoloads, IGDScriptProvider? scriptProvider = null)
    {
        _autoloads = autoloads
            .Where(a => a.Enabled)
            .ToDictionary(a => a.Name, a => a);
        _scriptProvider = scriptProvider;
    }

    /// <summary>
    /// Gets all autoload entries.
    /// </summary>
    public IEnumerable<GDAutoloadEntry> Autoloads => _autoloads.Values;

    public bool IsKnownType(string typeName)
    {
        // Autoloads are not types themselves, they are instances
        return false;
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        // Autoloads are not types
        return null;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        // Autoloads are not types with members
        return null;
    }

    public string? GetBaseType(string typeName)
    {
        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        return null;
    }

    /// <summary>
    /// Returns type info for an autoload singleton by name.
    /// This is called when the type system encounters an identifier like "Global".
    /// </summary>
    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        if (string.IsNullOrEmpty(className))
            return null;

        // Check cache first
        if (_typeCache.TryGetValue(className, out var cached))
            return cached;

        // Check if it's an autoload
        if (!_autoloads.TryGetValue(className, out var autoload))
            return null;

        // Build type info for the autoload
        var typeInfo = BuildAutoloadTypeInfo(autoload);
        if (typeInfo != null)
        {
            _typeCache[className] = typeInfo;
        }

        return typeInfo;
    }

    public bool IsBuiltIn(string identifier)
    {
        // Autoloads are user-defined, not built-in
        return false;
    }

    private GDRuntimeTypeInfo? BuildAutoloadTypeInfo(GDAutoloadEntry autoload)
    {
        // If it's a script, try to get type from script provider
        if (autoload.IsScript && _scriptProvider != null)
        {
            var scriptInfo = _scriptProvider.Scripts
                .FirstOrDefault(s => s.FullPath?.EndsWith(autoload.Path.Replace("res://", "").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString())) == true);

            if (scriptInfo != null)
            {
                var baseType = scriptInfo.Class?.Extends?.Type?.BuildName() ?? "Node";
                var members = ExtractMembers(scriptInfo);

                return new GDRuntimeTypeInfo(autoload.Name, baseType)
                {
                    Members = members
                };
            }
        }

        // If it's a scene, return Node type
        if (autoload.IsScene)
        {
            return new GDRuntimeTypeInfo(autoload.Name, "Node");
        }

        // Unknown autoload type, assume Node
        return new GDRuntimeTypeInfo(autoload.Name, "Node");
    }

    private List<GDRuntimeMemberInfo> ExtractMembers(IGDScriptInfo scriptInfo)
    {
        var members = new List<GDRuntimeMemberInfo>();

        if (scriptInfo.Class == null)
            return members;

        foreach (var member in scriptInfo.Class.Members)
        {
            switch (member)
            {
                case GDMethodDeclaration method when method.Identifier != null:
                    var paramCount = method.Parameters?.Count() ?? 0;
                    members.Add(GDRuntimeMemberInfo.Method(
                        method.Identifier.Sequence,
                        method.ReturnType?.BuildName() ?? "Variant",
                        paramCount,
                        paramCount,
                        isVarArgs: false,
                        isStatic: method.IsStatic));
                    break;

                case GDVariableDeclaration variable when variable.Identifier != null:
                    if (variable.ConstKeyword != null)
                    {
                        members.Add(GDRuntimeMemberInfo.Constant(
                            variable.Identifier.Sequence,
                            variable.Type?.BuildName() ?? "Variant"));
                    }
                    else
                    {
                        members.Add(GDRuntimeMemberInfo.Property(
                            variable.Identifier.Sequence,
                            variable.Type?.BuildName() ?? "Variant",
                            variable.StaticKeyword != null));
                    }
                    break;

                case GDSignalDeclaration signal when signal.Identifier != null:
                    members.Add(GDRuntimeMemberInfo.Signal(signal.Identifier.Sequence));
                    break;
            }
        }

        return members;
    }
}
