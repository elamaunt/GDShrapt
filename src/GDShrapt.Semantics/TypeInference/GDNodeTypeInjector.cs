using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public GDNodeTypeInjector(
        GDSceneTypesProvider? sceneProvider = null,
        IGDScriptProvider? scriptProvider = null,
        GDGodotTypesProvider? godotTypesProvider = null,
        IGDLogger? logger = null)
    {
        _sceneProvider = sceneProvider;
        _scriptProvider = scriptProvider;
        _godotTypesProvider = godotTypesProvider;
        _logger = logger;
    }

    public string? InjectType(GDNode node, GDTypeInjectionContext context)
    {
        return node switch
        {
            GDGetNodeExpression getNode => InferGetNodeType(getNode, context),
            GDGetUniqueNodeExpression uniqueNode => InferUniqueNodeType(uniqueNode, context),
            GDCallExpression call => InferCallType(call, context),
            _ => null
        };
    }

    private string? InferGetNodeType(GDGetNodeExpression expr, GDTypeInjectionContext context)
    {
        if (_sceneProvider == null)
            return null;

        var nodePath = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);
        if (string.IsNullOrEmpty(nodePath))
            return null;

        return ResolveNodeType(nodePath, context.ScriptPath);
    }

    private string? InferUniqueNodeType(GDGetUniqueNodeExpression expr, GDTypeInjectionContext context)
    {
        if (_sceneProvider == null)
            return null;

        var nodeName = GDNodePathExtractor.ExtractFromUniqueNodeExpression(expr);
        if (string.IsNullOrEmpty(nodeName))
            return null;

        return ResolveUniqueNodeType(nodeName, context.ScriptPath);
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
                return ResolveNodeType(nodePath, context.ScriptPath);
        }

        // preload() and load()
        if (GDWellKnownFunctions.IsResourceLoader(callName))
        {
            var resourcePath = GDNodePathExtractor.ExtractResourcePath(call);
            if (!string.IsNullOrEmpty(resourcePath))
                return ResolvePreloadType(resourcePath);
        }

        // preload("scene.tscn").instantiate() or scene_var.instantiate()
        if (callName == "instantiate")
        {
            var scenePath = ExtractScenePathFromCaller(call);
            if (!string.IsNullOrEmpty(scenePath))
                return ResolveSceneRootType(scenePath);
        }

        // instance.get_child(N) where instance comes from scene instantiate
        if (callName == "get_child" || callName == "get_child_or_null")
        {
            var result = InferGetChildOnSceneInstance(call);
            if (!string.IsNullOrEmpty(result))
                return result;
        }

        return null;
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

    private string? ExtractScenePathFromCaller(GDCallExpression instantiateCall)
    {
        var caller = instantiateCall.CallerExpression;
        if (caller is not GDMemberOperatorExpression memberExpr)
            return null;

        // Direct: preload("res://scene.tscn").instantiate()
        if (memberExpr.CallerExpression is GDCallExpression preloadCall)
            return ExtractScenePathFromPreloadCall(preloadCall);

        // Variable: scene_var.instantiate()
        if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
            return ExtractScenePathFromVariable(identExpr, instantiateCall);

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

    private string? ExtractScenePathFromVariable(GDIdentifierExpression identExpr, GDCallExpression contextCall)
    {
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return null;

        var classDecl = contextCall.RootClassDeclaration;
        if (classDecl == null)
            return null;

        var initExpr = FindVariableInitializer(classDecl, varName);
        if (initExpr is GDCallExpression preloadCall)
            return ExtractScenePathFromPreloadCall(preloadCall);

        return null;
    }

    private string? ResolveSceneRootType(string scenePath)
    {
        if (_sceneProvider == null)
            return null;

        if (_sceneProvider.GetSceneInfo(scenePath) == null)
            _sceneProvider.LoadScene(scenePath);

        return _sceneProvider.GetRootNodeType(scenePath);
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

        var initExpr = FindVariableInitializer(classDecl, varName);
        if (initExpr is not GDCallExpression instantiateCall)
            return null;

        var instantiateName = GDNodePathExtractor.GetCallName(instantiateCall);
        if (instantiateName != "instantiate")
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
}
