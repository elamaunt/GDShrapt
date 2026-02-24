using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class FindRefsCSharpInteropTests
{
    private static string CreateTempProject(
        (string name, string content)[] scripts,
        (string name, string path)[]? autoloads = null,
        string[]? csFiles = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_csifr_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"config_version=5

[application]
config/name=""CSIFindRefsTest""
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
    public void FindRefs_AutoloadMethod_InMixedProject_HasInteropNote()
    {
        var autoloadCode = @"extends Node
class_name DialogMgr

func show_dialogue():
    pass

func _ready():
    show_dialogue()
";
        var callerCode = @"extends Node

func _ready():
    DialogMgr.show_dialogue()
";
        var tempPath = CreateTempProject(
            scripts: new[] {
                ("dialogue_manager.gd", autoloadCode),
                ("caller.gd", callerCode)
            },
            autoloads: new[] { ("DialogMgr", "dialogue_manager.gd") },
            csFiles: new[] { "CSharpCaller.cs" });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDFindReferencesService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("dialogue_manager"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("show_dialogue");
            symbol.Should().NotBeNull("show_dialogue should be found in semantic model");

            var cursor = new GDCursorPosition(
                symbol!.DeclarationNode!.StartLine,
                symbol.DeclarationNode.StartColumn);
            var context = new GDRefactoringContext(
                script, script.Class!, cursor, GDSelectionInfo.None, project);

            var result = service.FindReferencesForScope(context, symbol);

            result.Success.Should().BeTrue();

            // The declaration reference should have a C# interop note
            var declarationRef = result.AllReferences
                .FirstOrDefault(r => r.Kind == GDReferenceKind.Declaration);

            declarationRef.Should().NotBeNull("should have a declaration reference");
            declarationRef!.ConfidenceReason.Should().Contain("C#",
                "declaration on autoload in mixed project should have C# interop note");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void FindRefs_NonAutoload_NoInteropNote()
    {
        var code = @"extends Node
class_name PlayerCtrl

func do_something():
    pass

func _ready():
    do_something()
";
        var tempPath = CreateTempProject(
            scripts: new[] { ("player.gd", code) },
            csFiles: new[] { "CSharpCaller.cs" });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var service = new GDFindReferencesService(project, projectModel);
            var script = project.ScriptFiles.First(f => f.FullPath != null && f.FullPath.Contains("player"));
            var model = projectModel.GetSemanticModel(script);
            var symbol = model!.FindSymbol("do_something");
            symbol.Should().NotBeNull("do_something should be found in semantic model");

            var cursor = new GDCursorPosition(
                symbol!.DeclarationNode!.StartLine,
                symbol.DeclarationNode.StartColumn);
            var context = new GDRefactoringContext(
                script, script.Class!, cursor, GDSelectionInfo.None, project);

            var result = service.FindReferencesForScope(context, symbol);

            result.Success.Should().BeTrue();

            // Non-autoload symbol should not have C# interop note
            var allRefs = result.AllReferences;
            var csharpNotes = allRefs
                .Where(r => r.ConfidenceReason != null && r.ConfidenceReason.Contains("C#"))
                .ToList();

            csharpNotes.Should().BeEmpty(
                "non-autoload symbol should not have C# interop note");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
