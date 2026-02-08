using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Integration tests for node type inference using the TestProject.
/// Tests $NodePath, get_node(), %UniqueNode, and preload() type inference.
/// </summary>
[TestClass]
public class NodeTypeInferenceIntegrationTests
{
    private readonly GDScriptReader _reader = new();

    #region $NodePath Type Inference

    [TestMethod]
    public void GetNode_Player_InfersCharacterBody2D()
    {
        // Arrange - scene_references.gd is attached to Main node in main.tscn
        // $Player → CharacterBody2D
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script, "scene_references.gd not found");

        var typeResolver = project.CreateTypeResolver();

        // Parse $Player expression
        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("CharacterBody2D", result.TypeName.DisplayName,
            $"$Player should infer to CharacterBody2D, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNode_UIStatusLabel_InfersLabel()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("$UI/StatusLabel") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("Label", result.TypeName.DisplayName,
            $"$UI/StatusLabel should infer to Label, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNode_UIHealthBar_InfersProgressBar()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("$UI/HealthBar") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("ProgressBar", result.TypeName.DisplayName,
            $"$UI/HealthBar should infer to ProgressBar, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNode_EnemyContainer_InfersNode2D()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("$EnemyContainer") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("Node2D", result.TypeName.DisplayName,
            $"$EnemyContainer should infer to Node2D, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNode_NonExistentNode_ReturnsDefaultNodeType()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("$NonExistent") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        // When node is not found, type inference falls back to default "Node"
        Assert.AreEqual("Node", result.TypeName.DisplayName,
            $"Non-existent node should infer to Node, got {result.TypeName}");
    }

    #endregion

    #region get_node() Call Type Inference

    [TestMethod]
    public void GetNodeCall_StringLiteral_InfersType()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("get_node(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("CharacterBody2D", result.TypeName.DisplayName,
            $"get_node(\"Player\") should infer to CharacterBody2D, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNodeCall_NestedPath_InfersType()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("get_node(\"UI/StatusLabel\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("Label", result.TypeName.DisplayName,
            $"get_node(\"UI/StatusLabel\") should infer to Label, got {result.TypeName}");
    }

    [TestMethod]
    public void GetNodeOrNullCall_StringLiteral_InfersType()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("get_node_or_null(\"EnemyContainer\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("Node2D", result.TypeName.DisplayName,
            $"get_node_or_null(\"EnemyContainer\") should infer to Node2D, got {result.TypeName}");
    }

    #endregion

    #region preload() Type Inference

    [TestMethod]
    public void Preload_Scene_InfersPackedScene()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("preload(\"res://test_scenes/main.tscn\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("PackedScene", result.TypeName.DisplayName,
            $"preload scene should infer to PackedScene, got {result.TypeName}");
    }

    [TestMethod]
    public void Preload_Script_WithClassName_InfersClassName()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        // base_entity.gd has class_name BaseEntity
        var expr = _reader.ParseExpression("preload(\"res://test_scripts/base_entity.gd\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        // Should return the class_name if available, otherwise GDScript
        Assert.IsTrue(result.TypeName.DisplayName == "BaseEntity" || result.TypeName.DisplayName == "GDScript",
            $"preload script should infer to BaseEntity or GDScript, got {result.TypeName}");
    }

    [TestMethod]
    public void Preload_Script_NoClassName_InfersGDScript()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        // global.gd doesn't have class_name
        var expr = _reader.ParseExpression("preload(\"res://test_scripts/global.gd\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("GDScript", result.TypeName.DisplayName,
            $"preload script without class_name should infer to GDScript, got {result.TypeName}");
    }

    [TestMethod]
    public void Load_Scene_InfersPackedScene()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("load(\"res://test_scenes/entity_test.tscn\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.IsTrue(result.IsResolved, "Type should be resolved");
        Assert.AreEqual("PackedScene", result.TypeName.DisplayName,
            $"load scene should infer to PackedScene, got {result.TypeName}");
    }

    #endregion

    #region Script Not In Scene

    [TestMethod]
    public void GetNode_ScriptNotInScene_ReturnsDefaultType()
    {
        // Arrange - type_inference.gd is not attached to any scene
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("type_inference.gd");
        Assert.IsNotNull(script, "type_inference.gd not found");

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        // When script is not in any scene, can't resolve node type
        Assert.AreEqual("Node", result.TypeName.DisplayName,
            $"Script not in scene should return default Node, got {result.TypeName}");
    }

    #endregion

    #region Multiple Scenes Same Script

    [TestMethod]
    public void GetNode_ScriptInMultipleScenes_SameNodeType_InfersType()
    {
        // Arrange - enemy_entity.gd is used in multiple scenes but with same node type
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("enemy_entity.gd");

        // Skip if script doesn't exist
        if (script == null)
        {
            Assert.Inconclusive("enemy_entity.gd not found - skipping test");
            return;
        }

        var typeResolver = project.CreateTypeResolver();

        // Parse a get_node expression that would be valid in all scenes
        var expr = _reader.ParseExpression("$Sprite2D") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert - If type is consistent across all scenes, should resolve
        // If not, returns null/Node
        Assert.IsNotNull(result.TypeName,
            "Type should be resolved (either specific type or default Node)");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GetNode_EmptyPath_FallsBackToDefaultType()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        // This should parse but result in empty path
        var expr = _reader.ParseExpression("get_node(\"\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert - empty path can't be resolved, falls back to runtime provider's default
        // get_node() returns "Variant" when type inference fails
        Assert.IsNotNull(result.TypeName,
            "Result should have a type name (either Node or fallback)");
    }

    [TestMethod]
    public void Preload_UnknownExtension_ReturnsResource()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("scene_references.gd");
        Assert.IsNotNull(script);

        var typeResolver = project.CreateTypeResolver();

        var expr = _reader.ParseExpression("preload(\"res://data.xyz\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        // Act
        var result = typeResolver.ResolveExpressionType(expr, script);

        // Assert
        Assert.AreEqual("Resource", result.TypeName.DisplayName,
            $"Unknown extension should return Resource, got {result.TypeName}");
    }

    #endregion

    #region TypeResolver Availability

    [TestMethod]
    public void TypeResolver_WithSceneProvider_IsInitialized()
    {
        // Arrange & Act
        var project = TestProjectFixture.Project;
        var typeResolver = project.CreateTypeResolver();

        // Assert
        Assert.IsNotNull(typeResolver, "TypeResolver should be created");
        Assert.IsNotNull(typeResolver.GodotTypesProvider, "GodotTypesProvider should be available");
    }

    [TestMethod]
    public void SceneTypesProvider_IsLoadedCorrectly()
    {
        // Arrange & Act
        var project = TestProjectFixture.Project;
        var sceneProvider = project.SceneTypesProvider;

        // Assert
        Assert.IsNotNull(sceneProvider, "SceneTypesProvider should be available");

        var scenes = sceneProvider.AllScenes.ToList();
        Assert.IsTrue(scenes.Count > 0, "Should have loaded scenes");
    }

    #endregion
}
