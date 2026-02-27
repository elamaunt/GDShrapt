using System;
using System.Diagnostics;
using System.IO;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

internal static class ExternalProjectFixture
{
    private static readonly string CacheRoot = Path.Combine(
        Path.GetTempPath(), "GDShrapt.SmokeTests");

    public static string EnsureRepo(string repoUrl, string repoName)
    {
        var repoPath = Path.Combine(CacheRoot, repoName);

        if (Directory.Exists(Path.Combine(repoPath, ".git")))
        {
            // Already cloned — try to update (best-effort)
            try
            {
                RunGit(repoPath, "pull --ff-only");
            }
            catch
            {
                // If pull fails (e.g., force-pushed), re-clone
                Directory.Delete(repoPath, true);
                Directory.CreateDirectory(CacheRoot);
                RunGit(CacheRoot, $"clone --depth 1 {repoUrl} {repoName}");
            }
        }
        else
        {
            Directory.CreateDirectory(CacheRoot);
            RunGit(CacheRoot, $"clone --depth 1 {repoUrl} {repoName}");
        }

        return repoPath;
    }

    public static string FindGodotProjectRoot(string repoPath)
    {
        if (File.Exists(Path.Combine(repoPath, "project.godot")))
            return repoPath;

        var found = Directory.GetFiles(repoPath, "project.godot", SearchOption.AllDirectories);
        if (found.Length > 0)
            return Path.GetDirectoryName(found[0])!;

        throw new InvalidOperationException($"project.godot not found in {repoPath}");
    }

    public static GDScriptProject LoadProject(string projectRoot)
    {
        var context = new GDDefaultProjectContext(projectRoot);
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        });

        project.LoadScripts();
        project.LoadScenes();
        project.AnalyzeAll();

        return project;
    }

    private static void RunGit(string workDir, string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit(120_000);

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {args} failed (exit {process.ExitCode}): {stderr}");
        }
    }
}
