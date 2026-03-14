using System.IO;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDSceneDiagnosticsHandlerTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;

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
    }

    [TestMethod]
    public void SceneDiagnostics_MissingMethod_ReportsGD4013()
    {
        SetupProject(
            ("handler.gd", @"extends Control

func _on_valid_pressed():
    print(""ok"")
"),
            ("ui.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://handler.gd"" id=""1""]

[node name=""Root"" type=""Control""]
script = ExtResource(""1"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_missing_pressed""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "ui.tscn");
        var handler = new GDSceneDiagnosticsHandler(_project!);
        var diagnostics = handler.AnalyzeScene(tscnPath);

        var gd4013 = diagnostics.FirstOrDefault(d => d.Code == "GD4013");
        gd4013.Should().NotBeNull("expected GD4013 for missing method '_on_missing_pressed'");
        gd4013!.Message.Should().Contain("_on_missing_pressed");
        gd4013.Severity.Should().Be(GDUnifiedDiagnosticSeverity.Error);
    }

    [TestMethod]
    public void SceneDiagnostics_ExistingMethod_NoDiagnostic()
    {
        SetupProject(
            ("handler.gd", @"extends Control

func _on_button_pressed():
    print(""ok"")
"),
            ("ui.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://handler.gd"" id=""1""]

[node name=""Root"" type=""Control""]
script = ExtResource(""1"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "ui.tscn");
        var handler = new GDSceneDiagnosticsHandler(_project!);
        var diagnostics = handler.AnalyzeScene(tscnPath);

        var gd4013 = diagnostics.FirstOrDefault(d => d.Code == "GD4013");
        gd4013.Should().BeNull("no GD4013 expected when method exists");
    }

    [TestMethod]
    public void SceneDiagnostics_NoScript_NoDiagnostic()
    {
        SetupProject(
            ("level.tscn", @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""_on_button_pressed""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var handler = new GDSceneDiagnosticsHandler(_project!);
        var diagnostics = handler.AnalyzeScene(tscnPath);

        // No script attached to root — should not crash or produce diagnostic
        diagnostics.Where(d => d.Code == "GD4013").Should().BeEmpty();
    }

    [TestMethod]
    public void SceneDiagnostics_InheritedMethod_NoDiagnostic()
    {
        SetupProject(
            ("handler.gd", @"extends Node2D

func _ready():
    pass
"),
            ("level.tscn", @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://handler.gd"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Area"" type=""Area2D"" parent="".""]

[connection signal=""area_entered"" from=""Area"" to=""."" method=""queue_free""]
"));

        var tscnPath = Path.Combine(_tempProjectPath!, "level.tscn");
        var handler = new GDSceneDiagnosticsHandler(_project!);
        var diagnostics = handler.AnalyzeScene(tscnPath);

        // queue_free is inherited from Node — no diagnostic
        diagnostics.Where(d => d.Code == "GD4013").Should().BeEmpty();
    }
}
