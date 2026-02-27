using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
[TestCategory("Performance")]
public class HandlerLatencyTests
{
    private static GDScriptProject? _project;
    private static GDServiceRegistry? _registry;
    private static GDScriptFile? _script;

    [ClassInitialize]
    public static void Init(TestContext _)
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(
            "https://github.com/elamaunt/GDShrapt-Demo.git", "GDShrapt-Demo");
        var projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);
        _project = ExternalProjectFixture.LoadProject(projectRoot);
        _registry = new GDServiceRegistry();
        _registry.LoadModules(_project, new GDBaseModule());
        _script = _project.ScriptFiles.First(f => f.FullPath != null);

        Console.WriteLine($"[LATENCY] Loaded GDShrapt-Demo: {_project.ScriptFiles.Count()} scripts");
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _project?.Dispose();
        _project = null;
        _registry = null;
        _script = null;
    }

    [TestMethod]
    [Timeout(30000)]
    public void Completion_Latency_Under2Seconds()
    {
        var handler = _registry!.GetService<IGDCompletionHandler>()!;
        var lspHandler = new GDLspCompletionHandler(handler);

        var sw = Stopwatch.StartNew();
        var result = lspHandler.HandleAsync(new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(_script!.FullPath!)
            },
            Position = new GDLspPosition(5, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"[LATENCY] Completion: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        result.Should().NotBeNull();
    }

    [TestMethod]
    [Timeout(30000)]
    public void Hover_Latency_Under500ms()
    {
        var handler = _registry!.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);

        var sw = Stopwatch.StartNew();
        lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(_script!.FullPath!)
            },
            Position = new GDLspPosition(5, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"[LATENCY] Hover: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [TestMethod]
    [Timeout(30000)]
    public void Definition_Latency_Under500ms()
    {
        var handler = _registry!.GetService<IGDGoToDefHandler>()!;
        var lspHandler = new GDDefinitionHandler(handler);

        var sw = Stopwatch.StartNew();
        lspHandler.HandleAsync(new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(_script!.FullPath!)
            },
            Position = new GDLspPosition(5, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"[LATENCY] Definition: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [TestMethod]
    [Timeout(30000)]
    public void DocumentSymbols_Latency_Under1Second()
    {
        var handler = _registry!.GetService<IGDSymbolsHandler>()!;
        var lspHandler = new GDDocumentSymbolHandler(handler);

        var sw = Stopwatch.StartNew();
        var result = lspHandler.HandleAsync(new GDDocumentSymbolParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(_script!.FullPath!)
            }
        }, CancellationToken.None).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"[LATENCY] DocumentSymbols: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        result.Should().NotBeNull();
    }

    [TestMethod]
    [Timeout(30000)]
    public void Formatting_Latency_Under2Seconds()
    {
        var handler = _registry!.GetService<IGDFormatHandler>()!;
        var lspHandler = new GDFormattingHandler(handler, null);

        var sw = Stopwatch.StartNew();
        lspHandler.HandleAsync(new GDDocumentFormattingParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(_script!.FullPath!)
            },
            Options = new GDFormattingOptions { TabSize = 4, InsertSpaces = false }
        }, CancellationToken.None).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"[LATENCY] Formatting: {sw.ElapsedMilliseconds}ms");
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    [Timeout(60000)]
    public void AllHandlers_10Iterations_AverageLatency()
    {
        var uri = GDDocumentManager.PathToUri(_script!.FullPath!);
        var latencies = new Dictionary<string, List<long>>
        {
            ["Completion"] = new(),
            ["Hover"] = new(),
            ["Definition"] = new(),
            ["DocumentSymbols"] = new(),
            ["Formatting"] = new()
        };

        var completionHandler = new GDLspCompletionHandler(_registry!.GetService<IGDCompletionHandler>()!);
        var hoverHandler = new GDLspHoverHandler(_registry.GetService<IGDHoverHandler>()!);
        var definitionHandler = new GDDefinitionHandler(_registry.GetService<IGDGoToDefHandler>()!);
        var symbolsHandler = new GDDocumentSymbolHandler(_registry.GetService<IGDSymbolsHandler>()!);
        var formattingHandler = new GDFormattingHandler(_registry.GetService<IGDFormatHandler>()!, null);

        var textDoc = new GDLspTextDocumentIdentifier { Uri = uri };
        var position = new GDLspPosition(5, 0);

        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();
            completionHandler.HandleAsync(new GDCompletionParams
            {
                TextDocument = textDoc, Position = position
            }, CancellationToken.None).GetAwaiter().GetResult();
            latencies["Completion"].Add(sw.ElapsedMilliseconds);

            sw.Restart();
            hoverHandler.HandleAsync(new GDHoverParams
            {
                TextDocument = textDoc, Position = position
            }, CancellationToken.None).GetAwaiter().GetResult();
            latencies["Hover"].Add(sw.ElapsedMilliseconds);

            sw.Restart();
            definitionHandler.HandleAsync(new GDDefinitionParams
            {
                TextDocument = textDoc, Position = position
            }, CancellationToken.None).GetAwaiter().GetResult();
            latencies["Definition"].Add(sw.ElapsedMilliseconds);

            sw.Restart();
            symbolsHandler.HandleAsync(new GDDocumentSymbolParams
            {
                TextDocument = textDoc
            }, CancellationToken.None).GetAwaiter().GetResult();
            latencies["DocumentSymbols"].Add(sw.ElapsedMilliseconds);

            sw.Restart();
            formattingHandler.HandleAsync(new GDDocumentFormattingParams
            {
                TextDocument = textDoc,
                Options = new GDFormattingOptions { TabSize = 4, InsertSpaces = false }
            }, CancellationToken.None).GetAwaiter().GetResult();
            latencies["Formatting"].Add(sw.ElapsedMilliseconds);
        }

        Console.WriteLine("\n[LATENCY] === 10-iteration averages ===");
        foreach (var (name, times) in latencies)
        {
            Console.WriteLine($"[LATENCY] {name}: avg={times.Average():F0}ms, min={times.Min()}ms, max={times.Max()}ms, p95={Percentile(times, 95):F0}ms");
        }
    }

    private static double Percentile(List<long> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return sorted[lower];

        return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
    }
}
