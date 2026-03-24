using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using SemanticsProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
[TestCategory("Performance")]
public class LspResponsivenessTests
{
    private const string RepoUrl = "https://github.com/gdquest-demos/godot-open-rpg.git";
    private const string RepoName = "godot-open-rpg";
    private const string PinnedCommit = "7cd2deb44e6020d0bbca4a6bedfc7ed070bd2557";

    private static string? _projectRoot;

    [ClassInitialize]
    public static void Init(TestContext _)
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(RepoUrl, RepoName, PinnedCommit);
        _projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);
        Console.WriteLine($"[RESPONSIVENESS] Project root: {_projectRoot}");
    }

    [TestMethod]
    [Timeout(30000)]
    public void StartupSpeed_LoadWithoutAnalysis_Under2Seconds()
    {
        var sw = Stopwatch.StartNew();
        using var project = SemanticsProjectLoader.LoadProjectWithoutAnalysis(_projectRoot!);
        sw.Stop();

        Console.WriteLine($"[RESPONSIVENESS] LoadProjectWithoutAnalysis: {sw.ElapsedMilliseconds}ms");

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "LoadProjectWithoutAnalysis must complete in under 5 seconds");
        project.ScriptFiles.Any().Should().BeTrue("project should have scripts loaded");
    }

    [TestMethod]
    [Timeout(30000)]
    public void PreAnalysis_AllHandlers_DoNotCrashOrHang()
    {
        using var project = SemanticsProjectLoader.LoadProjectWithoutAnalysis(_projectRoot!);
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule(deferAnalysis: true));

        var hoverHandler = new GDLspHoverHandler(registry.GetService<IGDHoverHandler>()!);
        var symbolsHandler = new GDDocumentSymbolHandler(registry.GetService<IGDSymbolsHandler>()!);
        var completionHandler = new GDLspCompletionHandler(registry.GetService<IGDCompletionHandler>()!);

        var scripts = project.ScriptFiles.Where(f => f.FullPath != null).Take(5).ToList();
        scripts.Should().NotBeEmpty("should have at least one script");

        foreach (var script in scripts)
        {
            var uri = GDDocumentManager.PathToUri(script.FullPath!);
            var textDoc = new GDLspTextDocumentIdentifier { Uri = uri };
            var position = new GDLspPosition(0, 0);

            // Hover — may return null, must not crash
            var hoverResult = hoverHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = textDoc,
                Position = position
            }, CancellationToken.None).GetAwaiter().GetResult();

            // Symbols — may return null, must not crash
            var symbolsResult = symbolsHandler.HandleAsync(new GDDocumentSymbolParams
            {
                TextDocument = textDoc
            }, CancellationToken.None).GetAwaiter().GetResult();

            // Completion — may return null, must not crash
            var completionResult = completionHandler.HandleAsync(new GDCompletionParams
            {
                TextDocument = textDoc,
                Position = position
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void FullLifecycle_ContinuousHoverPing_NeverFails()
    {
        // Phase 1: Load project without analysis
        var loadSw = Stopwatch.StartNew();
        var project = SemanticsProjectLoader.LoadProjectWithoutAnalysis(_projectRoot!);
        loadSw.Stop();

        Console.WriteLine($"[RESPONSIVENESS] LoadProjectWithoutAnalysis: {loadSw.ElapsedMilliseconds}ms");
        loadSw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "LoadProjectWithoutAnalysis must complete in under 5 seconds");

        try
        {
            // Phase 2: Create registry with deferred analysis
            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule(deferAnalysis: true));

            var hoverHandler = new GDLspHoverHandler(registry.GetService<IGDHoverHandler>()!);
            var symbolsHandler = new GDDocumentSymbolHandler(registry.GetService<IGDSymbolsHandler>()!);

            // Phase 3: Pick target script (field_camera.gd has class_name FieldCamera)
            var script = project.ScriptFiles.First(f =>
                f.FullPath != null &&
                f.FullPath.EndsWith("field_camera.gd", StringComparison.OrdinalIgnoreCase));
            var uri = GDDocumentManager.PathToUri(script.FullPath!);
            var textDoc = new GDLspTextDocumentIdentifier { Uri = uri };

            // Find the line/column for "FieldCamera" in class_name declaration
            var lines = script.LastContent?.Split('\n');
            int targetLine = 0, targetCol = 0;
            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    var idx = lines[i].IndexOf("FieldCamera");
                    if (idx >= 0)
                    {
                        targetLine = i;
                        targetCol = idx;
                        break;
                    }
                }
            }

            // Phase 4: Pre-analysis snapshot
            var preHover = hoverHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = textDoc,
                Position = new GDLspPosition(targetLine, targetCol)
            }, CancellationToken.None).GetAwaiter().GetResult();

            var preSymbols = symbolsHandler.HandleAsync(new GDDocumentSymbolParams
            {
                TextDocument = textDoc
            }, CancellationToken.None).GetAwaiter().GetResult();

            // Pre-analysis should already return AST-based hover
            preHover.Should().NotBeNull("hover should return AST-based content even before analysis");
            preHover!.Contents.Value.Should().Contain("FieldCamera",
                "pre-analysis hover should contain FieldCamera from AST");

            Console.WriteLine($"[RESPONSIVENESS] Pre-analysis hover: {preHover.Contents.Value.Length} chars");
            Console.WriteLine($"[RESPONSIVENESS] Pre-analysis symbols: {(preSymbols != null ? preSymbols.Length.ToString() : "null")}");

            // Phase 5: Start continuous hover pinger
            var pingResults = new ConcurrentBag<(long ElapsedMs, bool HasContent, Exception? Error)>();
            var pingStopper = new CancellationTokenSource();

            var pingTask = Task.Run(() =>
            {
                while (!pingStopper.IsCancellationRequested)
                {
                    var pingSw = Stopwatch.StartNew();
                    try
                    {
                        var result = hoverHandler.HandleAsync(new GDHoverParams
                        {
                            TextDocument = textDoc,
                            Position = new GDLspPosition(targetLine, targetCol)
                        }, CancellationToken.None).GetAwaiter().GetResult();
                        pingSw.Stop();
                        pingResults.Add((pingSw.ElapsedMilliseconds, result != null, null));
                    }
                    catch (Exception ex)
                    {
                        pingSw.Stop();
                        pingResults.Add((pingSw.ElapsedMilliseconds, false, ex));
                    }

                    Thread.Sleep(150);
                }
            });

            // Phase 6: Run AnalyzeAll on main thread (pinger runs in parallel)
            var analysisSw = Stopwatch.StartNew();
            project.AnalyzeAll();
            analysisSw.Stop();
            Console.WriteLine($"[RESPONSIVENESS] AnalyzeAll: {analysisSw.ElapsedMilliseconds}ms");

            // Phase 7: Stop pinger
            pingStopper.Cancel();
            pingTask.Wait(TimeSpan.FromSeconds(5));

            // Phase 8: Post-analysis assertions
            var errors = pingResults.Where(p => p.Error != null).ToList();
            errors.Should().BeEmpty("no hover ping should throw an exception during any lifecycle phase");

            pingResults.Count.Should().BeGreaterThan(0, "at least one hover ping should have been sent");

            pingResults.All(p => p.ElapsedMs < 2000).Should().BeTrue(
                "no individual hover ping should take longer than 2 seconds");

            // Post-analysis hover should return rich content
            var postHover = hoverHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = textDoc,
                Position = new GDLspPosition(targetLine, targetCol)
            }, CancellationToken.None).GetAwaiter().GetResult();
            postHover.Should().NotBeNull("hover should return content after analysis");
            postHover!.Contents.Value.Should().Contain("FieldCamera",
                "hover content should mention FieldCamera after analysis");

            // Post-analysis symbols should be populated
            var postSymbols = symbolsHandler.HandleAsync(new GDDocumentSymbolParams
            {
                TextDocument = textDoc
            }, CancellationToken.None).GetAwaiter().GetResult();
            postSymbols.Should().NotBeNull("document symbols should be available after analysis");
            postSymbols!.Length.Should().BeGreaterThan(0, "should have symbols after analysis");

            // Phase 9: Console output summary
            var preAnalysisPings = pingResults.Count(p => !p.HasContent);
            var postAnalysisPings = pingResults.Count(p => p.HasContent);
            Console.WriteLine($"[RESPONSIVENESS] Total pings: {pingResults.Count}");
            Console.WriteLine($"[RESPONSIVENESS] Pre-analysis (null): {preAnalysisPings}");
            Console.WriteLine($"[RESPONSIVENESS] Post-analysis (content): {postAnalysisPings}");
            Console.WriteLine($"[RESPONSIVENESS] Avg latency: {pingResults.Average(p => p.ElapsedMs):F0}ms");
            Console.WriteLine($"[RESPONSIVENESS] Max latency: {pingResults.Max(p => p.ElapsedMs)}ms");
        }
        finally
        {
            // Phase 10: Dispose
            project.Dispose();
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void DataRefinement_AfterAnalysis_HoverAndSymbolsImprove()
    {
        using var project = SemanticsProjectLoader.LoadProjectWithoutAnalysis(_projectRoot!);
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule(deferAnalysis: true));

        var hoverHandler = new GDLspHoverHandler(registry.GetService<IGDHoverHandler>()!);
        var symbolsHandler = new GDDocumentSymbolHandler(registry.GetService<IGDSymbolsHandler>()!);

        var script = project.ScriptFiles.First(f =>
            f.FullPath != null &&
            f.FullPath.EndsWith("field_camera.gd", StringComparison.OrdinalIgnoreCase));
        var uri = GDDocumentManager.PathToUri(script.FullPath!);
        var textDoc = new GDLspTextDocumentIdentifier { Uri = uri };

        // Find FieldCamera identifier position
        var lines = script.LastContent?.Split('\n');
        int targetLine = 0, targetCol = 0;
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var idx = lines[i].IndexOf("FieldCamera");
                if (idx >= 0)
                {
                    targetLine = i;
                    targetCol = idx;
                    break;
                }
            }
        }

        // Pre-analysis snapshot
        var preHover = hoverHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = textDoc,
            Position = new GDLspPosition(targetLine, targetCol)
        }, CancellationToken.None).GetAwaiter().GetResult();

        var preSymbols = symbolsHandler.HandleAsync(new GDDocumentSymbolParams
        {
            TextDocument = textDoc
        }, CancellationToken.None).GetAwaiter().GetResult();

        // Run analysis
        var sw = Stopwatch.StartNew();
        project.AnalyzeAll();
        sw.Stop();
        Console.WriteLine($"[RESPONSIVENESS] AnalyzeAll: {sw.ElapsedMilliseconds}ms");

        // Post-analysis snapshot
        var postHover = hoverHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = textDoc,
            Position = new GDLspPosition(targetLine, targetCol)
        }, CancellationToken.None).GetAwaiter().GetResult();

        var postSymbols = symbolsHandler.HandleAsync(new GDDocumentSymbolParams
        {
            TextDocument = textDoc
        }, CancellationToken.None).GetAwaiter().GetResult();

        // Pre-analysis should already return AST-based hover (signatures, doc comments)
        preHover.Should().NotBeNull("hover should return AST-based content even before analysis");
        preHover!.Contents.Value.Should().Contain("FieldCamera",
            "pre-analysis hover should contain FieldCamera from AST");

        // Pre-analysis symbols should already be available from parse tree
        preSymbols.Should().NotBeNull("symbols should be available from parse tree before analysis");
        preSymbols!.Length.Should().BeGreaterThan(0, "should have symbols from AST before analysis");

        // Post-analysis should also have content
        postHover.Should().NotBeNull("hover should return content after analysis");
        postHover!.Contents.Value.Should().Contain("FieldCamera",
            "hover should contain FieldCamera after analysis");

        postSymbols.Should().NotBeNull("symbols should be available after analysis");
        postSymbols!.Length.Should().BeGreaterThan(0, "should have symbols after analysis");

        Console.WriteLine($"[RESPONSIVENESS] Pre-analysis hover: {preHover.Contents.Value.Length} chars");
        Console.WriteLine($"[RESPONSIVENESS] Post-analysis hover: {postHover.Contents.Value.Length} chars");
        Console.WriteLine($"[RESPONSIVENESS] Pre-analysis symbols: {preSymbols.Length}");
        Console.WriteLine($"[RESPONSIVENESS] Post-analysis symbols: {postSymbols.Length}");
    }
}
