using System;
using System.Linq;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
public class GodotOpenRpgReferencesTests : SmokeTestBase
{
    private const string RepoUrl = "https://github.com/gdquest-demos/godot-open-rpg.git";
    private const string RepoName = "godot-open-rpg-refs";
    private const string PinnedCommit = "cadfe15ec0e439f3e9cfa8c726a6dde423944d6b";

    [ClassInitialize]
    public static void Init(TestContext _) => InitProject(RepoUrl, RepoName, PinnedCommit);

    [ClassCleanup]
    public static void Cleanup() => CleanupProject();

    [TestMethod]
    public void References_UiTurnBar_Icon_CountIsCorrect()
    {
        var script = FindScript("ui_turn_bar.gd");
        script.Should().NotBeNull("ui_turn_bar.gd should exist in godot-open-rpg at cadfe15");

        var line = FindLineContaining(script!, "var icon: UIBattlerIcon");
        line.Should().BeGreaterThanOrEqualTo(0, "ui_turn_bar.gd should contain var icon: UIBattlerIcon");

        var col = GetColumnOf(script!, line, "icon");

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

        result.Should().NotBeNull("FindReferences should return results for icon");

        Console.WriteLine($"[REFERENCES] icon references count: {result!.Length}");
        foreach (var r in result)
            Console.WriteLine($"  {r.Uri} L{r.Range.Start.Line}:{r.Range.Start.Character}");

        result.Length.Should().Be(10,
            "icon should have exactly 10 references (1 declaration + 9 usages)");
    }
}
