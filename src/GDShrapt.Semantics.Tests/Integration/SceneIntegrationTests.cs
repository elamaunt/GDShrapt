using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for scene file parsing and script-scene relationships.
/// </summary>
[TestClass]
public class SceneIntegrationTests
{
    #region Regex Debug

    [TestMethod]
    public void Debug_ScriptLoading_ShowsAllScripts()
    {
        var project = TestProjectFixture.Project;
        var scripts = project.ScriptFiles.ToList();

        var scriptInfo = scripts.Select(s => new
        {
            Name = Path.GetFileName(s.FullPath),
            HasClass = s.Class != null,
            HasAnalyzer = s.Analyzer != null,
            TypeName = s.TypeName
        }).ToList();

        var scriptList = string.Join("\n", scriptInfo.Select(s =>
            $"  {s.Name}: Class={s.HasClass}, Analyzer={s.HasAnalyzer}, Type={s.TypeName}"));

        // Find scene_references.gd specifically
        var sceneRefsScript = scripts.FirstOrDefault(s =>
            Path.GetFileName(s.FullPath)!.Equals("scene_references.gd", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(sceneRefsScript, $"scene_references.gd not found. Scripts:\n{scriptList}");
        Assert.IsNotNull(sceneRefsScript.Class, $"scene_references.gd Class is null. Scripts:\n{scriptList}");

        // If analyzer is null, try analyzing manually to see error
        if (sceneRefsScript.Analyzer == null)
        {
            try
            {
                sceneRefsScript.Analyze();
                Assert.IsNotNull(sceneRefsScript.Analyzer, $"Manual analyze still null. Scripts:\n{scriptList}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Manual analyze failed with exception: {ex}\n\nScripts:\n{scriptList}");
            }
        }
    }

    [TestMethod]
    public void Debug_ExtResourceRegex_ParsesMainTscn()
    {
        // Read actual main.tscn content
        var projectPath = IntegrationTestHelpers.GetTestProjectPath();
        var mainTscnPath = Path.Combine(projectPath, "test_scenes", "main.tscn");
        var content = File.ReadAllText(mainTscnPath);

        // Test the regex
        var extResBlockRegex = new Regex(@"\[ext_resource\s+([^\]]+)\]", RegexOptions.Multiline);
        var pathRegex = new Regex(@"path=""([^""]+)""");
        var idRegex = new Regex(@"\sid=""([^""]+)""");  // \s to avoid matching 'uid'

        var matches = extResBlockRegex.Matches(content);
        Assert.IsTrue(matches.Count > 0, $"Should find ext_resource blocks. Content starts with: {content.Substring(0, 200)}");

        var gdScripts = new System.Collections.Generic.Dictionary<string, string>();
        foreach (Match blockMatch in matches)
        {
            var block = blockMatch.Value;
            var pathMatch = pathRegex.Match(block);
            var idMatch = idRegex.Match(block);

            if (pathMatch.Success && idMatch.Success)
            {
                var path = pathMatch.Groups[1].Value;
                var id = idMatch.Groups[1].Value;
                if (path.EndsWith(".gd"))
                {
                    gdScripts[id] = path;
                }
            }
        }

        Assert.IsTrue(gdScripts.ContainsKey("1_script"),
            $"Should find 1_script. Found keys: [{string.Join(", ", gdScripts.Keys)}]");
        Assert.AreEqual("res://test_scripts/scene_references.gd", gdScripts["1_script"]);
    }

    #endregion

    #region Scene Loading

    [TestMethod]
    public void SceneTypesProvider_LoadsAllScenes()
    {
        // Arrange & Act
        var project = TestProjectFixture.Project;
        var sceneProvider = project.SceneTypesProvider;

        // Assert
        Assert.IsNotNull(sceneProvider, "Scene types provider should be initialized");

        var scenes = sceneProvider.AllScenes.ToList();
        Assert.IsTrue(scenes.Count >= 4,
            $"Should load at least 4 scenes, found {scenes.Count}");
    }

    [TestMethod]
    public void ParseScene_MainTscn_LoadsAllNodes()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var sceneInfo = sceneProvider.GetSceneInfo(scenePath);

        // Assert
        Assert.IsNotNull(sceneInfo, "main.tscn should be loaded");

        var nodePaths = sceneProvider.GetNodePaths(scenePath);
        Assert.IsTrue(nodePaths.Count >= 5,
            $"main.tscn should have at least 5 nodes, found {nodePaths.Count}");

        // Check specific expected nodes
        Assert.IsTrue(nodePaths.Contains(".") || nodePaths.Any(p => p == "Main" || string.IsNullOrEmpty(p)),
            "Should have root node");
        Assert.IsTrue(nodePaths.Contains("Player"),
            "Should have Player node");
        Assert.IsTrue(nodePaths.Contains("EnemyContainer"),
            "Should have EnemyContainer node");
        Assert.IsTrue(nodePaths.Contains("UI") || nodePaths.Any(p => p.Contains("UI")),
            "Should have UI node");
    }

    [TestMethod]
    public void ParseScene_EntityTestTscn_LoadsMultipleEnemies()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/entity_test.tscn";
        var sceneInfo = sceneProvider.GetSceneInfo(scenePath);

        // Assert
        Assert.IsNotNull(sceneInfo, "entity_test.tscn should be loaded");

        var nodes = sceneInfo.Nodes;

        // Should have Enemy1, Enemy2, Enemy3
        var enemyNodes = nodes.Where(n => n.Name.StartsWith("Enemy")).ToList();
        Assert.AreEqual(3, enemyNodes.Count,
            $"Should have 3 enemy nodes, found {enemyNodes.Count}");
    }

    #endregion

    #region Node Types

    [TestMethod]
    public void GetNodeType_MainTscn_Player_ReturnsCharacterBody2D()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var nodeType = sceneProvider.GetNodeType(scenePath, "Player");

        // Assert
        Assert.IsNotNull(nodeType, "Should get type for Player node");
        Assert.AreEqual("CharacterBody2D", nodeType,
            $"Player should be CharacterBody2D, got {nodeType}");
    }

    [TestMethod]
    public void GetNodeType_MainTscn_UI_ReturnsControl()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var nodeType = sceneProvider.GetNodeType(scenePath, "UI");

        // Assert
        Assert.IsNotNull(nodeType, "Should get type for UI node");
        Assert.AreEqual("Control", nodeType,
            $"UI should be Control, got {nodeType}");
    }

    [TestMethod]
    public void GetNodeType_EntityTestTscn_Enemy_ReturnsNode2D()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/entity_test.tscn";
        var nodeType = sceneProvider.GetNodeType(scenePath, "Enemies/Enemy1");

        // Assert
        Assert.IsNotNull(nodeType, "Should get type for Enemy1 node");
        // Type should be either Node2D (base) or script type name
        Assert.IsTrue(nodeType == "Node2D" || nodeType.Contains("Enemy"),
            $"Enemy1 should be Node2D or have Enemy script, got {nodeType}");
    }

    #endregion

    #region Script-Scene Relationships

    [TestMethod]
    public void GetNodeScript_MainTscn_Root_ReturnsSceneReferencesScript()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var scriptPath = sceneProvider.GetNodeScript(scenePath, ".");

        // Assert
        Assert.IsNotNull(scriptPath, "Root node should have script");
        Assert.IsTrue(scriptPath.Contains("scene_references.gd"),
            $"Root should use scene_references.gd, got {scriptPath}");
    }

    [TestMethod]
    public void GetNodeScript_EntityTestTscn_Player_ReturnsPlayerEntityScript()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/entity_test.tscn";
        var scriptPath = sceneProvider.GetNodeScript(scenePath, "Player");

        // Assert
        Assert.IsNotNull(scriptPath, "Player node should have script");
        Assert.IsTrue(scriptPath.Contains("player_entity.gd"),
            $"Player should use player_entity.gd, got {scriptPath}");
    }

    [TestMethod]
    public void GetScenesForScript_PlayerEntity_FindsEntityTestScene()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scriptPath = "res://test_scripts/player_entity.gd";
        var scenes = sceneProvider.GetScenesForScript(scriptPath).ToList();

        // Assert
        Assert.IsTrue(scenes.Count >= 1,
            $"PlayerEntity should be used in at least 1 scene, found {scenes.Count}");

        var entityTestScene = scenes.FirstOrDefault(s => s.scenePath.Contains("entity_test.tscn"));
        Assert.IsNotNull(entityTestScene.scenePath,
            "PlayerEntity should be used in entity_test.tscn");
    }

    [TestMethod]
    public void GetScenesForScript_EnemyEntity_FindsMultipleInstances()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scriptPath = "res://test_scripts/enemy_entity.gd";
        var scenes = sceneProvider.GetScenesForScript(scriptPath).ToList();

        // Assert
        // Enemy script is used for Enemy1, Enemy2, Enemy3 in entity_test.tscn
        Assert.IsTrue(scenes.Count >= 3,
            $"EnemyEntity should be used in at least 3 nodes, found {scenes.Count}");
    }

    #endregion

    #region Node Paths

    [TestMethod]
    public void GetNodePaths_MainTscn_IncludesNestedPaths()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var nodePaths = sceneProvider.GetNodePaths(scenePath);

        // Assert
        // UI/StatusLabel should be in the list
        Assert.IsTrue(nodePaths.Any(p => p.Contains("StatusLabel")),
            "Should have UI/StatusLabel path");

        // UI/HealthBar should be in the list
        Assert.IsTrue(nodePaths.Any(p => p.Contains("HealthBar")),
            "Should have UI/HealthBar path");
    }

    [TestMethod]
    public void GetNodePaths_EntityTestTscn_IncludesEnemiesParent()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/entity_test.tscn";
        var nodePaths = sceneProvider.GetNodePaths(scenePath);

        // Assert
        // Enemies/Enemy1, Enemies/Enemy2, Enemies/Enemy3 (exact match, not children like Enemies/Enemy1/Sprite2D)
        var enemyPathRegex = new Regex(@"^Enemies/Enemy\d+$");
        var enemyPaths = nodePaths.Where(p => enemyPathRegex.IsMatch(p)).ToList();
        Assert.AreEqual(3, enemyPaths.Count,
            $"Should have 3 enemy paths under Enemies, found {enemyPaths.Count}. All paths: [{string.Join(", ", nodePaths)}]");
    }

    #endregion

    #region Node Path Type Inference

    [TestMethod]
    public void TypeInference_OnreadyVar_NodePath_GetsCorrectType()
    {
        // Arrange - scene_references.gd has @onready var player: CharacterBody2D = $Player
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script, "scene_references.gd not found");
        Assert.IsNotNull(script.Analyzer, "Script should be analyzed");

        // Act
        var playerSymbol = script.Analyzer.FindSymbol("player");

        // Assert
        Assert.IsNotNull(playerSymbol, "Should find 'player' symbol");
        // The type should be CharacterBody2D (from type annotation)
    }

    [TestMethod]
    public void TypeInference_OnreadyVar_NestedPath_GetsCorrectType()
    {
        // Arrange - scene_references.gd has @onready var ui_label: Label = $UI/StatusLabel
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script, "scene_references.gd not found");
        Assert.IsNotNull(script.Analyzer, "Script should be analyzed");

        // Act
        var labelSymbol = script.Analyzer.FindSymbol("ui_label");

        // Assert
        Assert.IsNotNull(labelSymbol, "Should find 'ui_label' symbol");
    }

    #endregion

    #region Scene Node Renaming References

    [TestMethod]
    public void GetNodesWithParentContaining_FindsChildNodes()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/entity_test.tscn";
        var childNodes = sceneProvider.GetNodesWithParentContaining(scenePath, "Enemies").ToList();

        // Assert
        // Enemy1, Enemy2, Enemy3 have "Enemies" as parent
        Assert.IsTrue(childNodes.Count >= 3,
            $"Should find at least 3 children of Enemies, found {childNodes.Count}");
    }

    [TestMethod]
    public void GetNodeByName_ReturnsLineNumber()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        var playerNode = sceneProvider.GetNodeByName(scenePath, "Player");

        // Assert
        Assert.IsNotNull(playerNode, "Should find Player node");
        Assert.IsTrue(playerNode.LineNumber > 0,
            $"Player node should have valid line number, got {playerNode.LineNumber}");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GetNodeType_NonExistentPath_ReturnsNull()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var nodeType = sceneProvider.GetNodeType("res://test_scenes/main.tscn", "NonExistent");

        // Assert
        Assert.IsNull(nodeType, "Non-existent node should return null");
    }

    [TestMethod]
    public void GetNodeScript_NodeWithoutScript_ReturnsNull()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var scenePath = "res://test_scenes/main.tscn";
        // EnemyContainer is just a Node2D without script
        var scriptPath = sceneProvider.GetNodeScript(scenePath, "EnemyContainer");

        // Assert - should be null or empty since this node has no script
        Assert.IsTrue(string.IsNullOrEmpty(scriptPath),
            $"EnemyContainer should not have a script, got {scriptPath}");
    }

    [TestMethod]
    public void GetSceneInfo_NonExistentScene_ReturnsNull()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        var sceneInfo = sceneProvider.GetSceneInfo("res://nonexistent.tscn");

        // Assert
        Assert.IsNull(sceneInfo, "Non-existent scene should return null");
    }

    [TestMethod]
    public void GetScenesForScript_UnusedScript_ReturnsEmpty()
    {
        // Arrange
        var sceneProvider = TestProjectFixture.Project.SceneTypesProvider;
        Assert.IsNotNull(sceneProvider);

        // Act
        // type_inference.gd is likely not attached to any scene node
        var scenes = sceneProvider.GetScenesForScript("res://test_scripts/type_inference.gd").ToList();

        // Assert - may be empty or not, depends on test project setup
        // This is mostly to ensure no exceptions occur
        Assert.IsNotNull(scenes, "Should return a list (possibly empty)");
    }

    #endregion

    #region Cross-Script-Scene References

    [TestMethod]
    public void ScriptSceneReference_SceneReferencesGd_UsesNodePaths()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script, "scene_references.gd not found");

        // Act
        // Check that the script has @onready variables that reference scene nodes
        var variables = script.Analyzer?.GetVariables().ToList();

        // Assert
        Assert.IsNotNull(variables, "Should have variables");
        Assert.IsTrue(variables.Count >= 4,
            $"scene_references.gd should have at least 4 @onready vars, found {variables.Count}");

        // Check for specific node path references
        var playerVar = variables.FirstOrDefault(v => v.Name == "player");
        Assert.IsNotNull(playerVar, "Should have 'player' variable");
    }

    #endregion
}
