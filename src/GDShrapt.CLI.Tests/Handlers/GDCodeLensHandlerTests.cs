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

    [TestMethod]
    public void CodeLens_TscnFile_ShowsNodeReferences()
    {
        SetupProject(
            ("player.gd", @"extends Node2D

func _ready():
    var sprite = $Sprite2D
    sprite.visible = false
"),
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Sprite2D"" type=""Sprite2D"" parent="".""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var lenses = _handler!.GetCodeLenses(tscnPath);

        // The "Sprite2D" node should have a CodeLens since it's referenced via $Sprite2D in player.gd
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

        // No CodeLens for unreferenced nodes
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
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Camera2D"" type=""Camera2D"" parent="".""]
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

        // Should have a CodeLens on the [connection] line for _on_button_pressed
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

        // Should have CodeLens for class members in .gd files (existing behavior)
        lenses.Should().NotBeEmpty("expected CodeLens for GDScript class members");
    }
}
