using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference;

/// <summary>
/// Tests for signal connection parsing from .tscn scene files.
/// </summary>
[TestClass]
public class SceneSignalConnectionTests
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
        public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive)
        {
            var ext = pattern.TrimStart('*');
            return _files.Keys.Where(k => k.EndsWith(ext));
        }
        public IEnumerable<string> GetDirectories(string directory) => new string[0];
        public string GetFullPath(string path) => NormalizePath(path);
        public string CombinePath(params string[] paths) => Path.Combine(paths);
        public string GetFileName(string path) => Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
        public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string GetExtension(string path) => Path.GetExtension(path);
    }

    private MockFileSystem _mockFs = null!;
    private string _projectPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockFs = new MockFileSystem();
        _projectPath = Path.Combine("C:", "project");
    }

    #region Single Connection Tests

    [TestMethod]
    public void ParseSignalConnections_SingleConnection_ParsesCorrectly()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("pressed", connections[0].SignalName);
        Assert.AreEqual("Button", connections[0].FromNode);
        Assert.AreEqual(".", connections[0].ToNode);
        Assert.AreEqual("_on_button_pressed", connections[0].Method);
    }

    [TestMethod]
    public void ParseSignalConnections_ConnectionWithSourceType_ResolvesType()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]

[connection signal=""tree_entered"" from=""Player"" to=""."" method=""_on_player_ready""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("CharacterBody2D", connections[0].SourceNodeType);
    }

    [TestMethod]
    public void ParseSignalConnections_LineNumber_IsCorrect()
    {
        var sceneContent = @"[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(1, connections.Count);
        // Line number should point to the [connection] line
        Assert.IsTrue(connections[0].LineNumber > 0);
    }

    #endregion

    #region Multiple Connections Tests

    [TestMethod]
    public void ParseSignalConnections_MultipleConnections_ParsesAll()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button1"" type=""Button"" parent="".""]

[node name=""Button2"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button1"" to=""."" method=""_on_button1_pressed""]
[connection signal=""pressed"" from=""Button2"" to=""."" method=""_on_button2_pressed""]
[connection signal=""mouse_entered"" from=""Button1"" to=""."" method=""_on_hover""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(3, connections.Count);

        var methods = connections.Select(c => c.Method).ToList();
        CollectionAssert.Contains(methods, "_on_button1_pressed");
        CollectionAssert.Contains(methods, "_on_button2_pressed");
        CollectionAssert.Contains(methods, "_on_hover");
    }

    [TestMethod]
    public void ParseSignalConnections_SameSignalDifferentNodes()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""StartButton"" type=""Button"" parent="".""]

[node name=""QuitButton"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""StartButton"" to=""."" method=""_on_start""]
[connection signal=""pressed"" from=""QuitButton"" to=""."" method=""_on_quit""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();
        var pressedConnections = connections.Where(c => c.SignalName == "pressed").ToList();

        Assert.AreEqual(2, pressedConnections.Count);
    }

    #endregion

    #region GetConnectionsToMethod Tests

    [TestMethod]
    public void GetConnectionsToMethod_ReturnsMatchingConnections()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
[connection signal=""toggled"" from=""Button"" to=""."" method=""_on_toggled""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetConnectionsToMethod("res://main.tscn", "_on_button_pressed").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("pressed", connections[0].SignalName);
    }

    [TestMethod]
    public void GetConnectionsToMethod_NoMatches_ReturnsEmpty()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetConnectionsToMethod("res://main.tscn", "_nonexistent_method").ToList();

        Assert.AreEqual(0, connections.Count);
    }

    #endregion

    #region GetConnectionsBySignal Tests

    [TestMethod]
    public void GetConnectionsBySignal_FindsAcrossScenes()
    {
        var scene1 = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed_1""]
";
        var scene2 = @"
[gd_scene format=3]

[node name=""Dialog"" type=""Control""]
[node name=""OkButton"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""OkButton"" to=""."" method=""_on_ok""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), scene1);
        _mockFs.AddFile(Path.Combine(_projectPath, "dialog.tscn"), scene2);

        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.ReloadAllScenes();

        var connections = provider.GetConnectionsBySignal("pressed").ToList();

        Assert.AreEqual(2, connections.Count);
        Assert.IsTrue(connections.Any(c => c.connection.Method == "_on_pressed_1"));
        Assert.IsTrue(connections.Any(c => c.connection.Method == "_on_ok"));
    }

    [TestMethod]
    public void GetConnectionsBySignal_DifferentSignals_ReturnsOnlyMatching()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
[node name=""Button"" type=""Button"" parent="".""]
[node name=""Slider"" type=""Slider"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
[connection signal=""value_changed"" from=""Slider"" to=""."" method=""_on_value_changed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var pressedConnections = provider.GetConnectionsBySignal("pressed").ToList();
        var valueChangedConnections = provider.GetConnectionsBySignal("value_changed").ToList();

        Assert.AreEqual(1, pressedConnections.Count);
        Assert.AreEqual(1, valueChangedConnections.Count);
        Assert.AreEqual("_on_pressed", pressedConnections[0].connection.Method);
        Assert.AreEqual("_on_value_changed", valueChangedConnections[0].connection.Method);
    }

    #endregion

    #region GetSignalConnectionsForScriptMethod Tests

    [TestMethod]
    public void GetSignalConnectionsForScriptMethod_FindsConnectionsToScript()
    {
        var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://main.gd"" id=""1""]

[node name=""Main"" type=""Control""]
script = ExtResource(""1"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var scriptPath = Path.Combine(_projectPath, "main.gd");
        var connections = provider.GetSignalConnectionsForScriptMethod(scriptPath, "_on_button_pressed").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("pressed", connections[0].SignalName);
    }

    [TestMethod]
    public void GetSignalConnectionsForScriptMethod_ResourcePath_FindsCorrectly()
    {
        var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/player.gd"" id=""1""]

[node name=""Player"" type=""CharacterBody2D""]
script = ExtResource(""1"")

[node name=""AttackArea"" type=""Area2D"" parent="".""]

[connection signal=""body_entered"" from=""AttackArea"" to=""."" method=""_on_attack_hit""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "scenes", "player.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://scenes/player.tscn");

        // Using resource path format
        var connections = provider.GetSignalConnectionsForScriptMethod("res://scripts/player.gd", "_on_attack_hit").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("body_entered", connections[0].SignalName);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ParseSignalConnections_NoConnections_ReturnsEmpty()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(0, connections.Count);
    }

    [TestMethod]
    public void ParseSignalConnections_UnknownScene_ReturnsEmpty()
    {
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);

        var connections = provider.GetSignalConnections("res://unknown.tscn").ToList();

        Assert.AreEqual(0, connections.Count);
    }

    [TestMethod]
    public void ParseSignalConnections_NestedNodePath()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""UI"" type=""Control"" parent="".""]

[node name=""Button"" type=""Button"" parent=""UI""]

[connection signal=""pressed"" from=""UI/Button"" to=""."" method=""_on_nested_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("UI/Button", connections[0].FromNode);
    }

    [TestMethod]
    public void ParseSignalConnections_ConnectionWithExtraAttributes()
    {
        // Godot 4 may add extra attributes like flags or binds
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed"" flags=3]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();

        // Should still parse correctly despite extra attributes
        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("pressed", connections[0].SignalName);
        Assert.AreEqual("_on_pressed", connections[0].Method);
    }

    #endregion

    #region Cache and Reload Tests

    [TestMethod]
    public void ReloadAllScenes_UpdatesConnectionCache()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.ReloadAllScenes();

        var connections = provider.GetSignalConnections("res://main.tscn").ToList();
        Assert.AreEqual(1, connections.Count);

        // Update scene content
        var updatedContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
[connection signal=""mouse_entered"" from=""Button"" to=""."" method=""_on_hover""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), updatedContent);
        provider.ReloadAllScenes();

        connections = provider.GetSignalConnections("res://main.tscn").ToList();
        Assert.AreEqual(2, connections.Count);
    }

    [TestMethod]
    public void ClearCache_RemovesConnections()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Control""]
[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
";
        _mockFs.AddFile(Path.Combine(_projectPath, "main.tscn"), sceneContent);
        var provider = new GDSceneTypesProvider(_projectPath, _mockFs);
        provider.LoadScene("res://main.tscn");

        Assert.AreEqual(1, provider.GetSignalConnections("res://main.tscn").Count());

        provider.ClearCache();

        Assert.AreEqual(0, provider.GetSignalConnections("res://main.tscn").Count());
    }

    #endregion
}
