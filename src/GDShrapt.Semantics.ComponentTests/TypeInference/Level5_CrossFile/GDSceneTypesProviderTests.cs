using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDSceneTypesProviderTests
{
    private class MockFileSystem : IGDFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void AddFile(string path, string content)
        {
            // Normalize path to use system separator
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

    [TestMethod]
    public void LoadScene_ParsesNodes()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3 uid=""uid://example""]

[ext_resource type=""Script"" path=""res://scripts/player.gd"" id=""1""]

[node name=""Player"" type=""CharacterBody2D""]
script = ExtResource(""1"")

[node name=""Sprite"" type=""Sprite2D"" parent="".""]

[node name=""CollisionShape"" type=""CollisionShape2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "player.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/player.tscn");

        var nodePaths = provider.GetNodePaths("res://scenes/player.tscn");

        Assert.AreEqual(3, nodePaths.Count);
        Assert.IsTrue(nodePaths.Contains("."));
        Assert.IsTrue(nodePaths.Contains("Sprite"));
        Assert.IsTrue(nodePaths.Contains("CollisionShape"));
    }

    [TestMethod]
    public void GetNodeType_ReturnsCorrectType()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Main"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://main.tscn");

        var mainType = provider.GetNodeType("res://main.tscn", ".");
        var playerType = provider.GetNodeType("res://main.tscn", "Player");

        Assert.AreEqual("Node2D", mainType);
        Assert.AreEqual("CharacterBody2D", playerType);
    }

    [TestMethod]
    public void GetNodeByName_ReturnsNodeInfo()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Root"" type=""Node""]

[node name=""Child"" type=""Sprite2D"" parent="".""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scene.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scene.tscn");

        var nodeInfo = provider.GetNodeByName("res://scene.tscn", "Child");

        Assert.IsNotNull(nodeInfo);
        Assert.AreEqual("Child", nodeInfo.Name);
        Assert.AreEqual("Sprite2D", nodeInfo.NodeType);
    }

    [TestMethod]
    public void ClearCache_RemovesAllData()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Root"" type=""Node""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scene.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scene.tscn");

        Assert.AreEqual(1, provider.AllScenes.Count());

        provider.ClearCache();

        Assert.AreEqual(0, provider.AllScenes.Count());
    }

    [TestMethod]
    public void IGDRuntimeProvider_Methods_ReturnExpectedValues()
    {
        var provider = new GDSceneTypesProvider("/project");

        // Scene provider doesn't implement type resolution directly
        Assert.IsFalse(provider.IsKnownType("Node"));
        Assert.IsNull(provider.GetTypeInfo("Node"));
        Assert.IsNull(provider.GetMember("Node", "get_parent"));
        Assert.IsNull(provider.GetBaseType("Node"));
        Assert.IsFalse(provider.IsAssignableTo("Node", "Object"));
        Assert.IsNull(provider.GetGlobalFunction("print"));
        Assert.IsNull(provider.GetGlobalClass("Node"));
        Assert.IsFalse(provider.IsBuiltIn("PI"));
    }

    #region Collision Layer Parsing

    [TestMethod]
    public void ParseScene_ExtractsCollisionLayer()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://scripts/enemy.gd"" id=""1_script""]

[node name=""Enemy"" type=""Area2D""]
collision_layer = 2
script = ExtResource(""1_script"")
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "enemy.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/enemy.tscn");

        var node = provider.GetNodeByName("res://scenes/enemy.tscn", "Enemy");
        Assert.IsNotNull(node);
        Assert.AreEqual(2, node.CollisionLayer);
    }

    [TestMethod]
    public void ParseScene_CollisionLayerDefaultsToZero()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Plain"" type=""Area2D""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "plain.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/plain.tscn");

        var node = provider.GetNodeByName("res://scenes/plain.tscn", "Plain");
        Assert.IsNotNull(node);
        Assert.AreEqual(0, node.CollisionLayer);
    }

    [TestMethod]
    public void ParseScene_MultipleNodesWithDifferentCollisionLayers()
    {
        var sceneContent = @"
[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://scripts/body.gd"" id=""1_body""]
[ext_resource type=""Script"" path=""res://scripts/hitbox.gd"" id=""2_hitbox""]

[node name=""Enemy"" type=""CharacterBody2D""]
collision_layer = 2
script = ExtResource(""1_body"")

[node name=""Hitbox"" type=""Area2D"" parent="".""]
collision_layer = 8
script = ExtResource(""2_hitbox"")

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "enemy.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/enemy.tscn");

        var enemy = provider.GetNodeByName("res://scenes/enemy.tscn", "Enemy");
        var hitbox = provider.GetNodeByName("res://scenes/enemy.tscn", "Hitbox");
        var sprite = provider.GetNodeByName("res://scenes/enemy.tscn", "Sprite");

        Assert.IsNotNull(enemy);
        Assert.AreEqual(2, enemy.CollisionLayer);

        Assert.IsNotNull(hitbox);
        Assert.AreEqual(8, hitbox.CollisionLayer);

        Assert.IsNotNull(sprite);
        Assert.AreEqual(0, sprite.CollisionLayer);
    }

    [TestMethod]
    public void GetTypesWithNonZeroCollisionLayer_ReturnsCorrectTypes()
    {
        var enemyScene = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/enemy_basic.gd"" id=""1_en""]
[node name=""EnemyBasic"" type=""Area2D""]
collision_layer = 2
script = ExtResource(""1_en"")
";
        var towerScene = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/tower_basic.gd"" id=""1_tw""]
[node name=""TowerBasic"" type=""Area2D""]
collision_layer = 4
script = ExtResource(""1_tw"")
";
        var zoneScene = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/damage_zone.gd"" id=""1_dz""]
[node name=""DamageZone"" type=""Area2D""]
collision_layer = 0
script = ExtResource(""1_dz"")
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "enemy.tscn"), enemyScene);
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "tower.tscn"), towerScene);
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "zone.tscn"), zoneScene);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/enemy.tscn");
        provider.LoadScene("res://scenes/tower.tscn");
        provider.LoadScene("res://scenes/zone.tscn");

        var types = provider.GetTypesWithNonZeroCollisionLayer();

        CollectionAssert.Contains(types.ToList(), "EnemyBasic");
        CollectionAssert.Contains(types.ToList(), "TowerBasic");
        CollectionAssert.DoesNotContain(types.ToList(), "DamageZone");
    }

    [TestMethod]
    public void GetTypesWithNonZeroCollisionLayer_NoScenes_ReturnsEmpty()
    {
        var provider = new GDSceneTypesProvider("/project");
        var types = provider.GetTypesWithNonZeroCollisionLayer();
        Assert.AreEqual(0, types.Count);
    }

    [TestMethod]
    public void GetTypesWithNonZeroCollisionLayer_AllZero_ReturnsEmpty()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/passive.gd"" id=""1_p""]
[node name=""Passive"" type=""Area2D""]
collision_layer = 0
script = ExtResource(""1_p"")
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "passive.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/passive.tscn");

        var types = provider.GetTypesWithNonZeroCollisionLayer();
        Assert.AreEqual(0, types.Count);
    }

    #endregion

    #region Avoidance Layer Parsing

    [TestMethod]
    public void ParseScene_ExtractsAvoidanceLayers_NavigationAgent2D()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://scripts/agent.gd"" id=""1_script""]

[node name=""Agent"" type=""CharacterBody2D""]
script = ExtResource(""1_script"")

[node name=""NavAgent"" type=""NavigationAgent2D"" parent="".""]
avoidance_layers = 5
avoidance_mask = 3
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "agent.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/agent.tscn");

        var navAgent = provider.GetNodeByName("res://scenes/agent.tscn", "NavAgent");
        Assert.IsNotNull(navAgent);
        Assert.AreEqual(5, navAgent.AvoidanceLayers);
        Assert.AreEqual(3, navAgent.AvoidanceMask);
    }

    [TestMethod]
    public void ParseScene_AvoidanceLayersDefaultToZero()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Agent"" type=""NavigationAgent3D""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "nav.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/nav.tscn");

        var agent = provider.GetNodeByName("res://scenes/nav.tscn", "Agent");
        Assert.IsNotNull(agent);
        Assert.AreEqual(0, agent.AvoidanceLayers);
        Assert.AreEqual(0, agent.AvoidanceMask);
    }

    [TestMethod]
    public void GetTypesWithNonZeroAvoidanceLayers_ReturnsCorrectTypes()
    {
        var activeScene = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/active_agent.gd"" id=""1_aa""]
[node name=""ActiveAgent"" type=""CharacterBody2D""]
script = ExtResource(""1_aa"")

[node name=""NavAgent"" type=""NavigationAgent2D"" parent="".""]
avoidance_layers = 7
";
        var passiveScene = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/passive_agent.gd"" id=""1_pa""]
[node name=""PassiveAgent"" type=""CharacterBody2D""]
script = ExtResource(""1_pa"")

[node name=""NavAgent"" type=""NavigationAgent2D"" parent="".""]
avoidance_layers = 0
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "active.tscn"), activeScene);
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "passive.tscn"), passiveScene);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/active.tscn");
        provider.LoadScene("res://scenes/passive.tscn");

        var types = provider.GetTypesWithNonZeroAvoidanceLayers();

        Assert.IsTrue(types.Contains("NavigationAgent2D"));
    }

    [TestMethod]
    public void GetAvoidanceLayerDetails_ReturnsCorrectInfo()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://scripts/npc.gd"" id=""1_npc""]
[node name=""NPC"" type=""CharacterBody2D""]
script = ExtResource(""1_npc"")

[node name=""NavAgent"" type=""NavigationAgent2D"" parent="".""]
avoidance_layers = 10
avoidance_mask = 6
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "npc.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/npc.tscn");

        var details = provider.GetAvoidanceLayerDetails();

        Assert.AreEqual(1, details.Count);
        Assert.AreEqual("NavigationAgent2D", details[0].TypeName);
        Assert.AreEqual(10, details[0].LayersValue);
        Assert.AreEqual(6, details[0].MaskValue);
        Assert.AreEqual(Path.Combine(projectPath, "scenes", "npc.tscn"), details[0].ScenePath);
    }

    [TestMethod]
    public void GetTypesWithNonZeroAvoidanceLayers_NoScenes_ReturnsEmpty()
    {
        var provider = new GDSceneTypesProvider("/project");
        var types = provider.GetTypesWithNonZeroAvoidanceLayers();
        Assert.AreEqual(0, types.Count);
    }

    #endregion
}
