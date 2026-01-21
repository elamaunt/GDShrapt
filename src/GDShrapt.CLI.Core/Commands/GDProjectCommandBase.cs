using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base class for project-oriented CLI commands that need to load a Godot project.
/// Provides common project loading, config handling, and error handling.
/// </summary>
public abstract class GDProjectCommandBase : IGDCommand
{
    protected readonly string _projectPath;
    protected readonly IGDOutputFormatter _formatter;
    protected readonly TextWriter _output;
    protected readonly GDProjectConfig? _config;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected GDProjectCommandBase(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);
            using var project = GDProjectLoader.LoadProject(projectRoot);

            return ExecuteOnProjectAsync(project, projectRoot, config, cancellationToken);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(GDExitCode.Fatal);
        }
    }

    /// <summary>
    /// Core execution logic. Override in derived classes.
    /// </summary>
    /// <param name="project">The loaded GDScript project.</param>
    /// <param name="projectRoot">Path to the project root directory.</param>
    /// <param name="config">Project configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code.</returns>
    protected abstract Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets relative path from full path and base path.
    /// </summary>
    protected static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
