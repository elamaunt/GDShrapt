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
            // Try to get class_name from script
            var scriptInfo = _scriptProvider?.GetScriptByPath(resourcePath);
            if (scriptInfo?.TypeName != null)
                return scriptInfo.TypeName;
            return "GDScript";
        }

        if (resourcePath.EndsWith(".tscn") || resourcePath.EndsWith(".scn"))
            return "PackedScene";

        if (resourcePath.EndsWith(".tres") || resourcePath.EndsWith(".res"))
            return "Resource";

        // Textures, audio, etc.
        if (resourcePath.EndsWith(".png") || resourcePath.EndsWith(".jpg") ||
            resourcePath.EndsWith(".jpeg") || resourcePath.EndsWith(".webp") ||
            resourcePath.EndsWith(".svg"))
            return "Texture2D";

        if (resourcePath.EndsWith(".wav") || resourcePath.EndsWith(".ogg") ||
            resourcePath.EndsWith(".mp3"))
            return "AudioStream";

        if (resourcePath.EndsWith(".ttf") || resourcePath.EndsWith(".otf"))
            return "Font";

        if (resourcePath.EndsWith(".json"))
            return "JSON";

        if (resourcePath.EndsWith(".glb") || resourcePath.EndsWith(".gltf"))
            return "PackedScene"; // GLB/GLTF imports as scene

        return "Resource";
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
}
