using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using GDFlowSceneSnapshot = GDShrapt.Abstractions.GDSceneSnapshot;

namespace GDShrapt.Semantics;

// IGDRuntimeTypeInjector and GDTypeInjectionContext are defined in GDShrapt.Reader namespace

/// <summary>
/// Type injector for $NodePath, %UniqueNode, get_node(), preload() expressions and signal types.
/// Uses scene information to determine precise node types and TypesMap for signal parameters.
/// </summary>
public class GDNodeTypeInjector : IGDRuntimeTypeInjector
{
    private readonly GDSceneTypesProvider? _sceneProvider;
    private readonly IGDScriptProvider? _scriptProvider;
    private readonly GDGodotTypesProvider? _godotTypesProvider;
    private readonly IGDLogger? _logger;
    private readonly GDGroupRegistry? _groupRegistry;
    private readonly GDTresResourceProvider? _tresResourceProvider;

    // Scene snapshot cache: built once per scene path
    private readonly Dictionary<string, GDFlowSceneSnapshot?> _snapshotCache = new();

    // Side-channel: origin data from the last InjectType call
    private GDTypeOrigin? _lastInjectionOrigin;

    internal GDNodeTypeInjector(
        GDSceneTypesProvider? sceneProvider = null,
        IGDScriptProvider? scriptProvider = null,
        GDGodotTypesProvider? godotTypesProvider = null,
        IGDLogger? logger = null,
        GDGroupRegistry? groupRegistry = null,
        GDTresResourceProvider? tresResourceProvider = null)
    {
        _sceneProvider = sceneProvider;
        _scriptProvider = scriptProvider;
        _godotTypesProvider = godotTypesProvider;
        _logger = logger;
        _groupRegistry = groupRegistry;
        _tresResourceProvider = tresResourceProvider;
    }

    /// <summary>
    /// Returns the origin data from the last InjectType call, or null if no origin was produced.
    /// This is a side-channel: call it immediately after InjectType to get the associated origin.
    /// </summary>
    public GDTypeOrigin? GetLastInjectionOrigin() => _lastInjectionOrigin;

    /// <summary>
    /// Gets or builds a cached scene snapshot for a scene path.
    /// </summary>
    public GDFlowSceneSnapshot? GetSceneSnapshot(string scenePath)
    {
        if (_snapshotCache.TryGetValue(scenePath, out var cached))
            return cached;

        if (_sceneProvider == null)
            return null;

        if (_sceneProvider.GetSceneInfo(scenePath) == null)
            _sceneProvider.LoadScene(scenePath);

        var sceneInfo = _sceneProvider.GetSceneInfo(scenePath);
        var snapshot = GDSceneSnapshotBuilder.Build(sceneInfo);
        _snapshotCache[scenePath] = snapshot;
        return snapshot;
    }

    public string? InjectType(GDNode node, GDTypeInjectionContext context)
    {
        _lastInjectionOrigin = null;

        string? result = node switch
        {
            GDGetNodeExpression getNode => InferGetNodeType(getNode, context),
            GDGetUniqueNodeExpression uniqueNode => InferUniqueNodeType(uniqueNode, context),
            GDCallExpression call => InferCallType(call, context),
            _ => null
        };

        return result;
    }

    private string? InferGetNodeType(GDGetNodeExpression expr, GDTypeInjectionContext context)
    {
        if (_sceneProvider == null)
            return null;

        var nodePath = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);
        if (string.IsNullOrEmpty(nodePath))
            return null;

        var result = ResolveNodeType(nodePath, context.ScriptPath);
        if (result != null)
        {
            _lastInjectionOrigin = new GDTypeOrigin(
                GDTypeOriginKind.SceneInjection,
                GDTypeOriginConfidence.Exact,
                LocationFromNode(expr),
                description: $"$\"{nodePath}\"");
        }
        return result;
    }

    private string? InferUniqueNodeType(GDGetUniqueNodeExpression expr, GDTypeInjectionContext context)
    {
        if (_sceneProvider == null)
            return null;

        var nodeName = GDNodePathExtractor.ExtractFromUniqueNodeExpression(expr);
        if (string.IsNullOrEmpty(nodeName))
            return null;

        var result = ResolveUniqueNodeType(nodeName, context.ScriptPath);
        if (result != null)
        {
            _lastInjectionOrigin = new GDTypeOrigin(
                GDTypeOriginKind.SceneInjection,
                GDTypeOriginConfidence.Exact,
                LocationFromNode(expr),
                description: $"%{nodeName}");
        }
        return result;
    }

    private string? ResolveUniqueNodeType(string nodeName, string? scriptPath)
    {
        if (_sceneProvider == null || string.IsNullOrEmpty(scriptPath))
            return null;

        var scenes = _sceneProvider.GetScenesForScript(scriptPath).ToList();
        if (scenes.Count == 0)
        {
            _logger?.Debug($"No scenes found for script: {scriptPath}");
            return null;
        }

        var types = new HashSet<string>();
        foreach (var (scenePath, _) in scenes)
        {
            var nodeType = _sceneProvider.GetUniqueNodeType(scenePath, nodeName);
            if (!string.IsNullOrEmpty(nodeType))
                types.Add(nodeType);
        }

        if (types.Count == 1)
            return types.First();

        if (types.Count > 1)
            _logger?.Debug($"Ambiguous unique node type for '{nodeName}': {string.Join(", ", types)}");

        return null;
    }

    private string? InferCallType(GDCallExpression call, GDTypeInjectionContext context)
    {
        var callName = GDNodePathExtractor.GetCallName(call);

        // get_node() and variants
        if (callName == "get_node" || callName == "get_node_or_null" || callName == "find_node")
        {
            // Function to resolve variables to static values
            Func<string, GDExpression?>? resolveVariable = null;
            var classDecl = call.RootClassDeclaration;
            if (classDecl != null)
            {
                resolveVariable = varName =>
                    GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);
            }

            var nodePath = GDNodePathExtractor.ExtractFromCallExpression(call, resolveVariable);
            if (!string.IsNullOrEmpty(nodePath))
            {
                var result = ResolveNodeType(nodePath, context.ScriptPath);
                if (result != null)
                {
                    _lastInjectionOrigin = new GDTypeOrigin(
                        GDTypeOriginKind.SceneInjection,
                        GDTypeOriginConfidence.Exact,
                        LocationFromNode(call),
                        description: $"{callName}(\"{nodePath}\")");
                }
                return result;
            }
        }

        // preload() and load()
        if (GDWellKnownFunctions.IsResourceLoader(callName))
        {
            var resourcePath = GDNodePathExtractor.ExtractResourcePath(call);
            if (!string.IsNullOrEmpty(resourcePath))
            {
                var result = ResolvePreloadType(resourcePath);
                if (result != null)
                {
                    _lastInjectionOrigin = new GDTypeOrigin(
                        GDTypeOriginKind.PreloadInjection,
                        GDTypeOriginConfidence.Exact,
                        LocationFromNode(call),
                        value: new GDResourcePathValue(resourcePath, result));
                }
                return result;
            }
        }

        // preload("scene.tscn").instantiate() or scene_var.instantiate()
        if (callName == GDWellKnownFunctions.Instantiate)
        {
            var scenePath = ExtractScenePathFromCaller(call, context.ScriptPath);
            if (!string.IsNullOrEmpty(scenePath))
            {
                var result = ResolveSceneRootType(scenePath);
                if (result != null)
                {
                    var snapshot = GetSceneSnapshot(scenePath);
                    GDObjectState? objectState = null;
                    if (snapshot != null)
                    {
                        GDCollisionLayerState? rootCollision = null;
                        if (snapshot.Nodes.Count > 0)
                            rootCollision = snapshot.Nodes[0].CollisionLayers;

                        objectState = new GDObjectState(
                            sceneSnapshot: snapshot,
                            collisionLayers: rootCollision);
                    }

                    _lastInjectionOrigin = new GDTypeOrigin(
                        GDTypeOriginKind.InstantiateInjection,
                        GDTypeOriginConfidence.Exact,
                        LocationFromNode(call),
                        value: new GDResourcePathValue(scenePath, result),
                        objectState: objectState);
                }
                return result;
            }
        }

        // get_child(N) — try scene instance, then self scene, then add_child tracking
        if (GDWellKnownFunctions.IsGetChild(callName))
        {
            var result = InferGetChildOnSceneInstance(call);
            if (!string.IsNullOrEmpty(result))
                return result;

            result = InferGetChildOnSelf(call, context);
            if (!string.IsNullOrEmpty(result))
                return result;

            result = InferGetChildFromAddChildTracking(call, context);
            if (!string.IsNullOrEmpty(result))
                return result;
        }

        // get_nodes_in_group() / get_first_node_in_group() — narrow via group registry
        if (GDWellKnownFunctions.IsGroupQuery(callName))
        {
            var result = InferGroupQueryType(call, callName);
            if (!string.IsNullOrEmpty(result))
                return result;
        }

        return null;
    }

    private string? InferGroupQueryType(GDCallExpression call, string callName)
    {
        if (_groupRegistry == null)
            return null;

        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        Func<string, GDExpression?>? resolveVariable = null;
        var classDecl = call.RootClassDeclaration;
        if (classDecl != null)
            resolveVariable = GDStaticStringExtractor.CreateClassResolver(classDecl);

        Func<string, string, GDExpression?>? crossClassResolver = _scriptProvider != null
            ? GDStaticStringExtractor.CreateCrossClassResolver(_scriptProvider)
            : null;

        var groupName = GDStaticStringExtractor.TryExtractString(
            args[0] as GDExpression, resolveVariable, crossClassResolver);

        if (string.IsNullOrEmpty(groupName))
            return null;

        var groupType = _groupRegistry.GetGroupType(groupName);
        if (string.IsNullOrEmpty(groupType))
            return null;

        string resultType;
        if (callName == GDWellKnownFunctions.GetNodesInGroup)
            resultType = $"Array[{groupType}]";
        else
            resultType = groupType;

        _lastInjectionOrigin = new GDTypeOrigin(
            GDTypeOriginKind.GroupInjection,
            GDTypeOriginConfidence.Inferred,
            LocationFromNode(call),
            description: $"{callName}(\"{groupName}\") -> {resultType}");

        return resultType;
    }

    private string? ResolveNodeType(string nodePath, string? scriptPath)
    {
        if (_sceneProvider == null || string.IsNullOrEmpty(scriptPath))
            return null;

        // Find scenes using this script
        var scenes = _sceneProvider.GetScenesForScript(scriptPath).ToList();
        if (scenes.Count == 0)
        {
            _logger?.Debug($"No scenes found for script: {scriptPath}");
            return null;
        }

        // Collect node types from all scenes
        var types = new HashSet<string>();
        foreach (var (scenePath, scriptNodePath) in scenes)
        {
            // Resolve path relative to the node with script
            var fullPath = ResolveRelativePath(scriptNodePath, nodePath);
            var nodeType = _sceneProvider.GetNodeType(scenePath, fullPath);
            if (!string.IsNullOrEmpty(nodeType))
                types.Add(nodeType);
        }

        // If type is the same in all scenes - return it
        if (types.Count == 1)
            return types.First();

        // If types differ - leave "Node" (default)
        if (types.Count > 1)
            _logger?.Debug($"Ambiguous node type for '{nodePath}': {string.Join(", ", types)}");

        return null;
    }

    private string? ResolvePreloadType(string resourcePath)
    {
        if (resourcePath.EndsWith(".gd"))
        {
            var scriptInfo = _scriptProvider?.GetScriptByPath(resourcePath);
            if (scriptInfo?.TypeName != null)
                return scriptInfo.TypeName;
            return "GDScript";
        }

        if (resourcePath.EndsWith(".tscn") || resourcePath.EndsWith(".scn"))
            return "PackedScene";

        if (resourcePath.EndsWith(".tres") || resourcePath.EndsWith(".res"))
        {
            var tresType = _sceneProvider?.GetResourceType(resourcePath);
            if (!string.IsNullOrEmpty(tresType))
                return tresType;

            var tresInfo = _tresResourceProvider?.GetResourceInfo(resourcePath);
            if (!string.IsNullOrEmpty(tresInfo?.EffectiveClassName))
                return tresInfo.EffectiveClassName;

            return "Resource";
        }

        return GDResourceCategoryResolver.TypeNameFromExtension(resourcePath);
    }

    private string ResolveRelativePath(string basePath, string relativePath)
    {
        // Handle absolute paths from scene root
        if (relativePath.StartsWith("/"))
            return relativePath.TrimStart('/');

        // Handle "." as current node
        if (basePath == ".")
            return relativePath;

        var parts = basePath.Split('/').ToList();
        var relParts = relativePath.Split('/');

        foreach (var part in relParts)
        {
            if (part == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
            }
            else if (part != "." && !string.IsNullOrEmpty(part))
            {
                parts.Add(part);
            }
        }

        return string.Join("/", parts);
    }

    // IGDRuntimeTypeInjector - other methods
    public string? NarrowVariantType(GDExpression expression, GDTypeInjectionContext context) => null;

    public IReadOnlyList<string>? GetSignalParameterTypes(string signalName, string? emitterType)
    {
        // 1. For Godot types - use TypesMap
        if (_godotTypesProvider != null && !string.IsNullOrEmpty(emitterType))
        {
            var signals = _godotTypesProvider.GetSignals(emitterType);
            if (signals?.TryGetValue(signalName, out var signalData) == true)
            {
                if (signalData.Parameters == null || signalData.Parameters.Length == 0)
                    return Array.Empty<string>();

                return signalData.Parameters
                    .Select(p => p.GDScriptTypeName ?? "Variant")
                    .ToArray();
            }
        }

        // 2. For project types - search in scripts via IGDScriptProvider
        if (_scriptProvider != null && !string.IsNullOrEmpty(emitterType))
        {
            var scriptInfo = _scriptProvider.GetScriptByTypeName(emitterType);
            if (scriptInfo?.Class != null)
            {
                var signalDecl = scriptInfo.Class.Members?
                    .OfType<GDSignalDeclaration>()
                    .FirstOrDefault(s => s.Identifier?.Sequence == signalName);

                if (signalDecl != null)
                {
                    return ExtractSignalParameterTypes(signalDecl);
                }
            }
        }

        return null;
    }

    private IReadOnlyList<string> ExtractSignalParameterTypes(GDSignalDeclaration signalDecl)
    {
        var parameters = signalDecl.Parameters?.ToList();
        if (parameters == null || parameters.Count == 0)
            return Array.Empty<string>();

        return parameters
            .Select(p => p.Type?.BuildName() ?? "Variant")
            .ToArray();
    }

    public string? GetMethodReturnType(string methodName, string receiverType, IReadOnlyList<string> argumentTypes) => null;

    private string? ExtractScenePathFromCaller(GDCallExpression instantiateCall, string? scriptPath = null)
    {
        var caller = instantiateCall.CallerExpression;
        if (caller is not GDMemberOperatorExpression memberExpr)
            return null;

        // Direct: preload("res://scene.tscn").instantiate()
        if (memberExpr.CallerExpression is GDCallExpression preloadCall)
        {
            var path = ExtractScenePathFromPreloadCall(preloadCall);
            if (path != null && !path.StartsWith("res://"))
                path = ResolvePreloadRelativePath(path, scriptPath);
            return path;
        }

        // Variable: scene_var.instantiate()
        if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
            return ExtractScenePathFromVariable(identExpr, instantiateCall, scriptPath);

        return null;
    }

    private string? ExtractScenePathFromPreloadCall(GDCallExpression preloadCall)
    {
        var preloadName = GDNodePathExtractor.GetCallName(preloadCall);
        if (!GDWellKnownFunctions.IsResourceLoader(preloadName))
            return null;

        var resourcePath = GDNodePathExtractor.ExtractResourcePath(preloadCall);
        if (string.IsNullOrEmpty(resourcePath))
            return null;

        if (resourcePath.EndsWith(".tscn") || resourcePath.EndsWith(".scn"))
            return resourcePath;

        return null;
    }

    private string? ExtractScenePathFromVariable(GDIdentifierExpression identExpr, GDCallExpression contextCall, string? scriptPath)
    {
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return null;

        var classDecl = contextCall.RootClassDeclaration;
        if (classDecl == null)
            return null;

        // Check code-level initializer (preload)
        var initExpr = FindVariableInitializerInHierarchy(classDecl, varName);
        if (initExpr is GDCallExpression preloadCall)
        {
            var path = ExtractScenePathFromPreloadCall(preloadCall);
            if (path != null && !path.StartsWith("res://"))
                path = ResolvePreloadRelativePath(path, scriptPath);
            return path;
        }

        // Check .tscn resource references for @export PackedScene variables
        if (_sceneProvider != null && !string.IsNullOrEmpty(scriptPath))
        {
            var scenePath = FindExportedScenePathFromTscn(scriptPath, varName);
            if (!string.IsNullOrEmpty(scenePath))
                return scenePath;
        }

        return null;
    }

    private static string? ResolvePreloadRelativePath(string relativePath, string? scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath) || !scriptPath.StartsWith("res://"))
            return null;

        var lastSlash = scriptPath.LastIndexOf('/');
        if (lastSlash < 0)
            return null;

        var scriptDir = scriptPath.Substring(0, lastSlash + 1);
        return scriptDir + relativePath;
    }

    private string? FindExportedScenePathFromTscn(string scriptPath, string varName)
    {
        var scenes = _sceneProvider!.GetScenesForScript(scriptPath);

        foreach (var (scenePath, nodePath) in scenes)
        {
            var resourceRefs = _sceneProvider.GetSceneResourceReferences(scenePath);

            foreach (var resourceRef in resourceRefs)
            {
                if (resourceRef.PropertyName == varName &&
                    resourceRef.NodePath == nodePath &&
                    resourceRef.ResourceType == "PackedScene" &&
                    (resourceRef.ResourcePath.EndsWith(".tscn") || resourceRef.ResourcePath.EndsWith(".scn")))
                {
                    return resourceRef.ResourcePath;
                }
            }
        }

        return null;
    }

    private string? ResolveSceneRootType(string scenePath)
    {
        if (_sceneProvider == null)
            return null;

        if (_sceneProvider.GetSceneInfo(scenePath) == null)
            _sceneProvider.LoadScene(scenePath);

        var sceneInfo = _sceneProvider.GetSceneInfo(scenePath);
        if (sceneInfo == null || sceneInfo.Nodes.Count == 0)
            return null;

        var rootNode = sceneInfo.Nodes[0];

        if (!string.IsNullOrEmpty(rootNode.ScriptPath) && _scriptProvider != null)
        {
            // ScriptPath from scene is res:// format; try direct path lookup first,
            // then match against ResPath for res:// paths
            var scriptInfo = _scriptProvider.GetScriptByPath(rootNode.ScriptPath);
            if (scriptInfo == null && rootNode.ScriptPath.StartsWith("res://"))
                scriptInfo = _scriptProvider.Scripts.FirstOrDefault(s => s.ResPath == rootNode.ScriptPath);

            if (!string.IsNullOrEmpty(scriptInfo?.TypeName))
                return scriptInfo.TypeName;
        }

        return rootNode.ScriptTypeName ?? rootNode.NodeType;
    }

    private string? InferGetChildOnSceneInstance(GDCallExpression getChildCall)
    {
        if (_sceneProvider == null)
            return null;

        var caller = getChildCall.CallerExpression;
        if (caller is not GDMemberOperatorExpression memberExpr)
            return null;

        if (memberExpr.CallerExpression is not GDIdentifierExpression identExpr)
            return null;

        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return null;

        var classDecl = getChildCall.RootClassDeclaration;
        if (classDecl == null)
            return null;

        var initExpr = FindVariableInitializerInHierarchy(classDecl, varName);
        if (initExpr is not GDCallExpression instantiateCall)
            return null;

        var instantiateName = GDNodePathExtractor.GetCallName(instantiateCall);
        if (instantiateName != GDWellKnownFunctions.Instantiate)
            return null;

        var scenePath = ExtractScenePathFromCaller(instantiateCall);
        if (string.IsNullOrEmpty(scenePath))
            return null;

        if (_sceneProvider.GetSceneInfo(scenePath) == null)
            _sceneProvider.LoadScene(scenePath);

        var args = getChildCall.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        if (args[0] is GDNumberExpression numExpr)
        {
            if (int.TryParse(numExpr.Number?.Sequence, out var index))
            {
                var children = _sceneProvider.GetDirectChildren(scenePath, ".");
                if (index >= 0 && index < children.Count)
                    return children[index].ScriptTypeName ?? children[index].NodeType;
            }
        }

        return null;
    }

    private string? InferGetChildOnSelf(GDCallExpression call, GDTypeInjectionContext context)
    {
        if (_sceneProvider == null || string.IsNullOrEmpty(context.ScriptPath))
            return null;

        if (!IsCallOnSelf(call))
            return null;

        var scenes = _sceneProvider.GetScenesForScript(context.ScriptPath).ToList();
        if (scenes.Count == 0)
            return null;

        int? index = ExtractLiteralIndex(call);

        var allChildTypes = new HashSet<string>();
        foreach (var (scenePath, scriptNodePath) in scenes)
        {
            if (_sceneProvider.GetSceneInfo(scenePath) == null)
                _sceneProvider.LoadScene(scenePath);

            var children = _sceneProvider.GetDirectChildren(scenePath, scriptNodePath);
            if (children.Count == 0)
                continue;

            if (index.HasValue && index.Value >= 0 && index.Value < children.Count)
            {
                var childType = children[index.Value].ScriptTypeName ?? children[index.Value].NodeType;
                if (!string.IsNullOrEmpty(childType))
                    allChildTypes.Add(childType);
            }
            else
            {
                foreach (var child in children)
                {
                    var childType = child.ScriptTypeName ?? child.NodeType;
                    if (!string.IsNullOrEmpty(childType))
                        allChildTypes.Add(childType);
                }
            }
        }

        if (allChildTypes.Count == 1)
            return allChildTypes.First();

        if (allChildTypes.Count > 1)
            return FindCommonBaseType(allChildTypes);

        return null;
    }

    private string? InferGetChildFromAddChildTracking(GDCallExpression call, GDTypeInjectionContext context)
    {
        if (!IsCallOnSelf(call))
            return null;

        var classDecl = call.RootClassDeclaration;
        if (classDecl == null)
            return null;

        var collector = GetOrCollectAddChildInfo(classDecl);
        if (collector == null || collector.AddChildCalls.Count == 0)
            return null;

        var selfChildTypes = new HashSet<string>();
        foreach (var addChildInfo in collector.AddChildCalls)
        {
            if (addChildInfo.ParentPath != null && addChildInfo.ParentPath != "self")
                continue;

            var childType = ResolveAddChildArgumentType(addChildInfo, classDecl);
            if (!string.IsNullOrEmpty(childType))
                selfChildTypes.Add(childType);
        }

        if (selfChildTypes.Count == 1)
            return selfChildTypes.First();

        if (selfChildTypes.Count > 1)
            return FindCommonBaseType(selfChildTypes);

        return null;
    }

    private string? ResolveAddChildArgumentType(GDAddChildInfo addChildInfo, GDClassDeclaration classDecl)
    {
        // Case 1: add_child(SCENE.instantiate()) — inline instantiate call
        if (addChildInfo.ChildArgument is GDCallExpression callExpr)
        {
            var callName = GDNodePathExtractor.GetCallName(callExpr);
            if (callName == GDWellKnownFunctions.Instantiate)
            {
                var scenePath = ExtractScenePathFromCaller(callExpr);
                if (!string.IsNullOrEmpty(scenePath))
                    return ResolveSceneRootType(scenePath);
            }
        }

        // Case 2: add_child(var_name) — variable
        if (!string.IsNullOrEmpty(addChildInfo.ChildVariableName))
        {
            // Check class-level variable (const/var with initializer)
            var initExpr = FindVariableInitializerInHierarchy(classDecl, addChildInfo.ChildVariableName);
            if (initExpr is GDCallExpression initCall)
            {
                var initCallName = GDNodePathExtractor.GetCallName(initCall);
                if (initCallName == GDWellKnownFunctions.Instantiate)
                {
                    var scenePath = ExtractScenePathFromCaller(initCall);
                    if (!string.IsNullOrEmpty(scenePath))
                        return ResolveSceneRootType(scenePath);
                }
            }

            // Check local variable in same scope (for-loop pattern)
            var localInit = FindLocalVariableInitializer(addChildInfo.ChildArgument as GDIdentifierExpression);
            if (localInit is GDCallExpression localCall)
            {
                var localCallName = GDNodePathExtractor.GetCallName(localCall);
                if (localCallName == GDWellKnownFunctions.Instantiate)
                {
                    var scenePath = ExtractScenePathFromCaller(localCall);
                    if (!string.IsNullOrEmpty(scenePath))
                        return ResolveSceneRootType(scenePath);
                }
            }
        }

        return null;
    }

    private static bool IsCallOnSelf(GDCallExpression call)
    {
        // Bare call: get_child(N) — CallerExpression is GDIdentifierExpression
        if (call.CallerExpression is GDIdentifierExpression)
            return true;

        // self.get_child(N)
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr &&
            memberExpr.CallerExpression is GDIdentifierExpression selfIdent &&
            selfIdent.Identifier?.Sequence == "self")
            return true;

        return false;
    }

    private static int? ExtractLiteralIndex(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;
        if (args[0] is GDNumberExpression numExpr &&
            int.TryParse(numExpr.Number?.Sequence, out var idx))
            return idx;
        return null;
    }

    private string? FindCommonBaseType(HashSet<string> types)
    {
        if (_godotTypesProvider == null || types.Count == 0)
            return null;

        var first = types.First();
        var chain = GetInheritanceChain(first);

        foreach (var baseType in chain)
        {
            if (types.All(t => t == baseType || InheritsFrom(t, baseType)))
                return baseType;
        }

        return "Node";
    }

    private List<string> GetInheritanceChain(string typeName)
    {
        var chain = new List<string> { typeName };
        var current = typeName;
        var visited = new HashSet<string> { current };
        while (true)
        {
            var baseType = _godotTypesProvider!.GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || !visited.Add(baseType))
                break;
            chain.Add(baseType);
            current = baseType;
        }
        return chain;
    }

    private bool InheritsFrom(string typeName, string baseTypeName)
    {
        if (typeName == baseTypeName)
            return true;

        var current = typeName;
        var visited = new HashSet<string> { current };
        while (true)
        {
            var baseType = _godotTypesProvider!.GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || !visited.Add(baseType))
                return false;
            if (baseType == baseTypeName)
                return true;
            current = baseType;
        }
    }

    private static GDExpression? FindLocalVariableInitializer(GDIdentifierExpression? identExpr)
    {
        if (identExpr == null)
            return null;
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return null;

        var parent = identExpr.Parent;
        while (parent != null)
        {
            if (parent is GDStatementsList stmts)
            {
                foreach (var stmt in stmts)
                {
                    if (stmt is GDVariableDeclarationStatement varStmt &&
                        varStmt.Identifier?.Sequence == varName)
                    {
                        return varStmt.Initializer;
                    }
                }
            }
            parent = parent.Parent;
        }
        return null;
    }

    private readonly Dictionary<GDClassDeclaration, GDSceneInstantiationCollector?> _addChildCache = new();

    private GDSceneInstantiationCollector? GetOrCollectAddChildInfo(GDClassDeclaration classDecl)
    {
        if (_addChildCache.TryGetValue(classDecl, out var cached))
            return cached;

        var collector = new GDSceneInstantiationCollector();
        classDecl.WalkIn(collector);
        _addChildCache[classDecl] = collector;
        return collector;
    }

    private GDExpression? FindVariableInitializerInHierarchy(GDClassDeclaration classDecl, string variableName)
    {
        var visited = new HashSet<string>();
        return FindVariableInitializerInHierarchyCore(classDecl, variableName, visited);
    }

    private GDExpression? FindVariableInitializerInHierarchyCore(
        GDClassDeclaration? classDecl,
        string variableName,
        HashSet<string> visited)
    {
        if (classDecl == null)
            return null;

        // Prevent infinite loops in circular inheritance
        var className = classDecl.ClassName?.Identifier?.Sequence
            ?? classDecl.ToString();
        if (!visited.Add(className))
            return null;

        // Check current class
        var result = FindVariableInitializer(classDecl, variableName);
        if (result != null)
            return result;

        // Check base class via script provider
        if (_scriptProvider != null)
        {
            var baseTypeName = classDecl.Extends?.Type?.BuildName();
            if (!string.IsNullOrEmpty(baseTypeName))
            {
                var baseScript = _scriptProvider.GetScriptByTypeName(baseTypeName)
                    ?? _scriptProvider.GetScriptByPath(baseTypeName);

                if (baseScript?.Class != null)
                    return FindVariableInitializerInHierarchyCore(baseScript.Class, variableName, visited);
            }
        }

        return null;
    }

    private static GDExpression? FindVariableInitializer(GDClassDeclaration classDecl, string variableName)
    {
        if (classDecl.Members == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == variableName)
            {
                return varDecl.Initializer;
            }
        }

        return null;
    }

    private static GDFlowLocation LocationFromNode(GDNode? node)
    {
        if (node == null)
            return default;
        return new GDFlowLocation(null, node.StartLine, node.StartColumn);
    }
}
