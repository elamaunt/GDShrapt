using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using GDShrapt.Semantics.Validator;
using GDShrapt.Validator;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
public class GodotOpenRpgNodeAnalysisTests : SmokeTestBase
{
    private const string RepoUrl = "https://github.com/gdquest-demos/godot-open-rpg.git";
    private const string RepoName = "godot-open-rpg";

    [ClassInitialize]
    public static void Init(TestContext _) => InitProject(RepoUrl, RepoName);

    [ClassCleanup]
    public static void Cleanup() => CleanupProject();

    [TestMethod]
    public void DoorScript_IsFound()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull("door.gd should exist in godot-open-rpg");
    }

    [TestMethod]
    public void Project_ScenesLoaded()
    {
        Project.SceneTypesProvider.Should().NotBeNull("scene types provider should be enabled");
    }

    [TestMethod]
    public void DoorScript_HoverOnAnimationPlayer_ShowsAnimationPlayerType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // @onready var _anim: = $AnimationPlayer as AnimationPlayer
        var line = FindLineContaining(script, "$AnimationPlayer");
        line.Should().BeGreaterThan(-1, "door.gd should contain $AnimationPlayer");

        var column = GetColumnOf(script, line, "$AnimationPlayer");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on $AnimationPlayer should return result");
        result!.Contents.Value.Should().Contain("AnimationPlayer",
            "hover should show AnimationPlayer type, not Node");
    }

    [TestMethod]
    public void DoorScript_HoverOnClosedDoor_ShowsSprite2DType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // @onready var _closed_door: = $Area2D/ClosedDoor as Sprite2D
        var line = FindLineContaining(script, "$Area2D/ClosedDoor");
        line.Should().BeGreaterThan(-1, "door.gd should contain $Area2D/ClosedDoor");

        var column = GetColumnOf(script, line, "$Area2D/ClosedDoor");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on $Area2D/ClosedDoor should return result");
        result!.Contents.Value.Should().Contain("Sprite2D",
            "hover should show Sprite2D type, not Node");
    }

    [TestMethod]
    public void DoorScript_HoverOnAnimVariable_ShowsAnimationPlayerType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // @onready var _anim: = $AnimationPlayer as AnimationPlayer
        var line = FindLineContaining(script, "_anim");
        line.Should().BeGreaterThan(-1);

        var column = GetColumnOf(script, line, "_anim");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on _anim variable should return result");
        result!.Contents.Value.Should().Contain("AnimationPlayer",
            "hover on _anim should show AnimationPlayer type");
    }

    [TestMethod]
    public void DoorScript_HoverOnGetNodeExpression_DoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var line = FindLineContaining(script, "$AnimationPlayer");
        line.Should().BeGreaterThan(-1);

        var column = GetColumnOf(script, line, "$AnimationPlayer");

        Func<Task> act = async () =>
        {
            await lspHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                Position = new GDLspPosition(line, column)
            }, CancellationToken.None);
        };

        act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void DoorScript_HoverOnClosedDoorVariable_ShowsSprite2DType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // @onready var _closed_door: = $Area2D/ClosedDoor as Sprite2D
        var line = FindLineContaining(script, "_closed_door");
        line.Should().BeGreaterThan(-1);

        var column = GetColumnOf(script, line, "_closed_door");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on _closed_door variable should return result");
        result!.Contents.Value.Should().Contain("Sprite2D",
            "hover on _closed_door should show Sprite2D type");
    }

    [TestMethod]
    public void DoorScript_AfterEdit_HoverStillShowsAnimationPlayerType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        var line = FindLineContaining(script, "$AnimationPlayer");
        var column = GetColumnOf(script, line, "$AnimationPlayer");

        // 1. Verify type BEFORE edit
        var resultBefore = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        resultBefore.Should().NotBeNull();
        resultBefore!.Contents.Value.Should().Contain("AnimationPlayer",
            "BEFORE edit: hover should show AnimationPlayer");

        // 2. SimulateEdit (add a comment at the end)
        SimulateEdit(script, original + "\n# test edit");

        // 3. Verify type AFTER edit
        var lineAfter = FindLineContaining(script, "$AnimationPlayer");
        var columnAfter = GetColumnOf(script, lineAfter, "$AnimationPlayer");

        var resultAfter = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(lineAfter, columnAfter)
        }, CancellationToken.None).GetAwaiter().GetResult();

        resultAfter.Should().NotBeNull("AFTER edit: hover on $AnimationPlayer should still return result");
        resultAfter!.Contents.Value.Should().Contain("AnimationPlayer",
            "AFTER edit: hover should still show AnimationPlayer type, not Node");

        // 4. Restore original
        SimulateEdit(script, original);
    }

    [TestMethod]
    public void DoorScript_DummyGp_ShouldHaveGamepieceType_NotNull()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull("door.gd should exist in the project");

        // 1. Get semantic model
        var projectModel = new GDProjectSemanticModel(Project);
        var semanticModel = projectModel.GetSemanticModel(script!);
        semanticModel.Should().NotBeNull();

        // 2. Check symbol registration
        var symbol = semanticModel!.FindSymbol("_dummy_gp");
        symbol.Should().NotBeNull("_dummy_gp should be registered as a symbol");

        Console.WriteLine($"[DIAG] symbol.TypeName = '{symbol!.TypeName}'");
        Console.WriteLine($"[DIAG] symbol.Kind = {symbol.Kind}");

        symbol.TypeName.Should().Be("Gamepiece",
            "explicitly typed variable 'var _dummy_gp: Gamepiece = null' should have TypeName = Gamepiece");

        // 3. Find _dummy_gp.name access in the setter body via AST
        var memberAccessNodes = script!.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .Where(m => m.Identifier?.Sequence == "name" &&
                        m.CallerExpression is GDIdentifierExpression id &&
                        id.Identifier?.Sequence == "_dummy_gp")
            .ToList();

        Console.WriteLine($"[DIAG] Found {memberAccessNodes.Count} _dummy_gp.name access(es)");
        memberAccessNodes.Should().NotBeEmpty("door.gd should contain _dummy_gp.name access");

        // 4. Check the type via GetSymbolForNode on the caller expression
        foreach (var memberAccess in memberAccessNodes)
        {
            var callerExpr = memberAccess.CallerExpression!;
            var callerSymbol = semanticModel.GetSymbolForNode(callerExpr);
            Console.WriteLine($"[DIAG] GetSymbolForNode(_dummy_gp) at line {callerExpr.FirstLeafToken?.StartLine}: " +
                $"TypeName='{callerSymbol?.TypeName}', Kind={callerSymbol?.Kind}");

            // Check GetExpressionType separately (bypasses narrowing in GetEffectiveExpressionType)
            var exprType = ((IGDMemberAccessAnalyzer)semanticModel).GetExpressionType(callerExpr);
            Console.WriteLine($"[DIAG] GetExpressionType(_dummy_gp) = '{exprType}'");

            // Also check via IGDMemberAccessAnalyzer.GetEffectiveExpressionType (used by validator)
            var analyzer = (IGDMemberAccessAnalyzer)semanticModel;
            var effectiveType = analyzer.GetEffectiveExpressionType(callerExpr, memberAccess);
            Console.WriteLine($"[DIAG] GetEffectiveExpressionType(_dummy_gp) = '{effectiveType}'");

            effectiveType.Should().NotBe("null",
                "type of _dummy_gp should not be 'null' — it has explicit type annotation Gamepiece");
        }

        // 5. Run semantic validation on the class
        var validator = new GDSemanticValidator(semanticModel);
        var result = validator.Validate(script.Class);

        var gd3009 = result.Diagnostics
            .Where(d => d.Code == GDDiagnosticCode.PropertyNotFound)
            .ToList();

        Console.WriteLine($"[DIAG] Total GD3009 (PropertyNotFound) diagnostics: {gd3009.Count}");
        foreach (var diag in gd3009)
        {
            Console.WriteLine($"[DIAG]   {diag.CodeString} L{diag.StartLine}: {diag.Message}");
        }

        var nullTypeDiags = gd3009
            .Where(d => d.Message.Contains("type 'null'"))
            .ToList();

        nullTypeDiags.Should().BeEmpty(
            "GD3009 should not report 'Property not found on type null' for a variable with explicit Gamepiece type annotation");
    }

    [TestMethod]
    public void DoorScript_AfterEdit_AnimVariableStillShowsAnimationPlayerType()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        // Edit
        SimulateEdit(script, original + "\n# test edit");

        // Hover on _anim variable
        var line = FindLineContaining(script, "_anim");
        var column = GetColumnOf(script, line, "_anim");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull();
        result!.Contents.Value.Should().Contain("AnimationPlayer",
            "AFTER edit: _anim should still show AnimationPlayer type");

        // Restore
        SimulateEdit(script, original);
    }
}
