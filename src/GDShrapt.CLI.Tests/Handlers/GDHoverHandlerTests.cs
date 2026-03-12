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
}
