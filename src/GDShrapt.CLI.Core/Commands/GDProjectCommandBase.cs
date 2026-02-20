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

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug($"Finding project root from: {_projectPath}");

            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}\n  Hint: Run from a Godot project directory, or specify the path: 'gdshrapt {Name} /path/to/project'.");
                return GDExitCode.Fatal;
            }

            _logger.Debug($"Found project root: {projectRoot}");
            _logger.Info($"Loading project: {projectRoot}");

            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);
            var projectOptions = GetProjectOptions();
            using var project = projectOptions != null
                ? GDProjectLoader.LoadProject(projectRoot, projectOptions)
                : GDProjectLoader.LoadProject(projectRoot, _logger);

            _logger.Debug($"Project loaded: {project.ScriptFiles.Count()} scripts");

            // Report files with parse errors
            var filesWithErrors = project.ScriptFiles.Where(f => f.WasReadError).ToList();
            if (filesWithErrors.Count > 0)
            {
                _formatter.WriteError(_output, $"Warning: {filesWithErrors.Count} file(s) had parse errors and were excluded from analysis:");
                foreach (var file in filesWithErrors)
                {
                    var relPath = GetRelativePath(file.FullPath ?? "", projectRoot);
                    _formatter.WriteError(_output, $"  {relPath}");
                }
                _formatter.WriteMessage(_output, "");
            }

            // Initialize service registry with base module
            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule());
            Registry = registry;

            return await ExecuteOnProjectAsync(project, projectRoot, config, cancellationToken);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Error("Directory not found", ex);
            _formatter.WriteError(_output, $"Directory not found: {ex.Message}\n  Hint: Verify the project path exists.");
            return GDExitCode.Fatal;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error("Permission denied", ex);
            _formatter.WriteError(_output, $"Permission denied: {ex.Message}\n  Hint: Check file/directory permissions.");
            return GDExitCode.Fatal;
        }
        catch (IOException ex)
        {
            _logger.Error("File access error", ex);
            _formatter.WriteError(_output, $"File access error: {ex.Message}\n  Hint: Check if files are locked by another process.");
            return GDExitCode.Fatal;
        }
        catch (Exception ex)
        {
            _logger.Error("Command execution failed", ex);
            _formatter.WriteError(_output, $"{ex.Message}\n  Hint: Use --debug for detailed diagnostic output.");
            return GDExitCode.Fatal;
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
    /// Override to provide custom project options (e.g., for watch mode with file watchers).
    /// Return null to use default options.
    /// </summary>
    protected virtual GDScriptProjectOptions? GetProjectOptions() => null;

    /// <summary>
    /// Gets relative path from full path and base path.
    /// </summary>
    protected static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch (ArgumentException)
        {
            return fullPath;
        }
    }
}
