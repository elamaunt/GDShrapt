using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

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
    public void IsKnownType_RegisteredAutoload_ReturnsTrue()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        Assert.IsTrue(provider.IsKnownType("Global"));
        Assert.IsFalse(provider.IsKnownType("NonExistent"));
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
    public void GetMember_ExistingMethod_ReturnsMemberInfo()
    {
        var scriptContent = @"extends Node

var player_data: Dictionary

func start_game() -> void:
    pass

func get_score() -> int:
    return 0";

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

        var member = provider.GetMember("Global", "start_game");
        Assert.IsNotNull(member);
        Assert.AreEqual("start_game", member.Name);
        Assert.AreEqual(GDRuntimeMemberKind.Method, member.Kind);

        var property = provider.GetMember("Global", "player_data");
        Assert.IsNotNull(property);
        Assert.AreEqual("player_data", property.Name);
        Assert.AreEqual(GDRuntimeMemberKind.Property, property.Kind);

        Assert.IsNull(provider.GetMember("Global", "nonexistent"));
        Assert.IsNull(provider.GetMember("NonExistent", "start_game"));
    }

    [TestMethod]
    public void GetBaseType_ScriptAutoload_ReturnsBaseType()
    {
        var scriptContent = @"extends Node2D

func do_something():
    pass";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "MyAutoload",
            FullPath = "/project/my_autoload.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "MyAutoload", Path = "res://my_autoload.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var baseType = provider.GetBaseType("MyAutoload");
        Assert.AreEqual("Node2D", baseType);

        Assert.IsNull(provider.GetBaseType("NonExistent"));
    }

    [TestMethod]
    public void GetTypeInfo_ExistingAutoload_ReturnsTypeInfo()
    {
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads);

        var typeInfo = provider.GetTypeInfo("Global");
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual("Global", typeInfo.Name);

        Assert.IsNull(provider.GetTypeInfo("NonExistent"));
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

    [TestMethod]
    public void GetGlobalClass_ResPathMatching_ResolvesByResPath()
    {
        var scriptContent = @"extends Node

func start_wave() -> void:
    pass

func can_afford(cost: int) -> bool:
    return true";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "game_manager",
            FullPath = "C:/project/src/autoload/game_manager.gd",
            ResPath = "res://src/autoload/game_manager.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "GameManager", Path = "res://src/autoload/game_manager.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var typeInfo = provider.GetGlobalClass("GameManager");

        Assert.IsNotNull(typeInfo);
        Assert.AreEqual("GameManager", typeInfo.Name);
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "start_wave"));
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "can_afford"));
    }

    [TestMethod]
    public void GetGlobalClass_NormalizedForwardSlashPaths_Resolves()
    {
        var scriptContent = @"extends Node

func do_something() -> void:
    pass";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "my_autoload",
            FullPath = "C:/Users/dev/project/src/autoload/my_autoload.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "MyAutoload", Path = "res://src/autoload/my_autoload.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var typeInfo = provider.GetGlobalClass("MyAutoload");

        Assert.IsNotNull(typeInfo);
        Assert.IsTrue(typeInfo.Members.Any(m => m.Name == "do_something"));
    }

    [TestMethod]
    public void GetMember_MethodWithDefaultParams_ReportsCorrectMinMaxArgs()
    {
        var scriptContent = @"extends Node

func get_data(data: Dictionary, extra: Array = []) -> Dictionary:
    return data";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "DialogueManager",
            FullPath = "/project/dialogue_manager.gd",
            ResPath = "res://dialogue_manager.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "DialogueManager", Path = "res://dialogue_manager.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var member = provider.GetMember("DialogueManager", "get_data");
        Assert.IsNotNull(member);
        Assert.AreEqual(GDRuntimeMemberKind.Method, member.Kind);
        Assert.AreEqual(1, member.MinArgs, "Method with 1 required + 1 default param should have MinArgs=1");
        Assert.AreEqual(2, member.MaxArgs, "Method with 2 total params should have MaxArgs=2");
    }

    [TestMethod]
    public void GetMember_MethodAllDefaultParams_MinArgsZero()
    {
        var scriptContent = @"extends Node

func configure(timeout: int = 30, retries: int = 3) -> void:
    pass";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "Settings",
            FullPath = "/project/settings.gd",
            ResPath = "res://settings.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Settings", Path = "res://settings.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        var member = provider.GetMember("Settings", "configure");
        Assert.IsNotNull(member);
        Assert.AreEqual(0, member.MinArgs, "Method with all default params should have MinArgs=0");
        Assert.AreEqual(2, member.MaxArgs, "Method with 2 total params should have MaxArgs=2");
    }

    [TestMethod]
    public void GetMember_ResPathMatching_FindsMembers()
    {
        var scriptContent = @"extends Node

var gold: int = 100

func can_afford(cost: int) -> bool:
    return gold >= cost

func spend_gold(amount: int) -> void:
    gold -= amount

signal gold_changed";

        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(scriptContent);

        var mockScriptInfo = new MockScriptInfo
        {
            TypeName = "game_manager",
            FullPath = "C:/project/src/autoload/game_manager.gd",
            ResPath = "res://src/autoload/game_manager.gd",
            Class = classDecl
        };

        var mockProvider = new MockScriptProvider(new[] { mockScriptInfo });

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "GameManager", Path = "res://src/autoload/game_manager.gd", Enabled = true }
        };
        var provider = new GDAutoloadsProvider(autoloads, mockProvider);

        Assert.IsNotNull(provider.GetMember("GameManager", "can_afford"));
        Assert.IsNotNull(provider.GetMember("GameManager", "spend_gold"));
        Assert.IsNotNull(provider.GetMember("GameManager", "gold"));
        Assert.IsNotNull(provider.GetMember("GameManager", "gold_changed"));
        Assert.IsNull(provider.GetMember("GameManager", "nonexistent"));
    }
}
