using GDShrapt.Reader;
using System.Collections.Concurrent;
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
    private readonly GDSceneTypesProvider? _sceneTypesProvider;
    private readonly ConcurrentDictionary<string, GDRuntimeTypeInfo> _typeCache = new();

    /// <summary>
    /// Creates an autoloads provider.
    /// </summary>
    /// <param name="autoloads">Autoload entries from project.godot.</param>
    /// <param name="scriptProvider">Script provider for resolving script types (optional).</param>
    /// <param name="sceneTypesProvider">Scene types provider for resolving scene autoloads to their root scripts (optional).</param>
    public GDAutoloadsProvider(IEnumerable<GDAutoloadEntry> autoloads, IGDScriptProvider? scriptProvider = null, GDSceneTypesProvider? sceneTypesProvider = null)
    {
        _autoloads = autoloads
            .Where(a => a.Enabled)
            .ToDictionary(a => a.Name, a => a);
        _scriptProvider = scriptProvider;
        _sceneTypesProvider = sceneTypesProvider;
    }

    /// <summary>
    /// Gets all autoload entries.
    /// </summary>
    public IEnumerable<GDAutoloadEntry> Autoloads => _autoloads.Values;

    public bool IsKnownType(string typeName)
    {
        return _autoloads.ContainsKey(typeName);
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        return GetGlobalClass(typeName);
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        var typeInfo = GetGlobalClass(typeName);
        if (typeInfo == null)
            return null;

        return typeInfo.Members?.FirstOrDefault(m => m.Name == memberName);
    }

    public string? GetBaseType(string typeName)
    {
        var typeInfo = GetGlobalClass(typeName);
        return typeInfo?.BaseType;
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
            var scriptInfo = _scriptProvider.GetScriptByPath(autoload.Path)
                ?? _scriptProvider.Scripts.FirstOrDefault(s =>
                    s.ResPath != null && s.ResPath.Equals(autoload.Path, System.StringComparison.OrdinalIgnoreCase))
                ?? _scriptProvider.Scripts.FirstOrDefault(s =>
                    s.FullPath != null && s.FullPath.EndsWith(
                        autoload.Path.Replace("res://", "").Replace('\\', '/'),
                        System.StringComparison.OrdinalIgnoreCase));

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

        // If it's a scene, try to resolve the root node's attached script
        if (autoload.IsScene)
        {
            if (_sceneTypesProvider != null && _scriptProvider != null)
            {
                var rootScriptPath = _sceneTypesProvider.GetNodeScript(autoload.Path, ".");
                if (!string.IsNullOrEmpty(rootScriptPath))
                {
                    var scriptInfo = _scriptProvider.GetScriptByPath(rootScriptPath)
                        ?? _scriptProvider.Scripts.FirstOrDefault(s =>
                            s.ResPath != null && s.ResPath.Equals(rootScriptPath, System.StringComparison.OrdinalIgnoreCase))
                        ?? _scriptProvider.Scripts.FirstOrDefault(s =>
                            s.FullPath != null && s.FullPath.EndsWith(
                                rootScriptPath.Replace("res://", "").Replace('\\', '/'),
                                System.StringComparison.OrdinalIgnoreCase));

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

                // Try getting the root node type from the scene
                var rootType = _sceneTypesProvider.GetRootNodeType(autoload.Path);
                if (!string.IsNullOrEmpty(rootType))
                {
                    return new GDRuntimeTypeInfo(autoload.Name, rootType);
                }
            }

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
                    var allParams = method.Parameters?.ToList() ?? new List<GDParameterDeclaration>();
                    var minArgs = allParams.Count(p => p.DefaultValue == null);
                    var maxArgs = allParams.Count;
                    members.Add(GDRuntimeMemberInfo.Method(
                        method.Identifier.Sequence,
                        method.ReturnType?.BuildName() ?? GDWellKnownTypes.Variant,
                        minArgs,
                        maxArgs,
                        isVarArgs: false,
                        isStatic: method.IsStatic));
                    break;

                case GDVariableDeclaration variable when variable.Identifier != null:
                    if (variable.ConstKeyword != null)
                    {
                        members.Add(GDRuntimeMemberInfo.Constant(
                            variable.Identifier.Sequence,
                            variable.Type?.BuildName() ?? GDWellKnownTypes.Variant));
                    }
                    else
                    {
                        members.Add(GDRuntimeMemberInfo.Property(
                            variable.Identifier.Sequence,
                            variable.Type?.BuildName() ?? GDWellKnownTypes.Variant,
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

    public IEnumerable<string> GetAllTypes()
    {
        // Autoloads are instances (singletons), not types.
        // They are accessed via GetGlobalClass() by name.
        return Enumerable.Empty<string>();
    }

    public bool IsBuiltinType(string typeName)
    {
        // Autoloads are user-defined instances, not builtin value types
        return false;
    }

    public IReadOnlyList<string> FindTypesWithMethod(string methodName)
    {
        var result = new List<string>();
        foreach (var autoload in _autoloads.Keys)
        {
            var typeInfo = GetGlobalClass(autoload);
            if (typeInfo?.Members?.Any(m => m.Name == methodName && m.Kind == GDRuntimeMemberKind.Method) == true)
                result.Add(autoload);
        }
        return result;
    }

    // Type Traits - stub implementations (delegated to composite/main provider)
    public bool IsNumericType(string typeName) => false;
    public bool IsIterableType(string typeName) => false;
    public bool IsIndexableType(string typeName) => false;
    public bool IsNullableType(string typeName) => true;
    public bool IsVectorType(string typeName) => false;
    public bool IsContainerType(string typeName) => false;
    public bool IsPackedArrayType(string typeName) => false;
    public string? GetFloatVectorVariant(string integerVectorType) => null;
    public string? GetPackedArrayElementType(string packedArrayType) => null;
    public string? ResolveOperatorResult(string leftType, string operatorName, string rightType) => null;
    public IReadOnlyList<string> GetTypesWithOperator(string operatorName) => Array.Empty<string>();
    public IReadOnlyList<string> GetTypesWithNonZeroCollisionLayer() => Array.Empty<string>();
    public IReadOnlyList<GDCollisionLayerInfo> GetCollisionLayerDetails() => Array.Empty<GDCollisionLayerInfo>();
    public IReadOnlyList<string> GetTypesWithNonZeroAvoidanceLayers() => Array.Empty<string>();
    public IReadOnlyList<GDAvoidanceLayerInfo> GetAvoidanceLayerDetails() => Array.Empty<GDAvoidanceLayerInfo>();
    public GDExpression? GetConstantInitializer(string typeName, string constantName) => null;
}
