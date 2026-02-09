using GDShrapt.Abstractions;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Builds a ResourceFlow graph from scene data and GDScript AST analysis.
/// </summary>
internal class GDResourceFlowBuilder
{
    private readonly GDScriptProject _project;
    private readonly GDSceneTypesProvider? _sceneProvider;

    public GDResourceFlowBuilder(GDScriptProject project, GDSceneTypesProvider? sceneProvider)
    {
        _project = project;
        _sceneProvider = sceneProvider;
    }

    public GDResourceFlowGraph Build()
    {
        var graph = new GDResourceFlowGraph();

        AddSceneResourceEdges(graph);

        AddCodeResourceEdges(graph);

        return graph;
    }

    private void AddSceneResourceEdges(GDResourceFlowGraph graph)
    {
        if (_sceneProvider == null)
            return;

        foreach (var sceneInfo in _sceneProvider.AllScenes)
        {
            // Add edges for all ext_resources that are not scripts or scenes
            foreach (var kvp in sceneInfo.AllExtResources)
            {
                var (path, type) = kvp.Value;
                if (path.EndsWith(".gd") || path.EndsWith(".tscn") || path.EndsWith(".scn"))
                    continue;

                var category = GDResourceCategoryResolver.CategoryFromTypeName(type);
                if (category == GDResourceCategory.Other)
                    category = GDResourceCategoryResolver.CategoryFromExtension(path);

                if (!graph.AllResourcePaths.Contains(path))
                {
                    graph.AddResource(new GDResourceNode
                    {
                        ResourcePath = path,
                        ResourceType = type,
                        Category = category
                    });
                }
            }

            // Add edges for property → resource assignments
            foreach (var resRef in sceneInfo.ResourceReferences)
            {
                var category = GDResourceCategoryResolver.CategoryFromTypeName(resRef.ResourceType);
                if (category == GDResourceCategory.Other)
                    category = GDResourceCategoryResolver.CategoryFromExtension(resRef.ResourcePath);

                graph.AddEdge(new GDResourceFlowEdge
                {
                    ConsumerPath = sceneInfo.ScenePath,
                    ResourcePath = resRef.ResourcePath,
                    Source = GDResourceReferenceSource.SceneNodeProperty,
                    Confidence = GDTypeConfidence.Certain,
                    NodePath = resRef.NodePath,
                    PropertyName = resRef.PropertyName,
                    LineNumber = resRef.LineNumber
                });
            }

            // Add edges for ext_resources that are not bound to node properties (scene-level dependencies)
            var boundPaths = sceneInfo.ResourceReferences.Select(r => r.ResourcePath).ToHashSet();
            foreach (var kvp in sceneInfo.AllExtResources)
            {
                var (path, type) = kvp.Value;
                if (path.EndsWith(".gd") || path.EndsWith(".tscn") || path.EndsWith(".scn"))
                    continue;
                if (boundPaths.Contains(path))
                    continue;

                graph.AddEdge(new GDResourceFlowEdge
                {
                    ConsumerPath = sceneInfo.ScenePath,
                    ResourcePath = path,
                    Source = GDResourceReferenceSource.SceneExtResource,
                    Confidence = GDTypeConfidence.Certain
                });
            }
        }
    }

    private void AddCodeResourceEdges(GDResourceFlowGraph graph)
    {
        if (_project?.ScriptFiles == null)
            return;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            var collector = new GDResourceLoadCollector();
            scriptFile.Class.WalkIn(collector);

            var scriptResPath = scriptFile.ResPath ?? scriptFile.FullPath;

            // Find which scene uses this script
            var owningScene = FindSceneForScript(scriptResPath);

            foreach (var load in collector.ResourceLoads)
            {
                var consumerPath = owningScene ?? scriptResPath;

                graph.AddEdge(new GDResourceFlowEdge
                {
                    ConsumerPath = consumerPath,
                    ResourcePath = load.ResourcePath,
                    Source = load.Source,
                    Confidence = load.Confidence,
                    SourceFile = scriptResPath,
                    VariableName = load.VariableName,
                    IsConditional = load.IsConditional,
                    LineNumber = load.LineNumber
                });
            }
        }
    }

    private string? FindSceneForScript(string? scriptResPath)
    {
        if (_sceneProvider == null || string.IsNullOrEmpty(scriptResPath))
            return null;

        var scenes = _sceneProvider.GetScenesForScript(scriptResPath).ToList();
        if (scenes.Count > 0)
            return scenes[0].scenePath;

        return null;
    }
}
