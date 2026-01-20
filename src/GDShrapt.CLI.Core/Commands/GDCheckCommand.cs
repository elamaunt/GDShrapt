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
    private readonly bool _quiet;
    private readonly GDProjectConfig? _config;

    public string Name => "check";
    public string Description => "Check a GDScript project for errors (for CI/CD)";

    public GDCheckCommand(string projectPath, IGDOutputFormatter formatter, TextWriter? output = null, bool quiet = false, GDProjectConfig? config = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _quiet = quiet;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                if (!_quiet)
                {
                    _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                }
                return Task.FromResult(GDExitCode.Fatal);
            }

            // Load config from project or use provided
            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var errorCount = 0;
            var warningCount = 0;
            var hintCount = 0;
            var fileCount = 0;

            // Use unified diagnostics service - handles validator, linter, and config consistently
            var diagnosticsService = GDDiagnosticsService.FromConfig(config);

            foreach (var script in project.ScriptFiles)
            {
                var relativePath = Path.GetRelativePath(projectRoot, script.Reference.FullPath);

                // Check if file should be excluded
                if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                    continue;

                fileCount++;

                // Use unified diagnostics service - handles parse errors, invalid tokens,
                // validation, linting, config overrides, and comment suppression
                var result = diagnosticsService.Diagnose(script);

                errorCount += result.ErrorCount;
                warningCount += result.WarningCount;
                hintCount += result.HintCount;
            }

            // Determine exit code using new exit code system
            var exitCode = GDExitCode.FromResults(
                errorCount,
                warningCount,
                hintCount,
                config.Cli.FailOnWarning,
                config.Cli.FailOnHint);

            if (!_quiet)
            {
                if (exitCode == GDExitCode.Success)
                {
                    _formatter.WriteMessage(_output, $"OK: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
                }
                else
                {
                    _formatter.WriteMessage(_output, $"FAILED: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
                }
            }

            return Task.FromResult(exitCode);
        }
        catch (Exception ex)
        {
            if (!_quiet)
            {
                _formatter.WriteError(_output, ex.Message);
            }
            return Task.FromResult(GDExitCode.Fatal);
        }
    }
}
