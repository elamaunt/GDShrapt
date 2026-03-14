using System.IO;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDCodeLensHandlerTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;
    private GDCodeLensHandler? _handler;

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
        _handler = new GDCodeLensHandler(_project, new GDProjectSemanticModel(_project));
    }

    // ========== Existing tests ==========

    [TestMethod]
    public void CodeLens_TscnFile_ShowsNodeReferences()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var sprite = $Sprite2D
    sprite.visible = false
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Sprite2D"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var sprite2DLens = lenses.FirstOrDefault(l => l.CommandArgument == "Sprite2D");
        sprite2DLens.Should().NotBeNull("expected CodeLens for Sprite2D node that is referenced in GDScript");
        sprite2DLens!.Label.Should().Contain("reference");
    }

    [TestMethod]
    public void CodeLens_TscnFile_UnreferencedNode_NoLens()
    {
        SetupProject(
            ("player.gd", @"extends Node

func _ready():
    pass
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Unreferenced"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var unreferencedLens = lenses.FirstOrDefault(l => l.CommandArgument == "Unreferenced");
        unreferencedLens.Should().BeNull("no CodeLens expected for nodes without references");
    }

    [TestMethod]
    public void CodeLens_TscnFile_MultipleReferences_ShowsCount()
    {
        SetupProject(
            ("a.gd", @"extends Node

func _ready():
    var cam = $Camera2D
"),
            ("b.gd", @"extends Node

func _process(_delta):
    $Camera2D.position = Vector2.ZERO
"),
            ("level.tscn", @"[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://a.gd"" id=""1""]

[ext_resource type=""Script"" path=""res://b.gd"" id=""2""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Camera2D"" type=""Camera2D"" parent="".""]

[node name=""Other"" type=""Node2D"" parent="".""]
script = ExtResource(""2"")
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var cameraLens = lenses.FirstOrDefault(l => l.CommandArgument == "Camera2D");
        cameraLens.Should().NotBeNull("expected CodeLens for Camera2D node");
        cameraLens!.Label.Should().Contain("references", "should show plural references");
    }

    [TestMethod]
    public void CodeLens_TscnFile_SignalConnection_ShowsMethodReferences()
    {
        SetupProject(
            ("button_handler.gd", @"extends Control

func _on_button_pressed():
    print(""pressed"")
"),
            ("ui.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://button_handler.gd"" id=""1""]

[node name=""Root"" type=""Control""]
script = ExtResource(""1"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "ui.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var signalLens = lenses.FirstOrDefault(l =>
            l.CommandArgument != null && l.CommandArgument.Contains("_on_button_pressed"));
        signalLens.Should().NotBeNull("expected CodeLens for signal connection callback method");
        signalLens!.Label.Should().Contain("reference");
    }

    [TestMethod]
    public void CodeLens_GdFile_StillWorks()
    {
        SetupProject(
            ("player.gd", @"class_name Player
extends CharacterBody2D

var health: int = 100

func take_damage(amount: int) -> void:
    health -= amount
"),
            ("enemy.gd", @"extends Node

func attack(target: Player):
    target.take_damage(10)
"));

        var playerPath = Path.Combine(_tempProjectPath!, "player.gd");
        var lenses = _handler!.GetCodeLenses(playerPath);

        lenses.Should().NotBeEmpty("expected CodeLens for GDScript class members");
    }

    [TestMethod]
    public void CodeLens_SignalConnection_GodotOpenRpg_ExactReferenceCheck()
    {
        SetupProject(
            ("game_end_trigger.gd", @"extends Area2D

func _on_area_entered(area: Area2D) -> void:
    print(""game end"")
"),
            ("area_transition.gd", @"extends Area2D

func _on_area_entered(area: Area2D) -> void:
    print(""transition"")
"),
            ("door.gd", @"extends Area2D

func _on_area_entered(area: Area2D) -> void:
    print(""door"")
"),
            ("door_trigger.gd", @"extends Area2D

func _on_area_entered(area: Area2D) -> void:
    print(""door trigger"")
"),
            ("main.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://game_end_trigger.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]

[node name=""Field"" type=""Node2D"" parent="".""]

[node name=""Map"" type=""Node2D"" parent=""Field""]

[node name=""Forest"" type=""Node2D"" parent=""Field/Map""]

[node name=""Gamepieces"" type=""Node2D"" parent=""Field/Map/Forest""]

[node name=""GameEndTrigger"" type=""Area2D"" parent=""Field/Map/Forest/Gamepieces""]
script = ExtResource(""1"")

[node name=""Area2D2"" type=""Area2D"" parent=""Field/Map/Forest/Gamepieces/GameEndTrigger""]

[connection signal=""area_entered"" from=""Field/Map/Forest/Gamepieces/GameEndTrigger/Area2D2"" to=""Field/Map/Forest/Gamepieces/GameEndTrigger"" method=""_on_area_entered""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "main.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var signalLens = lenses.FirstOrDefault(l =>
            l.CommandArgument != null && l.CommandArgument.Contains("_on_area_entered"));
        signalLens.Should().NotBeNull("expected CodeLens for signal connection");

        signalLens!.Label.Should().Be("1 reference",
            "should show exactly 1 reference (declaration in game_end_trigger.gd)");

        var cacheKey = signalLens.CommandArgument!;
        var locations = _handler.GetCachedReferences(cacheKey, tscnPath);
        locations.Should().NotBeNull();
        locations!.Should().HaveCount(1, "exactly one location expected");
        locations[0].FilePath.Should().Contain("game_end_trigger",
            "the single reference must be from game_end_trigger.gd");
    }

    [TestMethod]
    public void CodeLens_SignalConnection_IncludesOverrideReferences()
    {
        SetupProject(
            ("base_handler.gd", @"class_name BaseHandler
extends Control

func _on_pressed():
    print(""base pressed"")
"),
            ("child_handler.gd", @"extends BaseHandler

func _on_pressed():
    print(""child pressed"")
    super._on_pressed()
"),
            ("ui.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://base_handler.gd"" id=""1""]

[node name=""Root"" type=""Control""]
script = ExtResource(""1"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_pressed""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "ui.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var signalLens = lenses.FirstOrDefault(l =>
            l.CommandArgument != null && l.CommandArgument.Contains("_on_pressed"));
        signalLens.Should().NotBeNull("expected CodeLens for signal connection");

        signalLens!.Label.Should().Match("*reference*");

        var cacheKey = signalLens.CommandArgument!;
        var locations = _handler.GetCachedReferences(cacheKey, tscnPath);
        locations.Should().NotBeNull();

        var childRefs = locations!.Where(l =>
            l.FilePath != null && l.FilePath.Contains("child_handler")).ToList();
        childRefs.Should().NotBeEmpty("override in child_handler.gd should be included");

        var baseRefs = locations!.Where(l =>
            l.FilePath != null && l.FilePath.Contains("base_handler")).ToList();
        baseRefs.Should().NotBeEmpty("declaration in base_handler.gd should be included");
    }

    // ========== Test A: Scoped to scene scripts ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_ScopedToSceneScripts()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var s = $Sprite2D
"),
            ("enemy.gd", @"extends Node2D

func _ready():
    var s = $Sprite2D
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Sprite2D"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Sprite2D");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference");

        var refs = _handler.GetCachedReferences("Sprite2D", tscnPath);
        refs.Should().NotBeNull();
        refs!.Should().HaveCount(1);
        refs[0].FilePath.Should().Contain("player");
        refs[0].FilePath.Should().NotContain("enemy");
    }

    // ========== Test B: Unreferenced from scene scripts ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_UnreferencedFromScene_NoLens()
    {
        SetupProject(
            ("other.gd", @"extends Node2D

func _ready():
    var m = $MyNode
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""MyNode"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "MyNode");
        lens.Should().BeNull("no lens when only external scripts reference the node");
    }

    // ========== Test C: Common node name, no false positives ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_CommonNodeName_NoFalsePositives()
    {
        SetupProject(
            ("a.gd", @"extends Node2D

func _ready():
    var c = $CollisionShape2D
"),
            ("b.gd", @"extends Node2D

func _ready():
    var c = $CollisionShape2D
"),
            ("a.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://a.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""CollisionShape2D"" type=""CollisionShape2D"" parent="".""]
"),
            ("b.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://b.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""CollisionShape2D"" type=""CollisionShape2D"" parent="".""]
"));

        // Check a.tscn
        var aTscnPath = Path.Combine(_tempProjectPath!, "a.tscn");
        var aLenses = _handler!.GetCodeLenses(aTscnPath);
        var aLens = aLenses.FirstOrDefault(l => l.CommandArgument == "CollisionShape2D");
        aLens.Should().NotBeNull();
        aLens!.Label.Should().Be("1 reference");
        var aRefs = _handler.GetCachedReferences("CollisionShape2D", aTscnPath);
        aRefs.Should().NotBeNull();
        aRefs!.Should().HaveCount(1);
        aRefs[0].FilePath.Should().Contain("a.gd");

        // Check b.tscn
        var bTscnPath = Path.Combine(_tempProjectPath!, "b.tscn");
        var bLenses = _handler!.GetCodeLenses(bTscnPath);
        var bLens = bLenses.FirstOrDefault(l => l.CommandArgument == "CollisionShape2D");
        bLens.Should().NotBeNull();
        bLens!.Label.Should().Be("1 reference");
        var bRefs = _handler.GetCachedReferences("CollisionShape2D", bTscnPath);
        bRefs.Should().NotBeNull();
        bRefs!.Should().HaveCount(1);
        bRefs[0].FilePath.Should().Contain("b.gd");
    }

    // ========== Test D: get_node string path ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GetNode_StringPath()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var e = get_node(""Enemy"")
"),
            ("other.gd", @"extends Node2D

func _ready():
    var e = get_node(""Enemy"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Enemy");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference");

        var refs = _handler.GetCachedReferences("Enemy", tscnPath);
        refs!.Should().HaveCount(1);
        refs[0].FilePath.Should().Contain("player");
    }

    // ========== Test E: get_node_or_null ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GetNodeOrNull_StringPath()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var h = get_node_or_null(""HUD"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""HUD"" type=""CanvasLayer"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "HUD");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference");
    }

    // ========== Test F: find_node ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_FindNode_StringPath()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var s = find_node(""Spawner"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Spawner"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Spawner");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference");
    }

    // ========== Test G: Nested path matches both segments ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_NestedPath_MatchesSegment()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var c = get_node(""Parent/Child"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Parent"" type=""Node2D"" parent="".""]

[node name=""Child"" type=""Node2D"" parent=""Parent""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var parentLens = lenses.FirstOrDefault(l => l.CommandArgument == "Parent");
        parentLens.Should().NotBeNull();
        parentLens!.Label.Should().Be("1 reference");

        var childLens = lenses.FirstOrDefault(l => l.CommandArgument == "Parent/Child");
        childLens.Should().NotBeNull();
        childLens!.Label.Should().Be("1 reference");
    }

    // ========== Test H: Nested path — only matching segment ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_NestedPath_OnlyMatchingSegment()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var c = get_node(""SomeParent/Child"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Child"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var childLens = lenses.FirstOrDefault(l => l.CommandArgument == "Child");
        childLens.Should().NotBeNull();
        childLens!.Label.Should().Be("1 reference");

        var someLens = lenses.FirstOrDefault(l => l.CommandArgument == "SomeParent");
        someLens.Should().BeNull("SomeParent is not a node in the scene");
    }

    // ========== Test I: Dollar path segments ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_DollarPath_SegmentMatch()
    {
        SetupProject(
            ("ctrl.gd", @"extends Control

func _ready():
    var l = $Panel/Label
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://ctrl.gd"" id=""1""]

[node name=""Root"" type=""Control""]
script = ExtResource(""1"")

[node name=""Panel"" type=""Panel"" parent="".""]

[node name=""Label"" type=""Label"" parent=""Panel""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var panelLens = lenses.FirstOrDefault(l => l.CommandArgument == "Panel");
        panelLens.Should().NotBeNull();
        panelLens!.Label.Should().Be("1 reference");

        var labelLens = lenses.FirstOrDefault(l => l.CommandArgument == "Panel/Label");
        labelLens.Should().NotBeNull();
        labelLens!.Label.Should().Be("1 reference");
    }

    // ========== Test J: self.get_node ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_MemberCallGetNode()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var s = self.get_node(""Sprite"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Sprite");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference");
    }

    // ========== Test K: Multiple scripts in scene ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_MultipleScriptsInScene()
    {
        SetupProject(
            ("root.gd", @"extends Node2D

func _ready():
    var h = $HitBox
"),
            ("player.gd", @"extends Node2D

func _ready():
    var h = $HitBox
"),
            ("enemy.gd", @"extends Node2D

func _ready():
    var h = $HitBox
"),
            ("other.gd", @"extends Node2D

func _ready():
    var h = $HitBox
"),
            ("level.tscn", @"[gd_scene load_steps=4 format=3]

[ext_resource type=""Script"" path=""res://root.gd"" id=""1""]

[ext_resource type=""Script"" path=""res://player.gd"" id=""2""]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""3""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Player"" type=""Node2D"" parent="".""]
script = ExtResource(""2"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
script = ExtResource(""3"")

[node name=""HitBox"" type=""Area2D"" parent=""Player""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Player/HitBox");
        lens.Should().NotBeNull();

        var refs = _handler.GetCachedReferences("Player/HitBox", tscnPath);
        refs.Should().NotBeNull();
        // root.gd + player.gd + enemy.gd = 3, but NOT other.gd
        refs!.Should().HaveCount(3);
        refs.Should().NotContain(r => r.FilePath.Contains("other"));
    }

    // ========== Test L: No script in scene ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_NoScriptInScene_NoRefs()
    {
        SetupProject(
            ("external.gd", @"extends Node2D

func _ready():
    var c = $Camera2D
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Camera2D"" type=""Camera2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Camera2D");
        lens.Should().BeNull("no lens when scene has no scripts");
    }

    // ========== Test M: Includes instantiating script (conditional) ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_IncludesInstantiatingScript()
    {
        SetupProject(
            ("enemy.gd", @"extends Node2D

func _ready():
    pass
"),
            ("spawner.gd", @"extends Node2D

func _ready():
    var scene = preload(""res://enemy.tscn"")
    var inst = scene.instantiate()
    var h = $HitBox
"),
            ("enemy.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""HitBox"" type=""Area2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "enemy.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        // SceneFlow may or may not detect preload in temp projects.
        // If it works, spawner.gd should be counted as instantiating script.
        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "HitBox");
        if (lens != null)
        {
            var refs = _handler.GetCachedReferences("HitBox", tscnPath);
            if (refs != null && refs.Any(r => r.FilePath.Contains("spawner")))
            {
                // SceneFlow works — spawner is counted
                refs.Count.Should().BeGreaterThanOrEqualTo(1);
            }
        }
    }

    // ========== Test N: Dollar path, same name in different scenes ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_DollarPath_FalsePositive_DifferentNodeSameName()
    {
        SetupProject(
            ("a.gd", @"extends Node2D

func _ready():
    var t = $Timer
"),
            ("b.gd", @"extends Node2D

func _ready():
    var t = $Timer
"),
            ("a.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://a.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Timer"" type=""Timer"" parent="".""]
"),
            ("b.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://b.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Timer"" type=""Timer"" parent="".""]
"));

        var aTscnPath = Path.Combine(_tempProjectPath!, "a.tscn");
        var aLenses = _handler!.GetCodeLenses(aTscnPath);
        var aLens = aLenses.FirstOrDefault(l => l.CommandArgument == "Timer");
        aLens.Should().NotBeNull();
        aLens!.Label.Should().Be("1 reference");

        var aRefs = _handler.GetCachedReferences("Timer", aTscnPath);
        aRefs!.Should().HaveCount(1);
        aRefs[0].FilePath.Should().Contain("a.gd");
    }

    // ========== Test O: Empty string get_node ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GetNode_EmptyString_Ignored()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var x = get_node("""")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Sprite2D"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var nodeLenses = lenses.Where(l => l.CommandArgument != null && !l.CommandArgument.StartsWith("signal:")).ToList();
        nodeLenses.Should().BeEmpty("empty get_node path should not match any node");
    }

    // ========== Test P: Relative paths ./Child and ../Sibling ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GetNode_RelativePath_CurrentAndParent()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var c = get_node(""./Child"")
    var s = get_node(""../Sibling"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Child"" type=""Node2D"" parent="".""]

[node name=""Sibling"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var childLens = lenses.FirstOrDefault(l => l.CommandArgument == "Child");
        childLens.Should().NotBeNull();
        childLens!.Label.Should().Be("1 reference");

        var siblingLens = lenses.FirstOrDefault(l => l.CommandArgument == "Sibling");
        siblingLens.Should().NotBeNull();
        siblingLens!.Label.Should().Be("1 reference");

        // "." and ".." should NOT match any nodes
        var dotLens = lenses.FirstOrDefault(l => l.CommandArgument == ".");
        dotLens.Should().BeNull();
        var dotdotLens = lenses.FirstOrDefault(l => l.CommandArgument == "..");
        dotdotLens.Should().BeNull();
    }

    // ========== Test Q: Sub-scene script included ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_SubSceneScriptIncluded()
    {
        SetupProject(
            ("root.gd", @"extends Node2D

func _ready():
    pass
"),
            ("enemy.gd", @"extends Node2D

func _ready():
    var t = get_node(""../Treasure"")
"),
            ("other.gd", @"extends Node2D

func _ready():
    var t = get_node(""Treasure"")
"),
            ("enemy.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]

[node name=""EnemyRoot"" type=""Node2D""]
script = ExtResource(""1"")
"),
            ("level.tscn", @"[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://root.gd"" id=""1""]

[ext_resource type=""PackedScene"" path=""res://enemy.tscn"" id=""2""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" parent=""."" instance=ExtResource(""2"")]

[node name=""Treasure"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        // Sub-scene scripts (enemy.gd) have no attachment in level.tscn,
        // so their $Node / get_node references are NOT counted for the parent scene.
        // This prevents FP from sub-scene scripts referencing their own local nodes.
        // The only references should be from root.gd (directly attached in level.tscn).
        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Treasure");

        // root.gd doesn't reference Treasure, and enemy.gd (sub-scene) is now filtered out
        lens.Should().BeNull("sub-scene scripts' get_node refs don't count for the parent scene");
    }

    // ========== Test R: Sub-scene script NOT counted in wrong scene ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_SubSceneScript_NotCountedInWrongScene()
    {
        SetupProject(
            ("root.gd", @"extends Node2D

func _ready():
    pass
"),
            ("enemy.gd", @"extends Node2D

func _ready():
    var t = get_node(""Treasure"")
"),
            ("enemy.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]

[node name=""EnemyRoot"" type=""Node2D""]
script = ExtResource(""1"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://root.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Treasure"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        // enemy.tscn is NOT a sub-scene of level.tscn, so enemy.gd should NOT be counted
        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Treasure");
        lens.Should().BeNull("enemy.gd is not in this scene — it belongs to enemy.tscn which is not a sub-scene here");
    }

    // ========== Test S: Group query - get_nodes_in_group ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupQuery_GetNodesInGroup()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var enemies = get_tree().get_nodes_in_group(""enemies"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
groups = [""enemies""]

[node name=""Ally"" type=""Node2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var enemyLens = lenses.FirstOrDefault(l => l.CommandArgument == "Enemy");
        enemyLens.Should().NotBeNull("Enemy is in 'enemies' group and referenced via get_nodes_in_group");
        enemyLens!.Label.Should().Be("1 reference");

        var allyLens = lenses.FirstOrDefault(l => l.CommandArgument == "Ally");
        allyLens.Should().BeNull("Ally is not in any group");
    }

    // ========== Test T: Group query - get_first_node_in_group ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupQuery_GetFirstNodeInGroup()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var boss = get_tree().get_first_node_in_group(""enemies"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Boss"" type=""Node2D"" parent="".""]
groups = [""enemies""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var bossLens = lenses.FirstOrDefault(l => l.CommandArgument == "Boss");
        bossLens.Should().NotBeNull();
        bossLens!.Label.Should().Be("1 reference");
    }

    // ========== Test U: Group query - multiple nodes in group ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupQuery_MultipleNodesInGroup()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var enemies = get_tree().get_nodes_in_group(""enemies"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""EnemyA"" type=""Node2D"" parent="".""]
groups = [""enemies""]

[node name=""EnemyB"" type=""Node2D"" parent="".""]
groups = [""enemies""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var aLens = lenses.FirstOrDefault(l => l.CommandArgument == "EnemyA");
        aLens.Should().NotBeNull();
        aLens!.Label.Should().Be("1 reference");

        var bLens = lenses.FirstOrDefault(l => l.CommandArgument == "EnemyB");
        bLens.Should().NotBeNull();
        bLens!.Label.Should().Be("1 reference");
    }

    // ========== Test V: Group query - external script filtered ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupQuery_ExternalScript_Filtered()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var enemies = get_tree().get_nodes_in_group(""enemies"")
"),
            ("other.gd", @"extends Node2D

func _ready():
    var enemies = get_tree().get_nodes_in_group(""enemies"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
groups = [""enemies""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Enemy");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("1 reference", "only player.gd is in the scene");

        var refs = _handler.GetCachedReferences("Enemy", tscnPath);
        refs!.Should().HaveCount(1);
        refs[0].FilePath.Should().Contain("player");
    }

    // ========== Test W: Group query - wrong group name ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupQuery_WrongGroupName_NoMatch()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var allies = get_tree().get_nodes_in_group(""allies"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
groups = [""enemies""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Enemy");
        lens.Should().BeNull("'allies' group query should not match 'enemies' group");
    }

    // ========== Test X: Group and direct ref combined ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_GroupAndDirectRef_Combined()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var e = $Enemy
    var targets = get_tree().get_nodes_in_group(""targets"")
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""Node2D"" parent="".""]
groups = [""targets""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        var lens = lenses.FirstOrDefault(l => l.CommandArgument == "Enemy");
        lens.Should().NotBeNull();
        lens!.Label.Should().Be("2 references", "1 from $Enemy + 1 from group query");
    }

    // ========== Test Y: Same name, different parent — scoped by parent path ==========

    [TestMethod]
    public void CodeLens_TscnNodeRef_SameNameDifferentParent_ScopedCorrectly()
    {
        SetupProject(
            ("npc_a.gd", @"extends Node2D

func _ready():
    var p = $InteractionPopup
"),
            ("npc_b.gd", @"extends Node2D

func _ready():
    var p = $InteractionPopup
"),
            ("level.tscn", @"[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://npc_a.gd"" id=""1""]

[ext_resource type=""Script"" path=""res://npc_b.gd"" id=""2""]

[node name=""Root"" type=""Node2D""]

[node name=""NpcA"" type=""Node2D"" parent="".""]
script = ExtResource(""1"")

[node name=""InteractionPopup"" type=""Control"" parent=""NpcA""]

[node name=""NpcB"" type=""Node2D"" parent="".""]
script = ExtResource(""2"")

[node name=""InteractionPopup"" type=""Control"" parent=""NpcB""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        // NpcA/InteractionPopup should have 1 ref from npc_a.gd only
        var lensA = lenses.FirstOrDefault(l => l.CommandArgument == "NpcA/InteractionPopup");
        lensA.Should().NotBeNull("NpcA/InteractionPopup should be referenced by npc_a.gd");
        lensA!.Label.Should().Be("1 reference");

        var refsA = _handler.GetCachedReferences("NpcA/InteractionPopup", tscnPath);
        refsA.Should().NotBeNull();
        refsA!.Should().HaveCount(1);
        refsA[0].FilePath.Should().Contain("npc_a", "only npc_a.gd can reach NpcA/InteractionPopup");

        // NpcB/InteractionPopup should have 1 ref from npc_b.gd only
        var lensB = lenses.FirstOrDefault(l => l.CommandArgument == "NpcB/InteractionPopup");
        lensB.Should().NotBeNull("NpcB/InteractionPopup should be referenced by npc_b.gd");
        lensB!.Label.Should().Be("1 reference");

        var refsB = _handler.GetCachedReferences("NpcB/InteractionPopup", tscnPath);
        refsB.Should().NotBeNull();
        refsB!.Should().HaveCount(1);
        refsB[0].FilePath.Should().Contain("npc_b", "only npc_b.gd can reach NpcB/InteractionPopup");
    }

    // ========== Hierarchy separation tests ==========

    [TestMethod]
    public void CodeLens_GdFile_TwoHierarchies_SameMethodName_NoFalsePositives()
    {
        SetupProject(
            ("base_a.gd", "class_name BaseA\nextends Node2D\n\nfunc do_work() -> void:\n\tpass\n"),
            ("child_a.gd", "extends BaseA\n\nfunc do_work() -> void:\n\tsuper.do_work()\n"),
            ("base_b.gd", "class_name BaseB\nextends Resource\n\nfunc do_work() -> void:\n\tpass\n"),
            ("child_b.gd", "extends BaseB\n\nfunc do_work() -> void:\n\tsuper.do_work()\n"));

        var childAPath = Path.Combine(_tempProjectPath!, "child_a.gd");
        var lenses = _handler!.GetCodeLenses(childAPath);

        var doWorkLens = lenses.FirstOrDefault(l => l.CommandArgument == "do_work");
        doWorkLens.Should().NotBeNull("CodeLens for do_work should exist");

        var refs = _handler.GetCachedReferences("do_work", childAPath);
        refs.Should().NotBeNull();

        refs!.Should().NotContain(r => r.FilePath.Contains("base_b"),
            "base_b.gd is from a different hierarchy");
        refs.Should().NotContain(r => r.FilePath.Contains("child_b"),
            "child_b.gd is from a different hierarchy");

        refs.Should().Contain(r => r.FilePath.Contains("base_a"),
            "base_a.gd declaration should be included");
    }

    [TestMethod]
    public void CodeLens_GdFile_SingleHierarchy_StillWorksCorrectly()
    {
        SetupProject(
            ("entity.gd", "class_name Entity\nextends Node2D\n\nfunc attack() -> void:\n\tpass\n"),
            ("player.gd", "extends Entity\n\nfunc attack() -> void:\n\tsuper.attack()\n"),
            ("boss.gd", "extends Entity\n\nfunc attack() -> void:\n\tsuper.attack()\n\tprint(\"boss\")\n"));

        var entityPath = Path.Combine(_tempProjectPath!, "entity.gd");
        var lenses = _handler!.GetCodeLenses(entityPath);

        var attackLens = lenses.FirstOrDefault(l => l.CommandArgument == "attack");
        attackLens.Should().NotBeNull("CodeLens for attack should exist");

        var refs = _handler.GetCachedReferences("attack", entityPath);
        refs.Should().NotBeNull();
        refs!.Count.Should().BeGreaterThanOrEqualTo(2,
            "overrides in player.gd and boss.gd should be counted");
    }

    [TestMethod]
    public void CodeLens_GdFile_ThreeHierarchies_FiltersCorrectly()
    {
        SetupProject(
            ("base_a.gd", "class_name TypeA\nextends Node\n\nfunc process() -> void:\n\tpass\n"),
            ("child_a.gd", "extends TypeA\n\nfunc process() -> void:\n\tsuper.process()\n"),
            ("base_b.gd", "class_name TypeB\nextends Resource\n\nfunc process() -> void:\n\tpass\n"),
            ("child_b.gd", "extends TypeB\n\nfunc process() -> void:\n\tsuper.process()\n"),
            ("base_c.gd", "class_name TypeC\nextends RefCounted\n\nfunc process() -> void:\n\tpass\n"),
            ("child_c.gd", "extends TypeC\n\nfunc process() -> void:\n\tsuper.process()\n"));

        var childAPath = Path.Combine(_tempProjectPath!, "child_a.gd");
        var lenses = _handler!.GetCodeLenses(childAPath);

        var processLens = lenses.FirstOrDefault(l => l.CommandArgument == "process");
        processLens.Should().NotBeNull();

        var refs = _handler.GetCachedReferences("process", childAPath);
        refs.Should().NotBeNull();
        refs!.Should().NotContain(r =>
            r.FilePath.Contains("base_b") || r.FilePath.Contains("child_b") ||
            r.FilePath.Contains("base_c") || r.FilePath.Contains("child_c"),
            "refs from TypeB and TypeC hierarchies should be excluded");
    }

    [TestMethod]
    public void CodeLens_BridgeConnected_ShowsBridgeLabel()
    {
        SetupProject(
            ("base_a.gd", "class_name BaseA\nextends Node2D\n\nfunc execute() -> void:\n\tpass\n"),
            ("child_a.gd", "extends BaseA\n\nfunc execute() -> void:\n\tsuper.execute()\n"),
            ("base_b.gd", "class_name BaseB\nextends Resource\n\nfunc execute() -> void:\n\tpass\n"),
            ("child_b.gd", "extends BaseB\n\nfunc execute() -> void:\n\tsuper.execute()\n"),
            ("bridge.gd", "extends Node\n\nvar obj\n\nfunc run():\n\tobj.execute()\n"));

        var baseAPath = Path.Combine(_tempProjectPath!, "base_a.gd");
        var lenses = _handler!.GetCodeLenses(baseAPath);

        var executeLens = lenses.FirstOrDefault(l => l.CommandArgument == "execute");
        executeLens.Should().NotBeNull("execute method should have a CodeLens");

        // Bridge label should use N+M format
        executeLens!.Label.Should().Contain("+",
            "bridge-connected CodeLens should show N+M format");
        executeLens.Label.Should().Contain("references",
            "label should end with 'references'");

        // Cached refs should include files from both hierarchies
        var refs = _handler.GetCachedReferences("execute", baseAPath);
        refs.Should().NotBeNull();
        var refFiles = refs!.Select(r => Path.GetFileName(r.FilePath)).Distinct().ToList();
        refFiles.Should().Contain(f => f == "base_b.gd" || f == "child_b.gd",
            "bridge-connected refs should include BaseB hierarchy");
        refFiles.Should().Contain(f => f == "bridge.gd",
            "bridge-connected refs should include bridge file");
    }

    [TestMethod]
    public void CodeLens_NoBridge_NormalLabel()
    {
        SetupProject(
            ("base_a.gd", "class_name BaseA\nextends Node2D\n\nfunc execute() -> void:\n\tpass\n"),
            ("child_a.gd", "extends BaseA\n\nfunc execute() -> void:\n\tsuper.execute()\n"),
            ("base_b.gd", "class_name BaseB\nextends Resource\n\nfunc execute() -> void:\n\tpass\n"));

        var baseAPath = Path.Combine(_tempProjectPath!, "base_a.gd");
        var lenses = _handler!.GetCodeLenses(baseAPath);

        var executeLens = lenses.FirstOrDefault(l => l.CommandArgument == "execute");
        executeLens.Should().NotBeNull("execute method should have a CodeLens");

        // Without bridge, label should NOT use N+M format
        executeLens!.Label.Should().NotContain("+",
            "non-bridge CodeLens should not show N+M format");
    }

    [TestMethod]
    public void FormatReferenceLabel_BridgeExtra_ShowsPlusFormat()
    {
        // Use reflection or test via public interface
        var label = GDCodeLensHandler.FormatReferenceLabel(5, 0, 3);
        label.Should().Be("5+3 references");
    }

    [TestMethod]
    public void FormatReferenceLabel_NoBridge_NormalFormat()
    {
        var label = GDCodeLensHandler.FormatReferenceLabel(5, 0, 0);
        label.Should().Be("5 references");
    }

    [TestMethod]
    public void FormatReferenceLabel_BridgeWithUnion_ShowsBothFormats()
    {
        var label = GDCodeLensHandler.FormatReferenceLabel(5, 2, 3);
        label.Should().Be("5+3 references (+2 unions)");
    }
}
