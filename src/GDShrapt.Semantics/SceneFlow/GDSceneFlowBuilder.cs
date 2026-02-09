using GDShrapt.Abstractions;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Builds a SceneFlow graph from a GDScript project.
/// </summary>
internal class GDSceneFlowBuilder
{
    private readonly GDScriptProject _project;
    private readonly GDSceneTypesProvider? _sceneProvider;
    private readonly IGDLogger? _logger;

    public GDSceneFlowBuilder(GDScriptProject project, GDSceneTypesProvider? sceneProvider, IGDLogger? logger = null)
    {
        _project = project;
        _sceneProvider = sceneProvider;
        _logger = logger;
    }

    public GDSceneFlowGraph Build()
    {
        var graph = new GDSceneFlowGraph();

        // Step 1: Add all scenes as nodes
        AddSceneNodes(graph);

        // Step 2: Static sub-scene edges (instance=ExtResource in .tscn)
        AddStaticSubSceneEdges(graph);

        // Step 3: Code-based instantiation edges (preload/load + instantiate)
        AddCodeBasedEdges(graph);

        return graph;
    }

    private void AddSceneNodes(GDSceneFlowGraph graph)
    {
        if (_sceneProvider == null)
            return;

        foreach (var sceneInfo in _sceneProvider.AllScenes)
        {
            var node = new GDSceneFlowNode
            {
                ScenePath = sceneInfo.ScenePath,
                SceneInfo = sceneInfo
            };

            foreach (var subScene in sceneInfo.SubSceneReferences)
                node.SubScenes.Add(subScene);

            graph.AddScene(node);
        }
    }

    private void AddStaticSubSceneEdges(GDSceneFlowGraph graph)
    {
        if (_sceneProvider == null)
            return;

        foreach (var sceneInfo in _sceneProvider.AllScenes)
        {
            foreach (var subScene in sceneInfo.SubSceneReferences)
            {
                graph.AddEdge(new GDSceneFlowEdge
                {
                    SourceScene = sceneInfo.ScenePath,
                    TargetScene = subScene.SubScenePath,
                    EdgeType = GDSceneFlowEdgeType.StaticSubScene,
                    Confidence = GDTypeConfidence.Certain,
                    NodePathInParent = subScene.NodePath,
                    LineNumber = subScene.LineNumber
                });
            }
        }
    }

    private void AddCodeBasedEdges(GDSceneFlowGraph graph)
    {
        if (_project?.ScriptFiles == null)
            return;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            var collector = new GDSceneInstantiationCollector();
            scriptFile.Class.WalkIn(collector);

            var scriptResPath = scriptFile.ResPath ?? scriptFile.FullPath;

            // Find which scene uses this script
            var owningScene = FindSceneForScript(scriptResPath);

            // Instantiation edges
            foreach (var inst in collector.Instantiations)
            {
                var sourceScene = owningScene ?? scriptResPath;

                graph.AddEdge(new GDSceneFlowEdge
                {
                    SourceScene = sourceScene,
                    TargetScene = inst.ScenePath,
                    EdgeType = inst.Confidence == GDTypeConfidence.High
                        ? GDSceneFlowEdgeType.PreloadInstantiate
                        : GDSceneFlowEdgeType.LoadInstantiate,
                    Confidence = inst.Confidence,
                    SourceFile = scriptResPath,
                    LineNumber = inst.LineNumber
                });

                // Add runtime node info to source scene
                var sourceNode = graph.GetScene(sourceScene);
                if (sourceNode != null)
                {
                    sourceNode.RuntimeNodes.Add(new GDRuntimeNodeInfo
                    {
                        ScenePath = inst.ScenePath,
                        Confidence = inst.Confidence,
                        SourceFile = scriptResPath ?? "",
                        LineNumber = inst.LineNumber,
                        IsConditional = inst.IsConditional
                    });
                }
            }

            // set_script edges
            foreach (var setScript in collector.SetScriptCalls)
            {
                var sourceScene = owningScene ?? scriptResPath;

                graph.AddEdge(new GDSceneFlowEdge
                {
                    SourceScene = sourceScene,
                    TargetScene = setScript.ScriptPath,
                    EdgeType = GDSceneFlowEdgeType.SetScript,
                    Confidence = setScript.Confidence,
                    SourceFile = scriptResPath,
                    LineNumber = setScript.LineNumber
                });

                var sourceNode = graph.GetScene(sourceScene);
                if (sourceNode != null)
                {
                    sourceNode.RuntimeNodes.Add(new GDRuntimeNodeInfo
                    {
                        ScriptPath = setScript.ScriptPath,
                        Confidence = setScript.Confidence,
                        SourceFile = scriptResPath ?? "",
                        LineNumber = setScript.LineNumber,
                        IsConditional = setScript.IsConditional
                    });
                }
            }

            // Track removals on the source scene node
            var sceneNode = graph.GetScene(owningScene ?? scriptResPath);
            if (sceneNode != null)
            {
                foreach (var removal in collector.RemoveNodeCalls)
                {
                    sceneNode.RuntimeNodes.Add(new GDRuntimeNodeInfo
                    {
                        ParentNodePath = removal.NodePath,
                        NodeType = removal.Method,
                        Confidence = GDTypeConfidence.Medium,
                        SourceFile = scriptResPath ?? "",
                        LineNumber = removal.LineNumber,
                        IsConditional = removal.IsConditional
                    });
                }
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
