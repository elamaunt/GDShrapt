using GDShrapt.Reader;
using GDShrapt.Semantics.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference;

[TestClass]
public class GDAutoloadsProviderTests
{
    [TestMethod]
    public void GetGlobalClass_ExistingAutoload_ReturnsTypeInfo()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetGlobalClass("Global");

        Assert.IsNotNull(typeInfo);
        Assert.AreEqual("Global", typeInfo.Name);
    }

    [TestMethod]
    public void GetGlobalClass_DisabledAutoload_ReturnsNull()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Disabled", Path = "res://disabled.gd", Enabled = false }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetGlobalClass("Disabled");

        Assert.IsNull(typeInfo);
    }

    [TestMethod]
    public void GetGlobalClass_NonExistent_ReturnsNull()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetGlobalClass("NonExistent");

        Assert.IsNull(typeInfo);
    }

    [TestMethod]
    public void GetGlobalClass_WithScriptProvider_ExtractsMembers()
    {
        var scriptContent = @"extends Node
class_name Global

var player_data: Dictionary
signal game_started

func start_game() -> void:
    pass";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "Global",
            FullPath = "/project/global.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var typeInfo = provider.GetGlobalClass("Global");

        Assert.IsNotNull(typeInfo);
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "player_data"));
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "game_started"));
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "start_game"));
    }

    [TestMethod]
    public void IsKnownType_Always_ReturnsFalse()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        // Autoloads are not types, they are instances
        Assert.IsFalse(provider.IsKnownType("Global"));
    }

    [TestMethod]
    public void GetGlobalClass_SceneAutoload_ReturnsNodeType()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "LevelManager", Path = "res://scenes/level_manager.tscn", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetGlobalClass("LevelManager");

        Assert.IsNotNull(typeInfo);
        Assert.AreEqual("LevelManager", typeInfo.Name);
        Assert.AreEqual("Node", typeInfo.BaseType);
    }

    [TestMethod]
    public void Autoloads_Property_ReturnsOnlyEnabled()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Enabled1", Path = "res://e1.gd", Enabled = true },
            new GDAutoloadEntry { Name = "Disabled1", Path = "res://d1.gd", Enabled = false },
            new GDAutoloadEntry { Name = "Enabled2", Path = "res://e2.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var enabledAutoloads = provider.Autoloads.ToList();

        Assert.AreEqual(2, enabledAutoloads.Count);
        Assert.IsTrue(enabledAutoloads.All(a => a.Enabled));
    }

    [TestMethod]
    public void GetGlobalClass_CachesTypeInfo()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo1 = provider.GetGlobalClass("Global");
        var typeInfo2 = provider.GetGlobalClass("Global");

        Assert.IsNotNull(typeInfo1);
        Assert.IsNotNull(typeInfo2);
        Assert.AreSame(typeInfo1, typeInfo2);
    }

    [TestMethod]
    public void GetGlobalClass_EmptyName_ReturnsNull()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        Assert.IsNull(provider.GetGlobalClass(""));
        Assert.IsNull(provider.GetGlobalClass(null!));
    }

    [TestMethod]
    public void GetGlobalClass_WithConstants_ExtractsConstants()
    {
        var scriptContent = @"extends Node

const VERSION = ""1.0.0""
const MAX_PLAYERS = 4";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "Config",
            FullPath = "/project/config.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Config", Path = "res://config.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var typeInfo = provider.GetGlobalClass("Config");

        Assert.IsNotNull(typeInfo);
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "VERSION"));
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "MAX_PLAYERS"));
    }
}
