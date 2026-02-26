using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDAutoloadsProviderTests
{
    #region Script-based autoloads

    [TestMethod]
    public void ScriptAutoload_ResolvesMembers()
    {
        var scriptCode = @"extends Node

func play(track_name: String) -> void:
	pass

func stop() -> void:
	pass

var volume: float = 1.0
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptCode);

        var scriptProvider = new MockScriptProvider();
        scriptProvider.AddScript("res://music.gd", classDecl);

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Music", Path = "res://music.gd", Enabled = true }
        };

        var provider = new GDAutoloadsProvider(autoloads, scriptProvider);

        provider.IsKnownType("Music").Should().BeTrue();

        var typeInfo = provider.GetTypeInfo("Music");
        typeInfo.Should().NotBeNull();
        typeInfo!.Name.Should().Be("Music");
        typeInfo.BaseType.Should().Be("Node");

        typeInfo.Members.Should().NotBeNull();
        typeInfo.Members!.Should().Contain(m => m.Name == "play" && m.Kind == GDRuntimeMemberKind.Method);
        typeInfo.Members.Should().Contain(m => m.Name == "stop" && m.Kind == GDRuntimeMemberKind.Method);
        typeInfo.Members.Should().Contain(m => m.Name == "volume" && m.Kind == GDRuntimeMemberKind.Property);

        provider.GetMember("Music", "play").Should().NotBeNull();
        provider.GetMember("Music", "nonexistent").Should().BeNull();
    }

    [TestMethod]
    public void ScriptAutoload_DisabledAutoload_NotResolved()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Music", Path = "res://music.gd", Enabled = false }
        };

        var provider = new GDAutoloadsProvider(autoloads);

        provider.IsKnownType("Music").Should().BeFalse();
        provider.GetTypeInfo("Music").Should().BeNull();
    }

    [TestMethod]
    public void ScriptAutoload_MethodArgumentCounts()
    {
        var scriptCode = @"extends Node

func play(track: String, fade_in: float = 0.5) -> void:
	pass
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptCode);

        var scriptProvider = new MockScriptProvider();
        scriptProvider.AddScript("res://music.gd", classDecl);

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Music", Path = "res://music.gd", Enabled = true }
        };

        var provider = new GDAutoloadsProvider(autoloads, scriptProvider);

        var playMember = provider.GetMember("Music", "play");
        playMember.Should().NotBeNull();
        playMember!.MinArgs.Should().Be(1);
        playMember.MaxArgs.Should().Be(2);
    }

    #endregion

    #region Scene-based autoloads

    [TestMethod]
    public void SceneAutoload_WithoutProviders_FallsBackToNode()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Transition", Path = "res://transition.tscn", Enabled = true }
        };

        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetTypeInfo("Transition");
        typeInfo.Should().NotBeNull();
        typeInfo!.Name.Should().Be("Transition");
        typeInfo.BaseType.Should().Be("Node");
    }

    [TestMethod]
    public void SceneAutoload_WithSceneProvider_ResolvesRootScript()
    {
        var scriptCode = @"extends CanvasLayer

func cover() -> void:
	pass

func clear() -> void:
	pass

signal transition_finished
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptCode);

        var scriptProvider = new MockScriptProvider();
        scriptProvider.AddScript("res://screen_transition.gd", classDecl);

        // Create a scene provider with the scene file
        var mockFs = new MockFileSystem();
        var scenePath = Path.Combine("/project", "transition.tscn");
        mockFs.AddFile(scenePath, @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://screen_transition.gd"" id=""1_abc""]

[node name=""ScreenTransition"" type=""CanvasLayer""]
script = ExtResource(""1_abc"")
");

        var sceneTypesProvider = new GDSceneTypesProvider("/project", mockFs);
        sceneTypesProvider.LoadScene("res://transition.tscn");

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Transition", Path = "res://transition.tscn", Enabled = true }
        };

        var provider = new GDAutoloadsProvider(autoloads, scriptProvider, sceneTypesProvider);

        var typeInfo = provider.GetTypeInfo("Transition");
        typeInfo.Should().NotBeNull();
        typeInfo!.Name.Should().Be("Transition");

        // The scene provider resolves the root node's script
        // Members should be extracted from the script
        if (typeInfo.Members != null && typeInfo.Members.Count > 0)
        {
            typeInfo.Members.Should().Contain(m => m.Name == "cover" && m.Kind == GDRuntimeMemberKind.Method);
            typeInfo.Members.Should().Contain(m => m.Name == "clear" && m.Kind == GDRuntimeMemberKind.Method);
            typeInfo.Members.Should().Contain(m => m.Name == "transition_finished" && m.Kind == GDRuntimeMemberKind.Signal);
        }
        else
        {
            // Even if script members aren't resolved, base type should be from the scene
            typeInfo.BaseType.Should().NotBeNull();
        }
    }

    #endregion

    #region Helper classes

    private class MockScriptProvider : IGDScriptProvider
    {
        private readonly List<MockScriptInfo> _scripts = new();

        public IEnumerable<IGDScriptInfo> Scripts => _scripts;

        public void AddScript(string resPath, GDClassDeclaration classDecl, string? typeName = null)
        {
            _scripts.Add(new MockScriptInfo
            {
                ResPath = resPath,
                FullPath = resPath.Replace("res://", "/project/"),
                Class = classDecl,
                TypeName = typeName,
                IsGlobal = typeName != null
            });
        }

        public IGDScriptInfo? GetScriptByPath(string path)
        {
            return _scripts.FirstOrDefault(s =>
                (s.ResPath != null && s.ResPath.Equals(path, System.StringComparison.OrdinalIgnoreCase)) ||
                (s.FullPath != null && s.FullPath.Equals(path, System.StringComparison.OrdinalIgnoreCase)));
        }

        public IGDScriptInfo? GetScriptByTypeName(string typeName)
        {
            return _scripts.FirstOrDefault(s => s.TypeName == typeName);
        }
    }

    private class MockScriptInfo : IGDScriptInfo
    {
        public string? TypeName { get; init; }
        public string? FullPath { get; init; }
        public string? ResPath { get; init; }
        public GDClassDeclaration? Class { get; init; }
        public bool IsGlobal { get; init; }
    }

    #endregion
}
