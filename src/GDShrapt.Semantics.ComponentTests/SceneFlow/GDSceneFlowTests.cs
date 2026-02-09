using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDSceneFlowTests
{
    private class MockFileSystem : IGDFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void AddFile(string path, string content)
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            _files[normalizedPath] = content;
        }

        private string NormalizePath(string path) =>
            path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
        public bool DirectoryExists(string path) => true;
        public string ReadAllText(string path) => _files[NormalizePath(path)];
        public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive) =>
            _files.Keys.Where(k => k.EndsWith(pattern.TrimStart('*')));
        public IEnumerable<string> GetDirectories(string directory) => new string[0];
        public string GetFullPath(string path) => NormalizePath(path);
        public string CombinePath(params string[] paths) => Path.Combine(paths);
        public string GetFileName(string path) => Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) =>
            Path.GetFileNameWithoutExtension(path);
        public string? GetDirectoryName(string path) =>
            Path.GetDirectoryName(path);
        public string GetExtension(string path) =>
            Path.GetExtension(path);
    }

    #region Phase 1: Sub-Scene Parsing

    [TestMethod]
    public void SubSceneParsing_InstanceExtResource_IsTracked()
    {
        var levelScene = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""PackedScene"" path=""res://scenes/player.tscn"" id=""1_player""]

[node name=""Level"" type=""Node2D""]

[node name=""Player"" parent=""."" instance=ExtResource(""1_player"")]
";

        var playerScene = @"
[gd_scene load_steps=1 format=3]

[node name=""Player"" type=""CharacterBody2D""]

[node name=""CollisionShape"" type=""CollisionShape2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "level.tscn"), levelScene);
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "player.tscn"), playerScene);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/level.tscn");
        provider.LoadScene("res://scenes/player.tscn");

        var sceneInfo = provider.GetSceneInfo("res://scenes/level.tscn");
        Assert.IsNotNull(sceneInfo);

        // Check that Player node is marked as sub-scene instance
        var playerNode = sceneInfo.Nodes.FirstOrDefault(n => n.Name == "Player");
        Assert.IsNotNull(playerNode);
        Assert.IsTrue(playerNode.IsSubSceneInstance);
        Assert.AreEqual("res://scenes/player.tscn", playerNode.SubScenePath);
    }

    [TestMethod]
    public void SubSceneParsing_PackedSceneResource_IsTracked()
    {
        var sceneContent = @"
[gd_scene load_steps=3 format=3]

[ext_resource type=""PackedScene"" path=""res://scenes/enemy.tscn"" id=""1_enemy""]
[ext_resource type=""PackedScene"" path=""res://scenes/npc.tscn"" id=""2_npc""]

[node name=""World"" type=""Node2D""]

[node name=""Enemy"" parent=""."" instance=ExtResource(""1_enemy"")]

[node name=""NPC"" parent=""."" instance=ExtResource(""2_npc"")]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "world.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/world.tscn");

        var refs = provider.GetSubSceneReferences("res://scenes/world.tscn");
        Assert.AreEqual(2, refs.Count);
        Assert.AreEqual("res://scenes/enemy.tscn", refs[0].SubScenePath);
        Assert.AreEqual("Enemy", refs[0].NodeName);
        Assert.AreEqual("res://scenes/npc.tscn", refs[1].SubScenePath);
        Assert.AreEqual("NPC", refs[1].NodeName);
    }

    [TestMethod]
    public void SubSceneParsing_NoInstance_NotMarked()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Main"" type=""Node2D""]

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "main.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/main.tscn");

        var sceneInfo = provider.GetSceneInfo("res://scenes/main.tscn");
        Assert.IsNotNull(sceneInfo);
        Assert.AreEqual(0, sceneInfo.SubSceneReferences.Count);
        Assert.IsFalse(sceneInfo.Nodes.Any(n => n.IsSubSceneInstance));
    }

    #endregion

    #region Phase 2: Graph Building

    [TestMethod]
    public void GraphBuild_StaticSubScenes_HasCorrectEdges()
    {
        var graph = BuildTestGraph();

        var edges = graph.GetOutgoingEdges("res://scenes/level.tscn");
        Assert.AreEqual(1, edges.Count);
        Assert.AreEqual(GDSceneFlowEdgeType.StaticSubScene, edges[0].EdgeType);
        Assert.AreEqual(GDTypeConfidence.Certain, edges[0].Confidence);
        Assert.AreEqual("res://scenes/player.tscn", edges[0].TargetScene);
        Assert.AreEqual("Player", edges[0].NodePathInParent);
    }

    [TestMethod]
    public void GraphBuild_GetScenesThatInstantiate_ReturnsParentScenes()
    {
        var graph = BuildTestGraph();

        var parents = graph.GetScenesThatInstantiate("res://scenes/player.tscn").ToList();
        Assert.AreEqual(1, parents.Count);
        Assert.AreEqual("res://scenes/level.tscn", parents[0]);
    }

    [TestMethod]
    public void GraphBuild_GetInstantiatedScenes_ReturnsChildScenes()
    {
        var graph = BuildTestGraph();

        var children = graph.GetInstantiatedScenes("res://scenes/level.tscn").ToList();
        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("res://scenes/player.tscn", children[0]);
    }

    #endregion

    #region Phase 3: Hierarchy Prediction

    [TestMethod]
    public void PredictHierarchy_StaticNodes_AlwaysPresent()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider);
        var hierarchy = predictor.Predict("res://scenes/player.tscn");

        Assert.IsNotNull(hierarchy.Root);
        Assert.AreEqual("Player", hierarchy.Root.Name);
        Assert.AreEqual(GDNodePresenceStatus.AlwaysPresent, hierarchy.Root.Presence.Status);
        Assert.AreEqual(GDTypeConfidence.Certain, hierarchy.Root.Presence.Confidence);
    }

    [TestMethod]
    public void PredictHierarchy_StaticChildren_Present()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider);
        var hierarchy = predictor.Predict("res://scenes/player.tscn");

        Assert.IsNotNull(hierarchy.Root);
        Assert.AreEqual(2, hierarchy.Root.Children.Count);

        var collision = hierarchy.Root.Children.FirstOrDefault(c => c.Name == "CollisionShape");
        Assert.IsNotNull(collision);
        Assert.AreEqual("CollisionShape2D", collision.NodeType);
        Assert.AreEqual(GDNodePresenceStatus.AlwaysPresent, collision.Presence.Status);

        var sprite = hierarchy.Root.Children.FirstOrDefault(c => c.Name == "Sprite");
        Assert.IsNotNull(sprite);
        Assert.AreEqual("Sprite2D", sprite.NodeType);
    }

    [TestMethod]
    public void PredictHierarchy_SubSceneExpansion_IncludesChildren()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider,
            new GDSceneFlowOptions { ExpandSubScenes = true });
        var hierarchy = predictor.Predict("res://scenes/level.tscn");

        Assert.IsNotNull(hierarchy.Root);

        // Find the Player sub-scene node
        var player = hierarchy.Root.Children.FirstOrDefault(c => c.Name == "Player");
        Assert.IsNotNull(player);
        Assert.IsTrue(player.IsSubSceneInstance);
        Assert.AreEqual("res://scenes/player.tscn", player.SubScenePath);

        // Sub-scene children should be expanded
        Assert.IsTrue(player.Children.Count >= 2, $"Expected >= 2 children, got {player.Children.Count}");

        var collision = player.Children.FirstOrDefault(c => c.Name == "CollisionShape");
        Assert.IsNotNull(collision, "Sub-scene child CollisionShape should be expanded");
    }

    [TestMethod]
    public void PredictHierarchy_CycleDetection_NoCrash()
    {
        // Scene A instances Scene B which instances Scene A
        var sceneA = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""PackedScene"" path=""res://b.tscn"" id=""1""]
[node name=""A"" type=""Node2D""]
[node name=""B"" parent=""."" instance=ExtResource(""1"")]
";
        var sceneB = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""PackedScene"" path=""res://a.tscn"" id=""1""]
[node name=""B"" type=""Node2D""]
[node name=""A"" parent=""."" instance=ExtResource(""1"")]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "a.tscn"), sceneA);
        mockFs.AddFile(Path.Combine(projectPath, "b.tscn"), sceneB);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://a.tscn");
        provider.LoadScene("res://b.tscn");

        var builder = new GDSceneFlowBuilder(null!, provider);
        var graph = builder.Build();

        var predictor = new GDSceneHierarchyPredictor(graph, provider,
            new GDSceneFlowOptions { ExpandSubScenes = true, MaxSubSceneDepth = 5 });

        // Should not throw or infinite loop
        var hierarchy = predictor.Predict("res://a.tscn");
        Assert.IsNotNull(hierarchy.Root);
    }

    [TestMethod]
    public void CheckNodePath_Valid_ReturnsAlwaysPresent()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider);
        var prediction = predictor.CheckNodePath("res://scenes/player.tscn", "CollisionShape");

        Assert.AreEqual(GDNodePresenceStatus.AlwaysPresent, prediction.Status);
    }

    [TestMethod]
    public void CheckNodePath_Unknown_ReturnsUnknown()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider);
        var prediction = predictor.CheckNodePath("res://scenes/player.tscn", "NonexistentNode");

        Assert.AreEqual(GDNodePresenceStatus.Unknown, prediction.Status);
    }

    [TestMethod]
    public void GetPossibleChildren_ReturnsDirectChildren()
    {
        var (graph, sceneProvider) = BuildTestGraphWithProvider();

        var predictor = new GDSceneHierarchyPredictor(graph, sceneProvider);
        var children = predictor.GetPossibleChildren("res://scenes/player.tscn", ".");

        Assert.AreEqual(2, children.Count);
        Assert.IsTrue(children.Any(c => c.Name == "CollisionShape"));
        Assert.IsTrue(children.Any(c => c.Name == "Sprite"));
    }

    #endregion

    #region Phase 4: Service API

    [TestMethod]
    public void SceneFlowGraph_AllEdges_ReturnsCorrectCounts()
    {
        var graph = BuildTestGraph();

        Assert.AreEqual(2, graph.SceneCount);
        Assert.AreEqual(1, graph.EdgeCount);
    }

    [TestMethod]
    public void SceneFlowGraph_NoOutgoing_ReturnsEmpty()
    {
        var graph = BuildTestGraph();

        var edges = graph.GetOutgoingEdges("res://scenes/player.tscn");
        Assert.AreEqual(0, edges.Count);
    }

    [TestMethod]
    public void SceneFlowGraph_NoIncoming_ReturnsEmpty()
    {
        var graph = BuildTestGraph();

        var edges = graph.GetIncomingEdges("res://scenes/level.tscn");
        Assert.AreEqual(0, edges.Count);
    }

    #endregion

    #region Helpers

    private GDSceneFlowGraph BuildTestGraph()
    {
        var (graph, _) = BuildTestGraphWithProvider();
        return graph;
    }

    private (GDSceneFlowGraph, GDSceneTypesProvider) BuildTestGraphWithProvider()
    {
        var levelScene = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""PackedScene"" path=""res://scenes/player.tscn"" id=""1_player""]

[node name=""Level"" type=""Node2D""]

[node name=""Player"" parent=""."" instance=ExtResource(""1_player"")]
";

        var playerScene = @"
[gd_scene load_steps=1 format=3]

[node name=""Player"" type=""CharacterBody2D""]

[node name=""CollisionShape"" type=""CollisionShape2D"" parent="".""]

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "level.tscn"), levelScene);
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "player.tscn"), playerScene);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/level.tscn");
        provider.LoadScene("res://scenes/player.tscn");

        var builder = new GDSceneFlowBuilder(null!, provider);
        var graph = builder.Build();

        return (graph, provider);
    }

    #endregion
}
