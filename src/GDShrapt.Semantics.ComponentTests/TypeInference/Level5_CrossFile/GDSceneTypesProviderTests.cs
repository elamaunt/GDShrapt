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
}
