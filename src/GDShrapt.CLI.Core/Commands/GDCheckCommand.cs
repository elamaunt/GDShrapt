using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Checks a GDScript project for errors.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// Designed for CI/CD pipelines.
/// Uses the unified GDDiagnosticsService for consistent diagnostics.
/// </summary>
public class GDCheckCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly bool _silent;
    private readonly GDProjectConfig? _config;

    public string Name => "check";
    public string Description => "Check a GDScript project for errors (for CI/CD)";

    public GDCheckCommand(string projectPath, IGDOutputFormatter formatter, TextWriter? output = null, bool silent = false, GDProjectConfig? config = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _silent = silent;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                if (!_silent)
                    _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}\n  Hint: Run from a Godot project directory, or specify the path: 'gdshrapt check /path/to/project'.");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var errorCount = 0;
            var warningCount = 0;
            var hintCount = 0;
            var fileCount = 0;

            var diagnosticsService = GDDiagnosticsService.FromConfig(config);

            foreach (var script in project.ScriptFiles)
            {
                var relativePath = Path.GetRelativePath(projectRoot, script.Reference.FullPath);

                if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                    continue;

                fileCount++;

                var result = diagnosticsService.Diagnose(script);

                errorCount += result.ErrorCount;
                warningCount += result.WarningCount;
                hintCount += result.HintCount;
            }

            var exitCode = GDExitCode.FromResults(
                errorCount,
                warningCount,
                hintCount,
                config.Cli.FailOnWarning,
                config.Cli.FailOnHint);

            if (!_silent)
            {
                if (exitCode == GDExitCode.Success)
                    _formatter.WriteMessage(_output, $"OK: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
                else
                    _formatter.WriteMessage(_output, $"FAILED: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
            }

            return Task.FromResult(exitCode);
        }
        catch (Exception ex)
        {
            if (!_silent)
                _formatter.WriteError(_output, $"{ex.Message}\n  Hint: Use --debug for detailed diagnostic output.");
            return Task.FromResult(GDExitCode.Fatal);
        }
    }
}
