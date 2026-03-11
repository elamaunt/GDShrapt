using System;
using System.Linq;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
public class GodotOpenRpgSmokeTests : SmokeTestBase
{
    private const string RepoUrl = "https://github.com/gdquest-demos/godot-open-rpg.git";
    private const string RepoName = "godot-open-rpg";

    [ClassInitialize]
    public static void Init(TestContext _) => InitProject(RepoUrl, RepoName);

    [ClassCleanup]
    public static void Cleanup() => CleanupProject();

    [TestMethod]
    [Timeout(60000)]
    public void LoadProject_CompletesWithoutCrash()
    {
        Project.Should().NotBeNull();
        Project.ScriptFiles.Should().NotBeNull();
    }

    [TestMethod]
    public void LoadProject_HasScripts()
    {
        Project.ScriptFiles.Any().Should().BeTrue();
        Console.WriteLine($"[SMOKE] godot-open-rpg: {Project.ScriptFiles.Count()} scripts");
    }

    [TestMethod]
    public void Completion_FirstScript_DoesNotCrash()
    {
        var handler = Registry.GetService<IGDCompletionHandler>()!;
        var lspHandler = new GDLspCompletionHandler(handler);
        var script = GetFirstScript();

        var result = lspHandler.HandleAsync(new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Hover_FirstScript_DoesNotCrash()
    {
        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var script = GetFirstScript();

        lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void Definition_FirstScript_DoesNotCrash()
    {
        var handler = Registry.GetService<IGDGoToDefHandler>()!;
        var lspHandler = new GDDefinitionHandler(handler);
        var script = GetFirstScript();

        lspHandler.HandleAsync(new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void DocumentSymbols_FirstScript_ReturnsSymbols()
    {
        var handler = Registry.GetService<IGDSymbolsHandler>()!;
        var lspHandler = new GDDocumentSymbolHandler(handler);
        var script = GetFirstScript();

        var result = lspHandler.HandleAsync(new GDDocumentSymbolParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            }
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull();
    }

    [TestMethod]
    public void References_FirstScript_DoesNotCrash()
    {
        var goToDefHandler = Registry.GetService<IGDGoToDefHandler>()!;
        var findRefsHandler = Registry.GetService<IGDFindRefsHandler>()!;
        var lspHandler = new GDReferencesHandler(findRefsHandler, goToDefHandler);
        var script = GetFirstScript();

        lspHandler.HandleAsync(new GDReferencesParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void Formatting_FirstScript_DoesNotCrash()
    {
        var handler = Registry.GetService<IGDFormatHandler>()!;
        var lspHandler = new GDFormattingHandler(handler, null);
        var script = GetFirstScript();

        lspHandler.HandleAsync(new GDDocumentFormattingParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Options = new GDFormattingOptions { TabSize = 4, InsertSpaces = false }
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void CodeAction_FirstScript_DoesNotCrash()
    {
        var handler = Registry.GetService<IGDCodeActionHandler>()!;
        var lspHandler = new GDLspCodeActionHandler(handler);
        var script = GetFirstScript();

        lspHandler.HandleAsync(new GDCodeActionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(script.FullPath!)
            },
            Range = new GDLspRange
            {
                Start = new GDLspPosition(0, 0),
                End = new GDLspPosition(0, 0)
            },
            Context = new GDCodeActionContext { Diagnostics = Array.Empty<GDLspDiagnostic>() }
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void AllScripts_ParseWithoutInvalidTokens()
    {
        foreach (var script in Project.ScriptFiles)
        {
            if (script.Class == null) continue;

            var invalidTokens = script.Class.AllNodes
                .OfType<GDInvalidToken>()
                .ToList();

            invalidTokens.Should().BeEmpty(
                because: $"script {script.FullPath} should parse without invalid tokens");
        }
    }

    [TestMethod]
    public void EditScript_AddFunction_HandlersStillWork()
    {
        var script = GetFirstScript();
        var original = script.LastContent;

        SimulateEdit(script, original + "\nfunc _smoke_test_func():\n\tpass\n");
        VerifyCompletionAfterEdit(script);
        VerifyHoverAfterEdit(script);

        SimulateEdit(script, original!);
    }

    [TestMethod]
    public void EditScript_BreakSyntax_NoCrash()
    {
        var script = GetFirstScript();
        var original = script.LastContent;

        SimulateEdit(script, "func ((( broken syntax {{{");
        VerifyCompletionAfterEdit(script);

        SimulateEdit(script, original!);
    }

    [TestMethod]
    public void EditScript_RestoreOriginal_NoStateCorruption()
    {
        var script = GetFirstScript();
        var original = script.LastContent;

        SimulateEdit(script, "# completely replaced content\nvar x = 1\n");
        SimulateEdit(script, original!);

        VerifyCompletionAfterEdit(script);
        VerifyHoverAfterEdit(script);
    }

    // ========================================================================
    // field.gd + field_camera.gd explicit tests
    // ========================================================================

    [TestMethod]
    public void Hover_FieldGd_CameraResetPosition()
    {
        var script = FindScript("field/field.gd");
        script.Should().NotBeNull("field.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "reset_position");
        line.Should().BeGreaterThanOrEqualTo(0, "field.gd should contain reset_position");

        var col = GetColumnOf(script!, line, "reset_position");

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on Camera.reset_position should return info");
        result!.Contents.Value.Should().Contain("reset_position",
            "hover content should mention reset_position");
    }

    [TestMethod]
    public void Hover_FieldGd_CameraMakeCurrent()
    {
        var script = FindScript("field/field.gd");
        script.Should().NotBeNull("field.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "make_current");
        line.Should().BeGreaterThanOrEqualTo(0, "field.gd should contain make_current");

        var col = GetColumnOf(script!, line, "make_current");

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on Camera.make_current should return info");
        result!.Contents.Value.Should().Contain("make_current",
            "hover content should mention make_current");
    }

    [TestMethod]
    public void GoToDef_FieldGd_CameraResetPosition_NavigatesToFieldCamera()
    {
        var script = FindScript("field/field.gd");
        script.Should().NotBeNull("field.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "reset_position");
        line.Should().BeGreaterThanOrEqualTo(0, "field.gd should contain reset_position");

        var col = GetColumnOf(script!, line, "reset_position");

        var handler = Registry.GetService<IGDGoToDefHandler>()!;
        var lspHandler = new GDDefinitionHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var (links, _) = lspHandler.HandleAsync(new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();

        links.Should().NotBeNullOrEmpty("GoToDef on reset_position should return a location");
        links![0].TargetUri.Should().Contain("field_camera",
            "reset_position definition should be in field_camera.gd");
    }

    [TestMethod]
    public void GoToDef_FieldGd_CameraMakeCurrent_DoesNotCrash()
    {
        var script = FindScript("field/field.gd");
        script.Should().NotBeNull("field.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "make_current");
        line.Should().BeGreaterThanOrEqualTo(0, "field.gd should contain make_current");

        var col = GetColumnOf(script!, line, "make_current");

        var handler = Registry.GetService<IGDGoToDefHandler>()!;
        var lspHandler = new GDDefinitionHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // make_current is inherited from Camera2D via FieldCamera autoload.
        // At minimum, this should not crash.
        lspHandler.HandleAsync(new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void References_FieldCamera_ResetPosition_IncludesCrossFileUsage()
    {
        var script = FindScript("field_camera.gd");
        script.Should().NotBeNull("field_camera.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "func reset_position");
        line.Should().BeGreaterThanOrEqualTo(0, "field_camera.gd should contain func reset_position");

        var col = GetColumnOf(script!, line, "reset_position");

        var goToDefHandler = Registry.GetService<IGDGoToDefHandler>()!;
        var findRefsHandler = Registry.GetService<IGDFindRefsHandler>()!;
        var lspHandler = new GDReferencesHandler(findRefsHandler, goToDefHandler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var result = lspHandler.HandleAsync(new GDReferencesParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col),
            Context = new GDReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("FindReferences should return results");
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "reset_position should have at least 2 references (declaration + usage in field.gd)");

        var uris = result.Select(r => r.Uri).ToList();
        uris.Should().Contain(u => u.Contains("field_camera"),
            "references should include field_camera.gd (declaration)");
        uris.Should().Contain(u => u.Contains("field/field.gd") || u.Contains("field\\field.gd"),
            "references should include field.gd (cross-file usage via Camera autoload)");
    }

    [TestMethod]
    public void Hover_FieldCameraGd_Extends_ShowsCamera2D()
    {
        var script = FindScript("field_camera.gd");
        script.Should().NotBeNull("field_camera.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "extends Camera2D");
        line.Should().BeGreaterThanOrEqualTo(0, "field_camera.gd should contain extends Camera2D");

        var col = GetColumnOf(script!, line, "Camera2D");

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on extends Camera2D should return info");
        result!.Contents.Value.Should().Contain("Camera2D",
            "hover on extends should show Camera2D, not FieldCamera");
        result.Contents.Value.Should().NotContain("FieldCamera",
            "hover on extends should NOT show FieldCamera class info");
    }

    [TestMethod]
    public void Hover_FieldCameraGd_ClassName_ShowsFieldCamera()
    {
        var script = FindScript("field_camera.gd");
        script.Should().NotBeNull("field_camera.gd should exist in godot-open-rpg");

        var line = FindLineContaining(script!, "class_name FieldCamera");
        line.Should().BeGreaterThanOrEqualTo(0, "field_camera.gd should contain class_name FieldCamera");

        var col = GetColumnOf(script!, line, "FieldCamera");

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, col)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on class_name FieldCamera should return info");
        result!.Contents.Value.Should().Contain("FieldCamera",
            "hover on class_name should show FieldCamera");
    }
}
