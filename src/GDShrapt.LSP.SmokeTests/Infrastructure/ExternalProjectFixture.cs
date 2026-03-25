using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

internal static class ExternalProjectFixture
{
    private static readonly string CacheRoot = Path.Combine(
        Path.GetTempPath(), "GDShrapt.SmokeTests");

    private static readonly Mutex RepoMutex = new(false, "GDShrapt_SmokeTests_RepoMutex");

    public static string EnsureRepo(string repoUrl, string repoName, string? pinnedCommit = null)
    {
        var repoPath = Path.Combine(CacheRoot, repoName);

        RepoMutex.WaitOne(TimeSpan.FromMinutes(5));
        try
        {
            return EnsureRepoCore(repoUrl, repoName, repoPath, pinnedCommit);
        }
        finally
        {
            RepoMutex.ReleaseMutex();
        }
    }

    private static string EnsureRepoCore(string repoUrl, string repoName, string repoPath, string? pinnedCommit)
    {
        if (Directory.Exists(Path.Combine(repoPath, ".git")))
        {
            if (pinnedCommit != null)
            {
                var currentCommit = GetCurrentCommit(repoPath);
                if (currentCommit != null && currentCommit.StartsWith(pinnedCommit, StringComparison.Ordinal))
                    return repoPath;

                try
                {
                    Directory.Delete(repoPath, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // Pack files locked by another test class — repo is likely at the right commit
                    return repoPath;
                }
            }
            else
            {
                try
                {
                    RunGit(repoPath, "pull --ff-only");
                    return repoPath;
                }
                catch
                {
                    try
                    {
                        Directory.Delete(repoPath, true);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return repoPath;
                    }
                }
            }
        }

        Directory.CreateDirectory(CacheRoot);

        if (pinnedCommit != null)
        {
            RunGit(CacheRoot, $"clone {repoUrl} {repoName}");
            RunGit(repoPath, $"checkout {pinnedCommit}");
        }
        else
        {
            RunGit(CacheRoot, $"clone --depth 1 {repoUrl} {repoName}");
        }

        return repoPath;
    }

    private static string? GetCurrentCommit(string repoPath)
    {
        try
        {
            // Read HEAD directly to avoid git process needing pack file access
            var headPath = Path.Combine(repoPath, ".git", "HEAD");
            if (!File.Exists(headPath))
                return null;

            var headContent = File.ReadAllText(headPath).Trim();

            // Detached HEAD: contains the commit hash directly
            if (!headContent.StartsWith("ref:", StringComparison.Ordinal))
                return headContent;

            // Symbolic ref: resolve by reading the ref file
            var refPath = headContent.Substring(5).Trim();
            var refFile = Path.Combine(repoPath, ".git", refPath);
            return File.Exists(refFile) ? File.ReadAllText(refFile).Trim() : null;
        }
        catch
        {
            return null;
        }
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
            EnableSceneTypesProvider = true,
            EnableCallSiteRegistry = true
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
