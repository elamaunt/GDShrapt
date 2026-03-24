using GDShrapt.Abstractions;
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

    /// <summary>
    /// Invalidates the cached type info so that next access rebuilds from current semantic models.
    /// Should be called after AnalyzeAll() completes to pick up flow-inferred types.
    /// </summary>
    public void InvalidateCache()
    {
        _typeCache.Clear();
    }

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

    private IGDScriptInfo? ResolveScript(string path)
    {
        return _scriptProvider?.GetScriptByPath(path)
            ?? _scriptProvider?.Scripts.FirstOrDefault(s =>
                s.ResPath != null && s.ResPath.Equals(path, System.StringComparison.OrdinalIgnoreCase))
            ?? _scriptProvider?.Scripts.FirstOrDefault(s =>
                s.FullPath != null && s.FullPath.EndsWith(
                    path.Replace("res://", "").Replace('\\', '/'),
                    System.StringComparison.OrdinalIgnoreCase));
    }

    private IGDScriptInfo? ResolveAutoloadScript(GDAutoloadEntry autoload)
    {
        if (autoload.IsScript)
            return ResolveScript(autoload.Path);

        if (autoload.IsScene && _sceneTypesProvider != null)
        {
            var rootScriptPath = _sceneTypesProvider.GetNodeScript(autoload.Path, ".");
            if (!string.IsNullOrEmpty(rootScriptPath))
                return ResolveScript(rootScriptPath);
        }

        return null;
    }

    private GDRuntimeTypeInfo? BuildAutoloadTypeInfo(GDAutoloadEntry autoload)
    {
        var scriptInfo = ResolveAutoloadScript(autoload);
        if (scriptInfo != null)
        {
            var baseType = scriptInfo.Class?.Extends?.Type?.BuildName() ?? "Node";
            var members = ExtractMembers(scriptInfo);
            var classNameId = scriptInfo.Class?.ClassName?.Identifier;
            var className = classNameId?.Sequence;

            return new GDRuntimeTypeInfo(autoload.Name, baseType)
            {
                ClassName = className,
                Members = members,
                SourceInfo = scriptInfo.FullPath != null ? new GDTypeSourceInfo
                {
                    FilePath = scriptInfo.FullPath,
                    Line = classNameId?.StartLine ?? 0,
                    StartColumn = classNameId?.StartColumn ?? 0,
                    EndColumn = classNameId?.EndColumn ?? 0,
                    TypeName = className ?? autoload.Name
                } : null
            };
        }

        // If it's a scene without a script, try getting the root node type
        if (autoload.IsScene && _sceneTypesProvider != null)
        {
            var rootType = _sceneTypesProvider.GetRootNodeType(autoload.Path);
            if (!string.IsNullOrEmpty(rootType))
            {
                return new GDRuntimeTypeInfo(autoload.Name, rootType);
            }

            return new GDRuntimeTypeInfo(autoload.Name, "Node");
        }

        // Unknown autoload type, assume Node
        return new GDRuntimeTypeInfo(autoload.Name, "Node");
    }

    private List<GDRuntimeMemberInfo> ExtractMembers(IGDScriptInfo scriptInfo)
    {
        if (scriptInfo is GDScriptFile file && file.SemanticModel != null)
            return ExtractMembersFromSemanticModel(file.SemanticModel);

        return ExtractMembersFromAst(scriptInfo);
    }

    private List<GDRuntimeMemberInfo> ExtractMembersFromSemanticModel(GDSemanticModel model)
    {
        var members = new List<GDRuntimeMemberInfo>();

        foreach (var method in model.GetMethods())
        {
            var inferredParams = model.InferParameterTypes(method.Name);
            var returnTypeName = method.ReturnTypeName;

            // Use inferred return type if no explicit annotation
            if (string.IsNullOrEmpty(returnTypeName) || returnTypeName == GDWellKnownTypes.Variant)
            {
                var analysis = model.AnalyzeMethodReturns(method.Name);
                if (analysis != null && !analysis.ReturnUnionType.IsEmpty)
                {
                    var inferredReturn = analysis.ReturnUnionType.UnionTypeName;
                    if (!string.IsNullOrEmpty(inferredReturn) && inferredReturn != "null")
                        returnTypeName = inferredReturn;
                }
            }

            var minArgs = method.Parameters?.Count(p => !p.HasDefaultValue) ?? 0;
            var memberInfo = GDRuntimeMemberInfo.Method(
                method.Name,
                returnTypeName ?? GDWellKnownTypes.Variant,
                minArgs, method.ParameterCount,
                isVarArgs: false, isStatic: method.IsStatic);

            // Get AST parameter declarations for default value type inference
            var paramDecls = (method.DeclarationNode as GDMethodDeclaration)?.Parameters?.ToArray();

            if (method.Parameters != null && method.Parameters.Count > 0)
            {
                memberInfo.Parameters = method.Parameters
                    .Select((p, i) =>
                    {
                        var typeName = p.TypeName;

                        // Try inferred types from usage analysis
                        if (string.IsNullOrEmpty(typeName) && inferredParams.TryGetValue(p.Name, out var inferred))
                            typeName = inferred.TypeName?.DisplayName;

                        // Infer type from default value expression
                        if (string.IsNullOrEmpty(typeName) && paramDecls != null && i < paramDecls.Length)
                        {
                            var defaultValue = paramDecls[i].DefaultValue;
                            if (defaultValue != null)
                            {
                                // Try semantic model first
                                var defaultType = model.GetTypeForNode(defaultValue);
                                if (string.IsNullOrEmpty(defaultType) || defaultType == GDWellKnownTypes.Variant)
                                {
                                    // Fallback to AST-based literal type
                                    defaultType = InferLiteralType(defaultValue);
                                }
                                if (!string.IsNullOrEmpty(defaultType) && defaultType != GDWellKnownTypes.Variant)
                                    typeName = defaultType;
                            }
                        }

                        return new GDRuntimeParameterInfo(p.Name, typeName, p.HasDefaultValue);
                    })
                    .ToList();
            }

            memberInfo.IsCoroutine = method.IsCoroutine;
            members.Add(memberInfo);
        }

        foreach (var signal in model.GetSignals())
        {
            var signalInfo = GDRuntimeMemberInfo.Signal(signal.Name);
            if (signal.Parameters != null && signal.Parameters.Count > 0)
            {
                signalInfo.Parameters = signal.Parameters
                    .Select(p => new GDRuntimeParameterInfo(p.Name, p.TypeName, p.HasDefaultValue))
                    .ToList();
            }
            members.Add(signalInfo);
        }

        foreach (var variable in model.GetVariables())
            members.Add(GDRuntimeMemberInfo.Property(variable.Name, variable.TypeName ?? GDWellKnownTypes.Variant, variable.IsStatic));

        foreach (var constant in model.GetConstants())
            members.Add(GDRuntimeMemberInfo.Constant(constant.Name, constant.TypeName ?? GDWellKnownTypes.Variant));

        return members;
    }

    private List<GDRuntimeMemberInfo> ExtractMembersFromAst(IGDScriptInfo scriptInfo)
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
                    var methodInfo = GDRuntimeMemberInfo.Method(
                        method.Identifier.Sequence,
                        method.ReturnType?.BuildName() ?? GDWellKnownTypes.Variant,
                        minArgs,
                        maxArgs,
                        isVarArgs: false,
                        isStatic: method.IsStatic);
                    if (allParams.Count > 0)
                    {
                        methodInfo.Parameters = allParams
                            .Select(p => new GDRuntimeParameterInfo(
                                p.Identifier?.Sequence ?? "param",
                                p.Type?.BuildName(),
                                p.DefaultValue != null))
                            .ToList();
                    }
                    members.Add(methodInfo);
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
                    var signalInfo = GDRuntimeMemberInfo.Signal(signal.Identifier.Sequence);
                    var signalParams = signal.Parameters?.ToList();
                    if (signalParams != null && signalParams.Count > 0)
                    {
                        signalInfo.Parameters = signalParams
                            .Select(p => new GDRuntimeParameterInfo(
                                p.Identifier?.Sequence ?? "param",
                                p.Type?.BuildName(),
                                p.DefaultValue != null))
                            .ToList();
                    }
                    members.Add(signalInfo);
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
    public bool IsVirtualMethod(string typeName, string methodName) => false;

    private static string? InferLiteralType(GDExpression expr)
    {
        return expr switch
        {
            GDNumberExpression num => num.Number?.Sequence?.Contains('.') == true ? "float" : "int",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            GDNodePathExpression => "NodePath",
            _ => null
        };
    }
}
