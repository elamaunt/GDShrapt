using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
[TestCategory("Memory")]
public class MemoryLeakTests
{
    private const string RepoUrl = "https://github.com/elamaunt/GDShrapt-Demo.git";
    private const string RepoName = "GDShrapt-Demo";

    [TestMethod]
    [Timeout(180000)]
    public void RepeatedEditReanalyze_NoMemoryLeak()
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(RepoUrl, RepoName);
        var projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);

        var memorySnapshots = new List<long>();

        using var project = ExternalProjectFixture.LoadProject(projectRoot);
        var script = project.ScriptFiles.First(f => f.FullPath != null);
        var originalContent = script.LastContent;

        for (int i = 0; i < 30; i++)
        {
            script.Reload(originalContent + $"\n# edit {i}\nfunc _test_{i}():\n\tpass\n");
            script.Analyze(project.CreateRuntimeProvider());

            script.Reload(originalContent!);
            script.Analyze(project.CreateRuntimeProvider());

            if (i % 5 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySnapshots.Add(GC.GetTotalMemory(true));
                Console.WriteLine($"[MEMORY] Edit cycle {i}: {memorySnapshots.Last() / (1024 * 1024)}MB");
            }
        }

        var firstQuarter = memorySnapshots.Take(2).Average();
        var lastQuarter = memorySnapshots.Skip(Math.Max(0, memorySnapshots.Count - 2)).Average();

        Console.WriteLine($"[MEMORY] First quarter avg: {firstQuarter / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Last quarter avg: {lastQuarter / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Growth ratio: {lastQuarter / firstQuarter:F2}x");

        lastQuarter.Should().BeLessThan(firstQuarter * 1.5,
            because: "memory should not grow significantly after repeated edit→reanalyze cycles");
    }

    [TestMethod]
    [Timeout(180000)]
    public void OpenCloseMultipleScripts_NoMemoryLeak()
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(RepoUrl, RepoName);
        var projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);

        using var project = ExternalProjectFixture.LoadProject(projectRoot);
        var scripts = project.ScriptFiles.Where(f => f.FullPath != null).Take(20).ToList();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var completionHandler = registry.GetService<IGDCompletionHandler>()!;
        var lspHandler = new GDLspCompletionHandler(completionHandler);

        for (int cycle = 0; cycle < 5; cycle++)
        {
            foreach (var script in scripts)
            {
                lspHandler.HandleAsync(new GDCompletionParams
                {
                    TextDocument = new GDLspTextDocumentIdentifier
                    {
                        Uri = GDDocumentManager.PathToUri(script.FullPath!)
                    },
                    Position = new GDLspPosition(0, 0)
                }, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var growth = finalMemory - baselineMemory;

        Console.WriteLine($"[MEMORY] Baseline: {baselineMemory / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Final: {finalMemory / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Growth: {growth / (1024 * 1024)}MB");

        finalMemory.Should().BeLessThan(baselineMemory * 2,
            because: "memory should not double after opening/completing multiple scripts");
    }

    [TestMethod]
    [Timeout(180000)]
    public void RepeatedCompletion_NoMemoryLeak()
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(RepoUrl, RepoName);
        var projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);

        using var project = ExternalProjectFixture.LoadProject(projectRoot);
        var script = project.ScriptFiles.First(f => f.FullPath != null);

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var completionHandler = registry.GetService<IGDCompletionHandler>()!;
        var lspHandler = new GDLspCompletionHandler(completionHandler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        var memorySnapshots = new List<long>();

        for (int i = 0; i < 50; i++)
        {
            var line = i % 10;
            lspHandler.HandleAsync(new GDCompletionParams
            {
                TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
                Position = new GDLspPosition(line, 0)
            }, CancellationToken.None).GetAwaiter().GetResult();

            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySnapshots.Add(GC.GetTotalMemory(true));
                Console.WriteLine($"[MEMORY] Completion iteration {i}: {memorySnapshots.Last() / (1024 * 1024)}MB");
            }
        }

        var firstQuarter = memorySnapshots.Take(2).Average();
        var lastQuarter = memorySnapshots.Skip(Math.Max(0, memorySnapshots.Count - 2)).Average();

        Console.WriteLine($"[MEMORY] Growth ratio: {lastQuarter / firstQuarter:F2}x");

        lastQuarter.Should().BeLessThan(firstQuarter * 1.5,
            because: "memory should remain stable after repeated completion requests");
    }

    [TestMethod]
    [Timeout(120000)]
    public void ProjectDisposal_ReleasesMemory()
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(RepoUrl, RepoName);
        var projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        var project = ExternalProjectFixture.LoadProject(projectRoot);
        var afterLoad = GC.GetTotalMemory(false);
        var memoryDuringLoad = afterLoad - baselineMemory;

        Console.WriteLine($"[MEMORY] Baseline: {baselineMemory / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] After load: {afterLoad / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Used by project: {memoryDuringLoad / (1024 * 1024)}MB");

        project.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterDisposal = GC.GetTotalMemory(true);
        var retained = afterDisposal - baselineMemory;

        Console.WriteLine($"[MEMORY] After disposal: {afterDisposal / (1024 * 1024)}MB");
        Console.WriteLine($"[MEMORY] Retained: {retained / (1024 * 1024)}MB");

        if (memoryDuringLoad > 0)
        {
            var retentionRatio = (double)retained / memoryDuringLoad;
            Console.WriteLine($"[MEMORY] Retention ratio: {retentionRatio:P0}");

            retained.Should().BeLessThan((long)(memoryDuringLoad * 0.3),
                because: "disposed project should release most of its memory (< 30% retained)");
        }
    }
}
