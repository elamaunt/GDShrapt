using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for reflection warnings in the rename service.
/// Verifies that renaming symbols matched by reflection patterns produces warnings.
/// </summary>
[TestClass]
public class ReflectionRenameWarningTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_reflrn_test_" + Guid.NewGuid().ToString("N"));
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

    // RRW-6: Rename method with reflection → has reflection warning
    [TestMethod]
    public void RRW6_Rename_MethodWithReflection_HasReflectionWarning()
    {
        var code = @"extends Node
class_name ReflRenameTest1

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_rename1.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("refl_rename1"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("test_a");

            var result = service.PlanRename(symbol!, "renamed_func");

            result.Warnings.Should().Contain(w =>
                w.Message.Contains("Reflection pattern") || w.Message.Contains("reflection"),
                "test_a is matched by a reflection pattern and renaming should warn");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RRW-7: Rename method NOT matching filter → no reflection warning
    [TestMethod]
    public void RRW7_Rename_MethodWithFilteredReflection_NoWarningForNonMatch()
    {
        var code = @"extends Node
class_name ReflRenameTest2

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func other_func() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_rename2.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("refl_rename2"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("other_func");

            var result = service.PlanRename(symbol!, "new_func");

            var reflectionWarnings = result.Warnings
                .Where(w => w.Message.Contains("Reflection pattern") || w.Message.Contains("reflection"))
                .ToList();

            reflectionWarnings.Should().BeEmpty("other_func does not match begins_with(\"test_\") filter");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RRW-8: Rename method matching reflection filter → warning contains evidence chain
    [TestMethod]
    public void RRW8_Rename_MethodMatchingReflectionFilter_HasWarning()
    {
        var code = @"extends Node
class_name ReflRenameTest3

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_something() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_rename3.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("refl_rename3"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("test_something");

            var result = service.PlanRename(symbol!, "renamed_func");

            var reflectionWarnings = result.Warnings
                .Where(w => w.Message.Contains("Reflection pattern") || w.Message.Contains("reflection"))
                .ToList();

            reflectionWarnings.Should().NotBeEmpty("test_something matches begins_with(\"test_\") filter");
            reflectionWarnings[0].Message.Should().Contain("get_method_list()");
            reflectionWarnings[0].Message.Should().Contain("call()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
