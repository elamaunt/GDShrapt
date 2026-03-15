using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDHoverHandlerTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;
    private GDHoverHandler? _handler;

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private void SetupProject(params (string name, string content)[] scripts)
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(scripts);
        _project = GDProjectLoader.LoadProject(_tempProjectPath);
        _handler = new GDHoverHandler(new GDProjectSemanticModel(_project));
    }

    [TestMethod]
    public void Hover_TscnFile_OnResPath_ReturnsScriptInfo()
    {
        SetupProject(
            ("player.gd", @"class_name Player
extends CharacterBody2D

func move():
    pass

func jump():
    pass
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""res://player.gd"")
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");

        // Find the line with "res://player.gd" — it's line 6
        var hover = _handler!.GetHover(tscnPath, 6, 30);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("player.gd");
    }

    [TestMethod]
    public void Hover_TscnFile_OutsideResPath_ReturnsNull()
    {
        SetupProject(
            ("player.gd", @"extends Node

func _ready():
    pass
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");

        // Hover on a line with no res:// path
        var hover = _handler!.GetHover(tscnPath, 3, 5);

        hover.Should().BeNull();
    }

    [TestMethod]
    public void Hover_TscnFile_OnScenePath_ReturnsSceneInfo()
    {
        SetupProject(
            ("main.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""PackedScene"" path=""res://sub.tscn"" id=""1""]

[node name=""Main"" type=""Node2D""]
"),
            ("sub.tscn", @"[gd_scene format=3]

[node name=""SubRoot"" type=""Control""]
"));

        var mainPath = Path.Combine(_tempProjectPath!, "main.tscn");

        // Hover on "res://sub.tscn" — line 3
        var hover = _handler!.GetHover(mainPath, 3, 45);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("sub.tscn");
    }

    [TestMethod]
    public void Hover_GdFile_StillWorks()
    {
        SetupProject(("test.gd", @"extends Node

var health: int = 100

func _ready():
    pass
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");

        // Hover on "health" at line 3, col ~5
        var hover = _handler!.GetHover(filePath, 3, 5);

        // Should return hover info for the variable (existing behavior)
        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("health");
    }

    [TestMethod]
    public void Hover_MemberAccess_KnownCallerType_ShowsMethodInfo()
    {
        SetupProject(
            ("inventory.gd", @"class_name Inventory
extends Resource

func add(item_type: String, amount: int) -> void:
    pass
"),
            ("main.gd", @"extends Node

func _ready():
    var inv: Inventory = Inventory.new()
    inv.add(""sword"", 1)
"));

        var filePath = Path.Combine(_tempProjectPath!, "main.gd");

        // Hover on "add" in "inv.add" — line 5, column at "add"
        // "    inv.add" → "add" starts at column 9 (1-based)
        var hover = _handler!.GetHover(filePath, 5, 9);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("func add", "should show method signature, not 'var add'");
        hover!.Content.Should().NotContain("Unknown", "type should be resolved");
    }

    [TestMethod]
    public void Hover_MemberAccess_BuiltinMember_HasHoverRange()
    {
        SetupProject(("test.gd", @"extends Node

func _ready():
    var n: Node = Node.new()
    n.get_name()
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");

        // Hover on "get_name" in "n.get_name()" — line 5
        // "    n.get_name()" → "get_name" starts at column 7 (1-based)
        var hover = _handler!.GetHover(filePath, 5, 7);

        hover.Should().NotBeNull();
        hover!.StartLine.Should().NotBeNull("built-in member hover should have a range");
        hover!.EndLine.Should().NotBeNull("built-in member hover should have a range");
        hover!.StartColumn.Should().NotBeNull("built-in member hover should have a range");
        hover!.EndColumn.Should().NotBeNull("built-in member hover should have a range");
    }

    [TestMethod]
    public void Hover_MemberAccess_InferredType_ShowsCorrectMember()
    {
        SetupProject(
            ("inventory.gd", @"class_name Inventory
extends Resource

static func restore() -> Inventory:
    return null

func add(item_type: String, amount: int) -> void:
    pass
"),
            ("user.gd", @"extends Node

func _ready():
    var inv: = Inventory.restore()
    inv.add(""x"", 1)
"));

        var filePath = Path.Combine(_tempProjectPath!, "user.gd");

        // Hover on "add" in "inv.add" — line 5
        // "    inv.add" → "add" starts at column 9 (1-based)
        var hover = _handler!.GetHover(filePath, 5, 9);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("func add", "inferred type should resolve member correctly");
    }

    [TestMethod]
    public void Hover_Signal_ShowsParameterSignature()
    {
        SetupProject(("test.gd", @"class_name TestClass
extends Node

signal item_changed(type: int, amount: int)
"));
        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        // Hover on "item_changed" at line 4, col ~8
        var hover = _handler!.GetHover(filePath, 4, 8);
        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("item_changed(type: int, amount: int)");
    }

    [TestMethod]
    public void Hover_ClassName_New_InSameFile_ShowsChildType_NotBaseClass()
    {
        SetupProject(
            ("inventory.gd", @"class_name Inventory
extends Resource

func save() -> void:
    pass

static func restore() -> Inventory:
    var new_inventory: = Inventory.new()
    new_inventory.save()
    return new_inventory
"));

        var filePath = Path.Combine(_tempProjectPath!, "inventory.gd");

        // Hover on "new_inventory" in "new_inventory.save()" (usage site, not declaration)
        // Line 9 (1-based): "    new_inventory.save()"
        // "new_inventory" starts at column 5 (after "    ")
        var hover = _handler!.GetHover(filePath, 9, 5);

        hover.Should().NotBeNull("hover on new_inventory should return content");
        hover!.Content.Should().Contain("Inventory",
            "inferred type should be Inventory, not Resource");
        hover!.Content.Should().NotContain("Unknown",
            "type should be resolved, not Unknown");
    }

    // ========================================
    // Bridge detection tests
    // ========================================

    [TestMethod]
    public void Hover_Bridge_BothTypesFlowIn_ShowsBridgeWithFilePaths()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func save() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func save() -> void:
    pass
"),
            ("bridge.gd", @"class_name Bridge
extends Node

func do_save(target):
    target.save()
"),
            ("caller.gd", @"extends Node

func _ready():
    var b: Bridge = Bridge.new()
    b.do_save(TypeA.new())
    b.do_save(TypeB.new())
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        // Hover on "save" declaration — line 4, col ~6
        var hover = _handler!.GetHover(filePath, 4, 6);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("bridge", "should show bridge info when both types flow in");
        hover!.Content.Should().Contain("TypeB", "should mention the other hierarchy type");
        hover!.Content.Should().Contain("files)", "should show file count");
    }

    [TestMethod]
    public void Hover_Bridge_OnlyOneTypeFlowsIn_NoBridge()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func save() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func save() -> void:
    pass
"),
            ("bridge.gd", @"class_name Bridge
extends Node

func do_save(target):
    target.save()
"),
            ("caller.gd", @"extends Node

func _ready():
    var b: Bridge = Bridge.new()
    b.do_save(TypeA.new())
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        // Should NOT show bridge since only TypeA flows into do_save
        if (hover != null)
        {
            hover.Content.Should().NotContain("bridge",
                "should not show bridge when only one type flows in via call sites");
        }
    }

    [TestMethod]
    public void Hover_Bridge_SameHierarchy_NoBridge()
    {
        SetupProject(
            ("base_type.gd", @"class_name BaseType
extends Node

func save() -> void:
    pass
"),
            ("child_type.gd", @"class_name ChildType
extends BaseType

func save() -> void:
    pass
"),
            ("user.gd", @"extends Node

func _ready():
    var b: BaseType = BaseType.new()
    b.save()
"));

        var filePath = Path.Combine(_tempProjectPath!, "base_type.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        if (hover != null)
        {
            hover.Content.Should().NotContain("bridge",
                "same inheritance hierarchy should not be a bridge");
        }
    }

    [TestMethod]
    public void Hover_Bridge_SingleType_NoBridge()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func unique_method() -> void:
    pass
"),
            ("user.gd", @"extends Node

func _ready():
    var a: TypeA = TypeA.new()
    a.unique_method()
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        if (hover != null)
        {
            hover.Content.Should().NotContain("bridge",
                "single type defining method should not be a bridge");
        }
    }

    [TestMethod]
    public void Hover_Bridge_ThreeHierarchies_AllFlowIn_ShowsBridge()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func process() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func process() -> void:
    pass
"),
            ("type_c.gd", @"class_name TypeC
extends RefCounted

func process() -> void:
    pass
"),
            ("bridge.gd", @"class_name Bridge
extends Node

func do_process(target):
    target.process()
"),
            ("caller.gd", @"extends Node

func _ready():
    var b: Bridge = Bridge.new()
    b.do_process(TypeA.new())
    b.do_process(TypeB.new())
    b.do_process(TypeC.new())
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("bridge", "should show bridge for 3 hierarchies");
        hover!.Content.Should().Contain("TypeB", "should mention TypeB");
        hover!.Content.Should().Contain("TypeC", "should mention TypeC");
    }

    [TestMethod]
    public void Hover_Bridge_Symmetry_HoverFromTypeBSide()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func save() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func save() -> void:
    pass
"),
            ("bridge.gd", @"class_name Bridge
extends Node

func do_save(target):
    target.save()
"),
            ("caller.gd", @"extends Node

func _ready():
    var b: Bridge = Bridge.new()
    b.do_save(TypeA.new())
    b.do_save(TypeB.new())
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_b.gd");

        // Hover on TypeB's save() declaration — line 4, col ~6
        var hover = _handler!.GetHover(filePath, 4, 6);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("bridge", "bridge should be symmetric");
        hover!.Content.Should().Contain("TypeA", "should mention TypeA from TypeB's perspective");
    }

    [TestMethod]
    public void Hover_Bridge_DeadCode_NoBridge()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func save() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func save() -> void:
    pass
"),
            ("bridge.gd", @"extends Node

func do_save(target):
    target.save()
"));
        // No caller file — do_save is never called

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        if (hover != null)
        {
            hover.Content.Should().NotContain("bridge",
                $"dead code (no call sites) should not produce bridge. Hover: {hover.Content}");
        }
    }

    [TestMethod]
    public void Hover_Bridge_FilePathLabelsShown()
    {
        SetupProject(
            ("type_a.gd", @"class_name TypeA
extends Node

func save() -> void:
    pass
"),
            ("type_b.gd", @"class_name TypeB
extends Resource

func save() -> void:
    pass
"),
            ("bridge.gd", @"class_name Bridge
extends Node

func do_save(target):
    target.save()
"),
            ("caller.gd", @"extends Node

func _ready():
    var b: Bridge = Bridge.new()
    b.do_save(TypeA.new())
    b.do_save(TypeB.new())
"));

        var filePath = Path.Combine(_tempProjectPath!, "type_a.gd");

        var hover = _handler!.GetHover(filePath, 4, 6);

        hover.Should().NotBeNull();
        hover!.Content.Should().Contain("bridge", "should show bridge info");
        hover!.Content.Should().Contain("type_b.gd", "should include file path for TypeB");
    }
}
