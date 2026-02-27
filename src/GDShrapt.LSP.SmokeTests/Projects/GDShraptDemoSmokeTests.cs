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
public class GDShraptDemoSmokeTests : SmokeTestBase
{
    private const string RepoUrl = "https://github.com/elamaunt/GDShrapt-Demo.git";
    private const string RepoName = "GDShrapt-Demo";

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
            Context = new GDCodeActionContext { Diagnostics = System.Array.Empty<GDLspDiagnostic>() }
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

        // Restore original
        SimulateEdit(script, original!);
    }

    [TestMethod]
    public void EditScript_BreakSyntax_NoCrash()
    {
        var script = GetFirstScript();
        var original = script.LastContent;

        // Deliberately broken GDScript
        SimulateEdit(script, "func ((( broken syntax {{{");
        VerifyCompletionAfterEdit(script);

        // Restore original
        SimulateEdit(script, original!);
    }

    [TestMethod]
    public void EditScript_RestoreOriginal_NoStateCorruption()
    {
        var script = GetFirstScript();
        var original = script.LastContent;

        // Break → fix cycle
        SimulateEdit(script, "# completely replaced content\nvar x = 1\n");
        SimulateEdit(script, original!);

        // Handlers should work normally after restore
        VerifyCompletionAfterEdit(script);
        VerifyHoverAfterEdit(script);
    }
}
