using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDCSharpInteropIndexTests
{
    private static string CreateTempProject(
        (string name, string content)[] scripts,
        (string name, string path)[]? autoloads = null,
        string[]? csFiles = null,
        string? tscnContent = null,
        string? tscnName = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_csi_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"config_version=5

[application]
config/name=""CSITestProject""
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

        if (tscnContent != null && tscnName != null)
        {
            var tscnPath = Path.Combine(tempPath, tscnName);
            var dir = Path.GetDirectoryName(tscnPath);
            if (!string.IsNullOrEmpty(dir) && dir != tempPath)
                Directory.CreateDirectory(dir);
            File.WriteAllText(tscnPath, tscnContent);
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
    public void HasCSharpCode_WithCsFiles_True()
    {
        var tempPath = CreateTempProject(
            scripts: new[] { ("test.gd", "extends Node\n") },
            csFiles: new[] { "PlayerController.cs" });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.HasCSharpCode.Should().BeTrue();
            projectModel.CSharpInterop.CSharpScriptPaths.Should().NotBeEmpty();
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void HasCSharpCode_NoCsFiles_False()
    {
        var tempPath = CreateTempProject(
            scripts: new[] { ("test.gd", "extends Node\n") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.HasCSharpCode.Should().BeFalse();
            projectModel.CSharpInterop.CSharpScriptPaths.Should().BeEmpty();
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void DetectsCSharpAutoload()
    {
        var tempPath = CreateTempProject(
            scripts: new[] { ("test.gd", "extends Node\n") },
            csFiles: new[] { "CSharpAutoload.cs" },
            autoloads: new[] { ("CSharpState", "CSharpAutoload.cs") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.HasCSharpAutoloads.Should().BeTrue();
            projectModel.CSharpInterop.CSharpAutoloadNames.Should().Contain("CSharpState");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void IgnoresGdScriptAutoload()
    {
        var tempPath = CreateTempProject(
            scripts: new[] { ("globals.gd", "extends Node\n") },
            autoloads: new[] { ("Globals", "globals.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.HasCSharpAutoloads.Should().BeFalse();
            projectModel.CSharpInterop.CSharpAutoloadNames.Should().BeEmpty();
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void DetectsCSharpSceneBinding()
    {
        var tscn = @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://PlayerController.cs"" id=""1_abc""]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""1_abc"")
";

        var tempPath = CreateTempProject(
            scripts: new[] { ("test.gd", "extends Node\n") },
            csFiles: new[] { "PlayerController.cs" },
            tscnContent: tscn,
            tscnName: "main.tscn");

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.CSharpSceneBindingCount.Should().BeGreaterThan(0);
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void IgnoresGdScriptScene()
    {
        var tscn = @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1_abc""]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""1_abc"")
";

        var tempPath = CreateTempProject(
            scripts: new[] {
                ("test.gd", "extends Node\n"),
                ("player.gd", "extends CharacterBody2D\n")
            },
            tscnContent: tscn,
            tscnName: "main.tscn");

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var projectModel = new GDProjectSemanticModel(project);

            projectModel.CSharpInterop.CSharpSceneBindingCount.Should().Be(0);
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
