using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDDeadCodeCSharpInteropTests
{
    private static string CreateTempProject(
        (string name, string content)[] scripts,
        (string name, string path)[]? autoloads = null,
        string[]? csFiles = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_dcsi_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"config_version=5

[application]
config/name=""DeadCodeCSITest""
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

    private static GDDeadCodeReport RunDeadCodeAnalysis(string tempPath)
    {
        using var project = GDProjectLoader.LoadProject(tempPath);
        project.BuildCallSiteRegistry();
        var projectModel = new GDProjectSemanticModel(project);

        var options = new GDDeadCodeOptions
        {
            IncludeFunctions = true,
            IncludeVariables = true,
            IncludeSignals = true,
            IncludeConstants = true,
            IncludePrivate = true,
            MaxConfidence = GDReferenceConfidence.Strict
        };

        return projectModel.DeadCode.AnalyzeProject(options);
    }

    [TestMethod]
    public void AutoloadMethod_WithCSharp_NotStrictDeadCode()
    {
        var autoloadScript = @"extends Node

func show_example_dialogue_balloon():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("dialogue_manager.gd", autoloadScript) },
            autoloads: new[] { ("DialogueManager", "dialogue_manager.gd") },
            csFiles: new[] { "DialogueManager.cs" });

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var strictItems = report.Items
                .Where(i => i.Confidence == GDReferenceConfidence.Strict && i.Name == "show_example_dialogue_balloon")
                .ToList();

            strictItems.Should().BeEmpty(
                "autoload method in mixed project should not be Strict dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AutoloadMethod_WithCSharp_IsPotentialCSI()
    {
        var autoloadScript = @"extends Node

func show_example_dialogue_balloon():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("dialogue_manager.gd", autoloadScript) },
            autoloads: new[] { ("DialogueManager", "dialogue_manager.gd") },
            csFiles: new[] { "DialogueManager.cs" });

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var csiItems = report.Items
                .Where(i => i.Name == "show_example_dialogue_balloon" && i.ReasonCode == GDDeadCodeReasonCode.CSI)
                .ToList();

            csiItems.Should().NotBeEmpty(
                "autoload method in mixed project should be downgraded to CSI");
            csiItems[0].Confidence.Should().Be(GDReferenceConfidence.Potential);
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AutoloadMethod_NoCSharp_RemainsStrict()
    {
        var autoloadScript = @"extends Node

func show_example_dialogue_balloon():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("dialogue_manager.gd", autoloadScript) },
            autoloads: new[] { ("DialogueManager", "dialogue_manager.gd") });

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var strictItems = report.Items
                .Where(i => i.Confidence == GDReferenceConfidence.Strict && i.Name == "show_example_dialogue_balloon")
                .ToList();

            strictItems.Should().NotBeEmpty(
                "autoload method without C# should remain Strict dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AutoloadPrivateMethod_WithCSharp_RemainsStrict()
    {
        var autoloadScript = @"extends Node

func _private_helper():
    pass

func _ready():
    pass
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("dialogue_manager.gd", autoloadScript) },
            autoloads: new[] { ("DialogueManager", "dialogue_manager.gd") },
            csFiles: new[] { "DialogueManager.cs" });

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var privateItems = report.Items
                .Where(i => i.Name == "_private_helper")
                .ToList();

            // Private methods should not get CSI downgrade
            privateItems.Should().NotBeEmpty();
            privateItems.Should().NotContain(i => i.ReasonCode == GDDeadCodeReasonCode.CSI,
                "private methods should not be downgraded by CSI");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
