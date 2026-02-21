using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class CSharpInteropRenameWarningTests
{
    private static string CreateTempProject(
        (string name, string content)[] scripts,
        (string name, string path)[]? autoloads = null,
        string[]? csFiles = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_csirn_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"config_version=5

[application]
config/name=""CSIRenameTest""
";

        if (autoloads != null && autoloads.Length > 0)
        {
            projectGodot += "\n[autoload]\n";
            foreach (var (name, path) in autoloads)
            {
                projectGodot += $"{name}=\"*res://{path}\"\n";
            }
        }

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

        if (csFiles != null)
        {
            foreach (var csFile in csFiles)
            {
                var filePath = Path.Combine(tempPath, csFile);
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && dir != tempPath)
                    Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, "// C# placeholder");
            }
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

    [TestMethod]
    public void Rename_AutoloadMethod_InMixedProject_HasCSharpWarning()
    {
        var code = @"extends Node

func public_func():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("globals.gd", code) },
            autoloads: new[] { ("Globals", "globals.gd") },
            csFiles: new[] { "GameManager.cs" });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("globals"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("public_func");

            var result = service.PlanRename(symbol!, "renamed_func");

            result.Warnings.Should().Contain(w =>
                w.Message.Contains("C#") && w.Message.Contains("autoload"),
                "renaming a public method on an autoload in a mixed project should warn about C# callers");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Rename_AutoloadMethod_PureGDProject_NoWarning()
    {
        var code = @"extends Node

func public_func():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("globals.gd", code) },
            autoloads: new[] { ("Globals", "globals.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("globals"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("public_func");

            var result = service.PlanRename(symbol!, "renamed_func");

            var csharpWarnings = result.Warnings
                .Where(w => w.Message.Contains("C#"))
                .ToList();

            csharpWarnings.Should().BeEmpty(
                "no C# warning expected in pure GDScript project");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Rename_NonAutoloadMethod_InMixedProject_NoWarning()
    {
        var code = @"extends Node

func public_func():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("player.gd", code) },
            csFiles: new[] { "GameManager.cs" });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDRenameService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("player"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("public_func");

            var result = service.PlanRename(symbol!, "renamed_func");

            var csharpWarnings = result.Warnings
                .Where(w => w.Message.Contains("C#") && w.Message.Contains("autoload"))
                .ToList();

            csharpWarnings.Should().BeEmpty(
                "non-autoload method should not get C# interop warning");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
