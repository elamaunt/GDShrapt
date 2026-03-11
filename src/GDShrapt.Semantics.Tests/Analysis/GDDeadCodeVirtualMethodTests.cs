using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests that dead code analysis correctly detects virtual method overrides
/// dynamically via TypesMap instead of hardcoded lists.
/// </summary>
[TestClass]
public class GDDeadCodeVirtualMethodTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_virtual_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            var filePath = Path.Combine(tempPath, fileName);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && dir != tempPath)
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, recursive: true); }
            catch { }
        }
    }

    private static GDDeadCodeReport RunDeadCodeAnalysis(string tempPath, Action<GDDeadCodeOptions>? configure = null)
    {
        using var project = GDProjectLoader.LoadProject(tempPath);
        project.BuildCallSiteRegistry();
        var projectModel = new GDProjectSemanticModel(project);
        var service = projectModel.DeadCode;

        var options = new GDDeadCodeOptions
        {
            IncludeFunctions = true,
            IncludeVariables = false,
            IncludeSignals = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeUnreachable = false,
            IncludePrivate = true,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    [TestMethod]
    public void Debug_GodotTypesProvider_IsVirtualMethod()
    {
        var provider = new GDGodotTypesProvider();

        // _physics_process is virtual on Node
        var member = provider.GetMember("Node", "_physics_process");
        member.Should().NotBeNull("_physics_process should exist on Node");
        member!.IsVirtual.Should().BeTrue("_physics_process should be virtual on Node");

        // Should work via inheritance (CharacterBody2D → ... → Node)
        provider.IsVirtualMethod("Node", "_physics_process").Should().BeTrue();
        provider.IsVirtualMethod("CharacterBody2D", "_physics_process").Should().BeTrue();

        // Control virtual methods
        provider.IsVirtualMethod("Control", "_gui_input").Should().BeTrue();
        provider.IsVirtualMethod("Control", "_get_minimum_size").Should().BeTrue();

        // Non-virtual method
        provider.IsVirtualMethod("Node", "add_child").Should().BeFalse();
    }

    [TestMethod]
    public void VirtualMethod_InheritedFromBuiltinType_NotDeadCode()
    {
        var code = @"extends Control

func _gui_input(event: InputEvent) -> void:
    pass

func _get_minimum_size() -> Vector2:
    return Vector2.ZERO
";
        var tempPath = CreateTempProject(("control_child.gd", code));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var deadFunctions = report.Items
                .Where(i => i.Kind == GDDeadCodeKind.Function)
                .Select(i => i.Name)
                .ToList();

            deadFunctions.Should().NotContain("_gui_input");
            deadFunctions.Should().NotContain("_get_minimum_size");
            report.VirtualMethodsSkipped.Should().BeGreaterThan(0);
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void VirtualMethod_NotInOldHardcodedList_StillDetected()
    {
        var code = @"extends ResourceFormatLoader

func _get_recognized_extensions() -> PackedStringArray:
    return PackedStringArray()

func _load(path: String, original_path: String, use_sub_threads: bool, cache_mode: int) -> Variant:
    return null
";
        var tempPath = CreateTempProject(("loader.gd", code));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var deadFunctions = report.Items
                .Where(i => i.Kind == GDDeadCodeKind.Function)
                .Select(i => i.Name)
                .ToList();

            deadFunctions.Should().NotContain("_get_recognized_extensions");
            deadFunctions.Should().NotContain("_load");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void NonVirtualMethod_WithUnderscorePrefix_StillDeadCode()
    {
        var code = @"extends Node

func _my_custom_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("custom.gd", code));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var deadFunctions = report.Items
                .Where(i => i.Kind == GDDeadCodeKind.Function)
                .Select(i => i.Name)
                .ToList();

            deadFunctions.Should().Contain("_my_custom_method");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void VirtualMethod_DeeperInheritanceChain_NotDeadCode()
    {
        var code = @"extends CharacterBody2D

func _physics_process(delta: float) -> void:
    pass
";
        var tempPath = CreateTempProject(("player.gd", code));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var deadFunctions = report.Items
                .Where(i => i.Kind == GDDeadCodeKind.Function)
                .Select(i => i.Name)
                .ToList();

            deadFunctions.Should().NotContain("_physics_process");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void RegularUnusedFunction_StillDetected()
    {
        var code = @"extends Node

func unused_func() -> void:
    pass

func another_unused() -> int:
    return 42
";
        var tempPath = CreateTempProject(("unused.gd", code));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var deadFunctions = report.Items
                .Where(i => i.Kind == GDDeadCodeKind.Function)
                .Select(i => i.Name)
                .ToList();

            deadFunctions.Should().Contain("unused_func");
            deadFunctions.Should().Contain("another_unused");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
