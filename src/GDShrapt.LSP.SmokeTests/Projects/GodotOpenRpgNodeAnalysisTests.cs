using System;
using System.Collections.Generic;
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

    [TestMethod]
    public void DoorScript_AnimReferences_CountMatchesFindRefs()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var findRefsHandler = Registry.GetService<IGDFindRefsHandler>()!;
        var result = findRefsHandler.FindAllReferences("_anim", script!.FullPath);

        Console.WriteLine($"[REFS] Symbol: {result.SymbolName}, Kind: {result.SymbolKind}");
        Console.WriteLine($"[REFS] Declared in: {result.DeclaredInClassName} at {result.DeclaredInFilePath}:{result.DeclaredAtLine}");

        // Count primary references
        int primaryCount = 0;
        foreach (var group in result.PrimaryGroups)
        {
            Console.WriteLine($"[REFS] Primary group: {group.ClassName} ({group.Locations.Count} locations)");
            foreach (var loc in group.Locations)
            {
                Console.WriteLine($"[REFS]   {loc.FilePath}:{loc.Line}:{loc.Column} [{loc.Confidence}] decl={loc.IsDeclaration} write={loc.IsWrite}");
                primaryCount++;
            }
            foreach (var ovr in group.Overrides)
            {
                Console.WriteLine($"[REFS]   Override: {ovr.ClassName} ({ovr.Locations.Count} locations)");
                foreach (var loc in ovr.Locations)
                {
                    Console.WriteLine($"[REFS]     {loc.FilePath}:{loc.Line}:{loc.Column} [{loc.Confidence}]");
                    primaryCount++;
                }
            }
        }

        // Count unrelated references (other scripts with their own _anim)
        int unrelatedCount = 0;
        foreach (var group in result.UnrelatedGroups)
        {
            Console.WriteLine($"[REFS] Unrelated group: {group.ClassName} ({group.Locations.Count} locations)");
            unrelatedCount += group.Locations.Count;
            foreach (var ovr in group.Overrides)
                unrelatedCount += ovr.Locations.Count;
        }

        Console.WriteLine($"[REFS] Total primary: {primaryCount}, Total unrelated: {unrelatedCount}");

        // Primary refs should be small (door.gd only: declaration + ~5 usages)
        primaryCount.Should().BeLessThan(20,
            "_anim in door.gd should have a small number of primary references (not 63+)");

        // _anim is a private member — other scripts' _anim are independent and correctly excluded
        // The old bug (63 references) was in GDCodeLensHandler.CountProjectReferences, not in FindAllReferences
        Console.WriteLine($"[REFS] Unrelated groups count: {result.UnrelatedGroups.Count} (0 is correct for private members)");
    }

    [TestMethod]
    public void DoorScript_AnimCodeLens_ShowsCorrectCount()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;
        var lenses = codeLensHandler.GetCodeLenses(script!.FullPath!);

        Console.WriteLine($"[LENS] Total lenses: {lenses.Count}");
        foreach (var lens in lenses)
        {
            Console.WriteLine($"[LENS] L{lens.Line}: {lens.Label} ({lens.CommandArgument})");
        }

        var animLens = lenses.FirstOrDefault(l => l.CommandArgument == "_anim");
        animLens.Should().NotBeNull("CodeLens for _anim should exist in door.gd");

        // After fix: should NOT show 63 references
        animLens!.Label.Should().NotContain("63",
            "CodeLens should not naively count _anim across all scripts");

        Console.WriteLine($"[LENS] _anim label: {animLens.Label}");

        // Verify click count matches label count using cached refs
        AssertCodeLensLabelMatchesCachedRefs(codeLensHandler, lenses, script, "_anim");
    }

    [TestMethod]
    public void DoorScript_MethodCodeLens_ShowsCorrectCounts()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;
        var lenses = codeLensHandler.GetCodeLenses(script!.FullPath!);

        var methodsToTest = new[] { "_ready", "open", "_on_area_entered", "_on_blackout" };

        foreach (var methodName in methodsToTest)
        {
            Console.WriteLine($"--- Testing method: {methodName} ---");
            AssertCodeLensLabelMatchesCachedRefs(codeLensHandler, lenses, script, methodName);
        }
    }

    private static void AssertCodeLensLabelMatchesCachedRefs(
        IGDCodeLensHandler codeLensHandler,
        System.Collections.Generic.IReadOnlyList<GDCodeLens> lenses,
        GDScriptFile script,
        string symbolName)
    {
        var lens = lenses.FirstOrDefault(l => l.CommandArgument == symbolName);
        lens.Should().NotBeNull($"CodeLens for '{symbolName}' should exist in door.gd");

        var cached = codeLensHandler.GetCachedReferences(symbolName, script.FullPath!);
        cached.Should().NotBeNull($"Cached references for '{symbolName}' should exist after GetCodeLenses");

        // Count non-own-declaration entries (what the user sees on click)
        int clickCount = cached!.Count(r => !r.IsDeclaration);

        // Parse the strict count from the label (format: "N references" or "N references (+M unions)")
        var labelNumber = lens!.Label.Split(' ')[0];
        int labelStrict = int.Parse(labelNumber);

        // Parse union count if present
        int labelUnion = 0;
        var plusIdx = lens.Label.IndexOf("(+");
        if (plusIdx >= 0)
        {
            var unionStr = lens.Label.Substring(plusIdx + 2);
            unionStr = unionStr.Split(' ')[0];
            labelUnion = int.Parse(unionStr);
        }

        int labelTotal = labelStrict + labelUnion;

        Console.WriteLine($"[LENS] {symbolName}: label='{lens.Label}', labelTotal={labelTotal}, clickCount={clickCount}");

        clickCount.Should().Be(labelTotal,
            $"Clicking CodeLens for '{symbolName}' should show exactly the same number of references as the label");
    }

    [TestMethod]
    public void DoorScript_HoverOnGamepieceType_ShowsTypeInfo()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // var _dummy_gp: Gamepiece = null
        var line = FindLineContaining(script, "Gamepiece");
        line.Should().BeGreaterThan(-1, "door.gd should contain Gamepiece type annotation");

        var column = GetColumnOf(script, line, "Gamepiece");

        Console.WriteLine($"[HOVER] Testing hover on Gamepiece at line={line}, column={column}");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        Console.WriteLine($"[HOVER] Result: {(result == null ? "null" : result.Contents.Value)}");

        result.Should().NotBeNull("hover on Gamepiece type annotation should return result");
        result!.Contents.Value.Should().Contain("Gamepiece",
            "hover should show Gamepiece type info");
    }

    [TestMethod]
    public void DoorScript_HoverOnOnAreaEntered_DoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        // func _on_area_entered(area: Area2D) -> void:
        var line = FindLineContaining(script, "_on_area_entered");
        line.Should().BeGreaterThan(-1, "door.gd should contain _on_area_entered");

        var column = GetColumnOf(script, line, "_on_area_entered");

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
    public void DoorScript_CodeLensComputation_DoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;

        Func<Task> act = () => Task.Run(() =>
        {
            codeLensHandler.GetCodeLenses(script!.FullPath!);
        });

        act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void DoorScript_HoverAfterCodeLens_StillWorks()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        // First, compute CodeLens (simulates what happens in the editor)
        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;
        codeLensHandler.GetCodeLenses(script!.FullPath!);

        // Then, hover on _on_area_entered (simulates user hovering after CodeLens loads)
        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        var line = FindLineContaining(script, "_on_area_entered");
        var column = GetColumnOf(script, line, "_on_area_entered");

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
    public void DoorScript_CodeLensReferences_AllHighlightCorrectText()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;
        var lenses = codeLensHandler.GetCodeLenses(script!.FullPath!);

        var symbolsToCheck = new[] { "_on_area_entered", "_on_blackout", "open", "_ready" };

        foreach (var symbolName in symbolsToCheck)
        {
            var lens = lenses.FirstOrDefault(l => l.CommandArgument == symbolName);
            if (lens == null)
                continue;

            var cached = codeLensHandler.GetCachedReferences(symbolName, script.FullPath!);
            if (cached == null || cached.Count == 0)
                continue;

            Console.WriteLine($"--- {symbolName}: {cached.Count} cached refs ---");

            foreach (var r in cached)
            {
                var rangeWidth = r.EndColumn - r.Column;

                r.EndColumn.Should().BeGreaterThan(r.Column,
                    $"Reference for '{symbolName}' at {r.FilePath}:{r.Line}:{r.Column} " +
                    $"should have a non-zero highlight range");

                rangeWidth.Should().Be(symbolName.Length,
                    $"Highlight width for '{symbolName}' at {r.FilePath}:{r.Line}:{r.Column} " +
                    $"should equal symbol name length ({symbolName.Length})");

                // Read the file and verify the highlighted text matches the symbol name
                if (!System.IO.File.Exists(r.FilePath))
                    continue;

                var lines = System.IO.File.ReadAllLines(r.FilePath);
                var lineIdx = r.Line - 1;
                var colIdx = r.Column - 1;

                if (lineIdx < 0 || lineIdx >= lines.Length)
                    continue;

                var lineText = lines[lineIdx];
                var highlighted = colIdx >= 0 && colIdx + symbolName.Length <= lineText.Length
                    ? lineText.Substring(colIdx, symbolName.Length)
                    : $"<out of range: col={colIdx}, lineLen={lineText.Length}>";

                Console.WriteLine($"  {r.FilePath}:{r.Line}:{r.Column}-{r.EndColumn} " +
                    $"decl={r.IsDeclaration} text='{highlighted}'");

                if (highlighted != symbolName)
                {
                    // Dump raw reference data from cache for diagnosis
                    var projectModel = new GDProjectSemanticModel(Project);
                    var collector = new GDSymbolReferenceCollector(Project, projectModel);
                    var rawRefs = collector.CollectReferences(symbolName, script.FullPath!);
                    var matchingRaw = rawRefs.References.Where(raw =>
                        raw.FilePath != null &&
                        raw.FilePath.Replace('\\', '/').EndsWith(
                            r.FilePath.Replace('\\', '/').Split('/').Last(),
                            StringComparison.OrdinalIgnoreCase) &&
                        raw.Line + 1 == r.Line).ToList();

                    foreach (var raw in matchingRaw)
                    {
                        Console.WriteLine($"    RAW: L{raw.Line}:C{raw.Column} Kind={raw.Kind} " +
                            $"Conf={raw.Confidence} Node={raw.Node?.GetType().Name} " +
                            $"IdentToken={raw.IdentifierToken?.GetType().Name}@{raw.IdentifierToken?.StartLine}:{raw.IdentifierToken?.StartColumn}");
                    }
                }

                highlighted.Should().Be(symbolName,
                    $"Highlighted text at {r.FilePath}:{r.Line}:{r.Column} should be '{symbolName}' " +
                    $"but was '{highlighted}'. Full line: '{lineText}'");
            }
        }
    }

    [TestMethod]
    public void DoorScript_AfterEdit_HoverOnElseBranch_DoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        // Simulate editing the file (triggers Reload + Analyze, like didChange in LSP)
        SimulateEdit(script, original + "\n# edit trigger");

        // Hover on "else:" inside open() method — line 47 (0-based=46)
        var elseLine = FindLineContaining(script, "else:");
        elseLine.Should().BeGreaterThan(-1, "door.gd should contain 'else:'");

        Func<Task> act = async () =>
        {
            await Task.Run(() => lspHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                Position = new GDLspPosition(elseLine, 2)
            }, CancellationToken.None));
        };

        act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

        // Restore original
        SimulateEdit(script, original);
    }

    [TestMethod]
    public void DoorScript_AfterEdit_HoverOnMultiplePositions_DoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        // Simulate editing
        SimulateEdit(script, original + "\n# rapid edit");

        // Hover on multiple positions rapidly (simulates mouse movement after edit)
        var positions = new[]
        {
            ("_anim", "open"),
            ("_closed_door", "open"),
            ("is_locked", "is_locked"),
            ("_dummy_gp", "_dummy_gp"),
        };

        Func<Task> act = async () =>
        {
            foreach (var (text, _) in positions)
            {
                var line = FindLineContaining(script, text);
                if (line < 0) continue;
                var col = GetColumnOf(script, line, text);

                await Task.Run(() => lspHandler.HandleAsync(new GDHoverParams
                {
                    TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                    Position = new GDLspPosition(line, col)
                }, CancellationToken.None));
            }
        };

        act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

        // Restore original
        SimulateEdit(script, original);
    }

    [TestMethod]
    [Timeout(30000)]
    public void DoorScript_EditBreakingAnimationFinished_AnalyzeDoesNotHang()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;
        var projectModel = Registry.GetService<GDProjectSemanticModel>()!;

        // Verify we start with a semantic model
        script.SemanticModel.Should().NotBeNull("initial script should have SemanticModel");

        // Get initial diagnostic count (with semantic model)
        var diagnosticsService = new GDDiagnosticsService();
        var initialResult = GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService);
        var initialCount = initialResult.Diagnostics.Count();
        initialCount.Should().BeGreaterThan(0, "initial diagnostics should have results");
        Console.WriteLine($"Initial diagnostics: {initialCount} (hasModel={script.SemanticModel != null})");

        // Insert a space inside "animation_finished" to break it: "animation_fi nished"
        var broken = original.Replace("animation_finished", "animation_fi nished");
        broken.Should().NotBe(original, "replacement should have changed the content");

        // === Simulate real LSP didChange path ===
        // 1. Reload (sets SemanticModel = null, re-parses)
        script.Reload(broken);
        script.SemanticModel.Should().BeNull("Reload should clear SemanticModel");

        // 2. InvalidateFile (clears cache, re-collects signals)
        projectModel.InvalidateFile(script.FullPath!);

        // 3. DiagnoseWithSemantics WITHOUT manual GetSemanticModel — this is the real LSP path
        //    Before the fix, this would run with hasModel=False and produce fewer diagnostics
        var afterEditResult = GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService);
        var afterEditCount = afterEditResult.Diagnostics.Count();
        Console.WriteLine($"After edit diagnostics (no model rebuild): {afterEditCount} (hasModel={script.SemanticModel != null})");

        // 4. Now simulate what the fixed PublishDiagnosticsAsync does:
        //    Rebuild semantic model via GetSemanticModel BEFORE running diagnostics
        script.Reload(broken); // Reset again
        projectModel.InvalidateFile(script.FullPath!);
        script.SemanticModel.Should().BeNull("Reload should clear SemanticModel again");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        projectModel.GetSemanticModel(script); // This is what the fix adds
        var rebuildMs = sw.ElapsedMilliseconds;
        script.SemanticModel.Should().NotBeNull("GetSemanticModel should rebuild the model");
        Console.WriteLine($"Model rebuild: {rebuildMs}ms");

        var fixedResult = GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService);
        var fixedCount = fixedResult.Diagnostics.Count();
        Console.WriteLine($"After rebuild diagnostics: {fixedCount} (hasModel={script.SemanticModel != null})");

        // The fixed path should produce MORE diagnostics than the broken path (semantic diagnostics included)
        fixedCount.Should().BeGreaterThanOrEqualTo(afterEditCount,
            "diagnostics with rebuilt model should include semantic diagnostics");

        // 5. Verify concurrent requests work after rebuild
        var uri = GDDocumentManager.PathToUri(script.FullPath!);
        var codeLensHandler = Registry.GetService<IGDCodeLensHandler>()!;
        var lspCodeLensHandler = new GDLspCodeLensHandler(codeLensHandler);
        var hoverHandler = Registry.GetService<IGDHoverHandler>()!;
        var lspHoverHandler = new GDLspHoverHandler(hoverHandler);
        var tokensHandler = Registry.GetService<IGDSemanticTokensHandler>()!;
        var semanticTokensHandler = new GDLspSemanticTokensHandler(tokensHandler);

        sw.Restart();
        Func<Task> concurrentAct = async () =>
        {
            var tasks = new List<Task>
            {
                Task.Run(() => { GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService); }),
                Task.Run(async () => { await lspCodeLensHandler.HandleAsync(new GDCodeLensParams
                {
                    TextDocument = new GDLspTextDocumentIdentifier { Uri = uri }
                }, CancellationToken.None); }),
                Task.Run(async () => { await semanticTokensHandler.HandleAsync(new GDSemanticTokensParams
                {
                    TextDocument = new GDLspTextDocumentIdentifier { Uri = uri }
                }, CancellationToken.None); }),
                Task.Run(async () => { await lspHoverHandler.HandleAsync(new GDHoverParams
                {
                    TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                    Position = new GDLspPosition(0, 0)
                }, CancellationToken.None); }),
            };
            await Task.WhenAll(tasks);
        };
        concurrentAct.Should().CompleteWithinAsync(TimeSpan.FromSeconds(10),
            "Concurrent LSP requests should not hang on broken AST")
            .GetAwaiter().GetResult();
        Console.WriteLine($"Concurrent requests: {sw.ElapsedMilliseconds}ms");

        // Restore original
        SimulateEdit(script, original);
    }

    [TestMethod]
    [Timeout(30000)]
    public void DoorScript_EditBreakingAnimationFinished_RepeatedHighlightDoesNotAccumulateCPU()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        // Break AST: insert space in animation_finished
        var broken = original.Replace("animation_finished", "animation_fi nished");
        SimulateEdit(script, broken);

        var goToDefHandler = Registry.GetService<IGDGoToDefHandler>()!;
        var projectModel = new GDProjectSemanticModel(Project);
        var highlightHandler = new GDHighlightHandler(Project, projectModel);
        var handler = new GDLspDocumentHighlightHandler(highlightHandler, goToDefHandler);

        var uri = GDDocumentManager.PathToUri(script.FullPath!);
        var line = FindLineContaining(script, "animation_fi nished");
        var column = GetColumnOf(script, line, "animation_fi nished");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        CancellationTokenSource? previousCts = null;

        for (int i = 0; i < 20; i++)
        {
            // Cancel previous request (simulates VS Code $/cancelRequest)
            previousCts?.Cancel();
            previousCts?.Dispose();

            previousCts = new CancellationTokenSource();
            var @params = new GDDocumentHighlightParams
            {
                TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                Position = new GDLspPosition(line, column + (i % 5))
            };

            var result = handler.HandleAsync(@params, previousCts.Token).GetAwaiter().GetResult();
            // Result is null because FindDefinition times out on broken AST — that's expected
        }

        previousCts?.Cancel();
        previousCts?.Dispose();
        sw.Stop();

        Console.WriteLine($"[STRESS] 20 repeated highlights on broken AST: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(15),
            "20 repeated highlight requests should not accumulate unbounded CPU time");

        // Restore original
        SimulateEdit(script, original);
    }

    [TestMethod]
    [Timeout(10000)]
    public void DoorScript_EditBreakingAnimationFinished_HighlightWithCancellation_CompletesQuickly()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();
        var original = script!.LastContent!;

        var broken = original.Replace("animation_finished", "animation_fi nished");
        SimulateEdit(script, broken);

        var goToDefHandler = Registry.GetService<IGDGoToDefHandler>()!;
        var projectModel = new GDProjectSemanticModel(Project);
        var highlightHandler = new GDHighlightHandler(Project, projectModel);
        var handler = new GDLspDocumentHighlightHandler(highlightHandler, goToDefHandler);

        var uri = GDDocumentManager.PathToUri(script.FullPath!);
        var line = FindLineContaining(script, "animation_fi nished");
        var column = GetColumnOf(script, line, "animation_fi nished");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = handler.HandleAsync(new GDDocumentHighlightParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, cts.Token).GetAwaiter().GetResult();
        sw.Stop();

        result.Should().BeNull("cancelled request should return null");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100),
            "pre-cancelled request should return immediately");

        SimulateEdit(script, original);
    }

    [TestMethod]
    public void DoorScript_HoverOnAnimationFinished_ShowsSnakeCaseName()
    {
        var script = FindScript("doors/door.gd");
        script.Should().NotBeNull();

        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script!.FullPath!);

        var line = FindLineContaining(script, "animation_finished");
        line.Should().BeGreaterThan(-1, "door.gd should contain animation_finished");

        var column = GetColumnOf(script, line, "animation_finished");

        var result = lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(line, column)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull("hover on animation_finished should return result");
        var content = result!.Contents.Value;
        Console.WriteLine($"[HOVER] animation_finished content: {content}");

        content.Should().Contain("animation_finished",
            "hover should show snake_case signal name");
    }
}
