using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDScriptProjectTests
{
    [TestMethod]
    public void CreateProject_WithScriptContent_ParsesCorrectly()
    {
        var scriptContent = @"
class_name Player
extends CharacterBody2D

var health: int = 100
var speed: float = 200.0

func _ready() -> void:
    pass

func take_damage(amount: int) -> void:
    health -= amount
";

        var project = new GDScriptProject(scriptContent);

        Assert.AreEqual(1, project.ScriptFiles.Count());

        var script = project.ScriptFiles.First();
        Assert.IsNotNull(script.Class);
        Assert.AreEqual("Player", script.TypeName);
        Assert.IsTrue(script.IsGlobal);
    }

    [TestMethod]
    public void CreateProject_MultipleScripts_AllParsed()
    {
        var baseEntity = @"
class_name BaseEntity
extends Node

var id: int = 0

func get_id() -> int:
    return id
";

        var player = @"
class_name Player
extends BaseEntity

var health: int = 100
";

        var project = new GDScriptProject(baseEntity, player);

        Assert.AreEqual(2, project.ScriptFiles.Count());
    }

    [TestMethod]
    public void GetScriptByTypeName_ExistingType_ReturnsScript()
    {
        var scriptContent = @"
class_name MyClass

func test() -> void:
    pass
";

        var project = new GDScriptProject(scriptContent);

        var script = project.GetScriptByTypeName("MyClass");

        Assert.IsNotNull(script);
        Assert.AreEqual("MyClass", script.TypeName);
    }

    [TestMethod]
    public void GetScriptByTypeName_NonExistingType_ReturnsNull()
    {
        var project = new GDScriptProject("extends Node");

        var script = project.GetScriptByTypeName("NonExistent");

        Assert.IsNull(script);
    }

    [TestMethod]
    public void AnalyzeAll_CollectsSymbols()
    {
        var scriptContent = @"
class_name TestScript

var my_var: int = 0
const MY_CONST = 42
signal my_signal
enum MyEnum { VALUE_A, VALUE_B }

func my_func() -> void:
    pass
";

        var project = new GDScriptProject(scriptContent);
        project.AnalyzeAll();

        var script = project.ScriptFiles.First();
        Assert.IsNotNull(script.SemanticModel);

        var symbols = script.SemanticModel.Symbols.ToList();
        Assert.IsTrue(symbols.Count > 0);
    }

    [TestMethod]
    public void CreateTypeResolver_ReturnsWorkingResolver()
    {
        var scriptContent = @"
class_name MyType
extends Node

var value: int = 0
";

        var project = new GDScriptProject(scriptContent);
        var resolver = project.CreateTypeResolver();

        Assert.IsNotNull(resolver);
        Assert.IsNotNull(resolver.RuntimeProvider);

        // Check that Node type is known (from Godot types)
        var typeInfo = resolver.GetTypeInfo("Node");
        Assert.IsNotNull(typeInfo);
    }

    [TestMethod]
    public void FindStaticDeclarationIdentifier_ExistingClass_ReturnsPointer()
    {
        var scriptContent = @"
class_name MyClass

func test() -> void:
    pass
";

        var project = new GDScriptProject(scriptContent);

        var pointer = project.FindStaticDeclarationIdentifier("MyClass");

        Assert.IsNotNull(pointer);
        Assert.IsNotNull(pointer.DeclarationIdentifier);
    }

    [TestMethod]
    public void FindStaticDeclarationIdentifier_NonExisting_ReturnsNull()
    {
        var project = new GDScriptProject("extends Node");

        var pointer = project.FindStaticDeclarationIdentifier("NonExistent");

        Assert.IsNull(pointer);
    }

    [TestMethod]
    public void CreateTypeResolver_WithAutoloads_ResolvesAutoloadTypes()
    {
        var globalScript = @"
extends Node

var player_data: Dictionary = {}
signal game_started

func start_game() -> void:
    pass
";

        var mainScript = @"
extends Node

func _ready() -> void:
    Global.start_game()
";

        var project = new GDScriptProject(globalScript, mainScript);

        // Manually add autoload entry (in real project this comes from project.godot)
        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };
        var autoloadsProvider = new GDAutoloadsProvider(autoloads, project);

        var resolver = new GDTypeResolver(
            new GDGodotTypesProvider(),
            new GDProjectTypesProvider(project),
            autoloadsProvider);

        // Check that autoload is resolved as global class
        var globalInfo = resolver.RuntimeProvider.GetGlobalClass("Global");
        Assert.IsNotNull(globalInfo, "Global autoload should be resolved");
        Assert.AreEqual("Global", globalInfo.Name);
    }

    [TestMethod]
    public void CreateTypeResolver_AutoloadMembers_AreAccessible()
    {
        // This test uses mock objects for detailed member extraction testing.
        // For in-memory scripts, FullPath is null, so GDAutoloadsProvider can't find them.
        // See GDAutoloadsProviderTests.GetGlobalClass_WithScriptProvider_ExtractsMembers for full member test.

        var autoloads = new[]
        {
            new GDAutoloadEntry { Name = "Global", Path = "res://global.gd", Enabled = true }
        };

        // Without matching script provider, autoload returns basic Node type
        var autoloadsProvider = new GDAutoloadsProvider(autoloads);
        var globalInfo = autoloadsProvider.GetGlobalClass("Global");

        Assert.IsNotNull(globalInfo);
        Assert.AreEqual("Global", globalInfo.Name);
        Assert.AreEqual("Node", globalInfo.BaseType, "Without script provider, base type should default to Node");
    }
}
