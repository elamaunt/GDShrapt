using System;
using System.Linq;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

public abstract class SmokeTestBase
{
    private static GDScriptProject? _project;
    private static GDServiceRegistry? _registry;
    private static string? _projectRoot;

    protected static GDScriptProject Project => _project!;
    protected static GDServiceRegistry Registry => _registry!;
    protected static string ProjectRoot => _projectRoot!;

    protected static void InitProject(string repoUrl, string repoName)
    {
        var repoPath = ExternalProjectFixture.EnsureRepo(repoUrl, repoName);
        _projectRoot = ExternalProjectFixture.FindGodotProjectRoot(repoPath);
        _project = ExternalProjectFixture.LoadProject(_projectRoot);

        _registry = new GDServiceRegistry();
        _registry.LoadModules(_project, new GDBaseModule());

        Console.WriteLine($"[SMOKE] Loaded {repoName}: {_project.ScriptFiles.Count()} scripts from {_projectRoot}");
    }

    protected static void CleanupProject()
    {
        _project?.Dispose();
        _project = null;
        _registry = null;
    }

    protected static GDScriptFile GetFirstScript()
    {
        return Project.ScriptFiles.First(f => f.FullPath != null);
    }

    protected static GDScriptFile? FindScript(string fileName)
    {
        return Project.ScriptFiles.FirstOrDefault(f =>
            f.FullPath != null &&
            f.FullPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
    }

    protected static void SimulateEdit(GDScriptFile script, string newContent)
    {
        script.Reload(newContent);
        script.Analyze(Project.CreateRuntimeProvider());
    }

    protected void VerifyCompletionAfterEdit(GDScriptFile script)
    {
        var handler = Registry.GetService<IGDCompletionHandler>()!;
        var lspHandler = new GDLspCompletionHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        var result = lspHandler.HandleAsync(new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();

        result.Should().NotBeNull();
    }

    protected void VerifyHoverAfterEdit(GDScriptFile script)
    {
        var handler = Registry.GetService<IGDHoverHandler>()!;
        var lspHandler = new GDLspHoverHandler(handler);
        var uri = GDDocumentManager.PathToUri(script.FullPath!);

        // Hover may return null (no symbol at position), just verify no crash
        lspHandler.HandleAsync(new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier { Uri = uri },
            Position = new GDLspPosition(0, 0)
        }, CancellationToken.None).GetAwaiter().GetResult();
    }
}
