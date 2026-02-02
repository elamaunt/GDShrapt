using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
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
    protected readonly IGDLogger _logger;

    /// <summary>
    /// Service registry for accessing CLI.Core handlers.
    /// Available after project is loaded in ExecuteOnProjectAsync.
    /// </summary>
    protected IGDServiceRegistry? Registry { get; private set; }

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected GDProjectCommandBase(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _logger = logger ?? GDNullLogger.Instance;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug($"Finding project root from: {_projectPath}");

            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            _logger.Debug($"Found project root: {projectRoot}");
            _logger.Info($"Loading project: {projectRoot}");

            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);
            using var project = GDProjectLoader.LoadProject(projectRoot, _logger);

            _logger.Debug($"Project loaded: {project.ScriptFiles.Count()} scripts");

            // Initialize service registry with base module
            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule());
            Registry = registry;

            return ExecuteOnProjectAsync(project, projectRoot, config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Command execution failed", ex);
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
