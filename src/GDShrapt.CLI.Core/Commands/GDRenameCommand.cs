using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Renames a symbol across the project.
/// </summary>
public class GDRenameCommand : IGDCommand
{
    private readonly string? _oldName;
    private readonly string _newName;
    private readonly string? _filePath;
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly bool _dryRun;
    private readonly int? _line;
    private readonly int? _column;

    public string Name => "rename";
    public string Description => "Rename a symbol across the project";

    public GDRenameCommand(
        string? oldName,
        string newName,
        string projectPath,
        string? filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        bool dryRun = false,
        int? line = null,
        int? column = null)
    {
        _oldName = oldName;
        _newName = newName;
        _projectPath = projectPath;
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _dryRun = dryRun;
        _line = line;
        _column = column;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var searchPath = _filePath ?? _projectPath;
            var projectRoot = GDProjectLoader.FindProjectRoot(searchPath);

            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {searchPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule());
            var renameHandler = registry.GetService<IGDRenameHandler>();

            if (renameHandler == null)
            {
                _formatter.WriteError(_output, "Rename handler not available");
                return Task.FromResult(GDExitCode.Fatal);
            }

            // Resolve symbol name from position if --line is used
            string oldName;
            if (_line.HasValue)
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    _formatter.WriteError(_output, "The --file option is required when using --line.");
                    return Task.FromResult(GDExitCode.Fatal);
                }

                var resolved = renameHandler.ResolveSymbolAtPosition(_filePath, _line.Value, _column ?? 1);
                if (resolved == null)
                {
                    _formatter.WriteError(_output, $"No symbol found at line {_line.Value}, column {_column ?? 1}.");
                    return Task.FromResult(GDExitCode.Errors);
                }

                oldName = resolved;
            }
            else if (!string.IsNullOrEmpty(_oldName))
            {
                oldName = _oldName;
            }
            else
            {
                _formatter.WriteError(_output, "Specify a symbol name or use --line to identify by position.");
                return Task.FromResult(GDExitCode.Fatal);
            }

            if (!renameHandler.ValidateIdentifier(oldName, out var oldNameError))
            {
                _formatter.WriteError(_output, oldNameError ?? $"Invalid identifier: {oldName}");
                return Task.FromResult(GDExitCode.Errors);
            }

            if (!renameHandler.ValidateIdentifier(_newName, out var validationError))
            {
                _formatter.WriteError(_output, validationError ?? $"Invalid identifier: {_newName}");
                return Task.FromResult(GDExitCode.Errors);
            }

            var result = renameHandler.Plan(oldName, _newName, _filePath);

            if (!result.Success)
            {
                if (result.Conflicts.Count > 0)
                {
                    _formatter.WriteError(_output, "Rename would cause conflicts:");
                    foreach (var conflict in result.Conflicts)
                    {
                        _formatter.WriteError(_output, $"  - {conflict.Message}");
                    }
                }
                else
                {
                    _formatter.WriteError(_output, result.ErrorMessage ?? "Rename failed");
                }
                return Task.FromResult(GDExitCode.WarningsOrHints);
            }

            if (result.Edits.Count == 0)
            {
                _formatter.WriteMessage(_output, $"No occurrences of '{oldName}' found.");
                return Task.FromResult(GDExitCode.Success);
            }

            if (_dryRun)
            {
                _formatter.WriteMessage(_output, $"[Dry run] Would rename {result.Edits.Count} occurrence(s) in {result.FileCount} file(s):");

                if (result.StrictEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n  Strict references ({result.StrictEdits.Count}):");
                    foreach (var fileGroup in result.GetStrictEditsByFile())
                    {
                        var relativePath = Path.GetRelativePath(projectRoot, fileGroup.Key);
                        _formatter.WriteMessage(_output, $"    {relativePath}:");
                        foreach (var edit in fileGroup.Value)
                        {
                            _formatter.WriteMessage(_output, $"      Line {edit.Line}: {edit.OldText} -> {edit.NewText}");
                        }
                    }
                }

                if (result.PotentialEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n  Potential references ({result.PotentialEdits.Count}) [lower confidence, not applied]:");
                    foreach (var fileGroup in result.GetPotentialEditsByFile())
                    {
                        var relativePath = Path.GetRelativePath(projectRoot, fileGroup.Key);
                        _formatter.WriteMessage(_output, $"    {relativePath}:");
                        foreach (var edit in fileGroup.Value)
                        {
                            _formatter.WriteMessage(_output, $"      Line {edit.Line}: {edit.OldText} -> {edit.NewText} ({edit.ConfidenceReason ?? "duck-typed"})");
                        }
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n  Warnings ({result.Warnings.Count}):");
                    foreach (var warning in result.Warnings)
                    {
                        var relativePath = Path.GetRelativePath(projectRoot, warning.FilePath);
                        _formatter.WriteMessage(_output, $"    {relativePath}:{warning.Line}:{warning.Column} — {warning.Message}");
                    }
                }
            }
            else
            {
                // Base CLI: Apply only Strict edits (type-verified)
                if (result.StrictEdits.Count == 0)
                {
                    _formatter.WriteMessage(_output, $"No type-verified references found for '{oldName}'.");
                    if (result.PotentialEdits.Count > 0)
                    {
                        _formatter.WriteMessage(_output, $"{result.PotentialEdits.Count} duck-typed reference(s) found but not applied (lower confidence).");
                    }
                    return Task.FromResult(GDExitCode.Success);
                }

                var fileCount = renameHandler.ApplyEdits(result.StrictEdits);
                _formatter.WriteMessage(_output, $"Renamed {result.StrictEdits.Count} confirmed reference(s) in {fileCount} file(s).");

                if (result.PotentialEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n{result.PotentialEdits.Count} additional duck-typed reference(s) found but not applied (lower confidence).");
                }

                if (result.Warnings.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\nWarnings ({result.Warnings.Count}):");
                    foreach (var warning in result.Warnings)
                    {
                        var relativePath = Path.GetRelativePath(projectRoot, warning.FilePath);
                        _formatter.WriteMessage(_output, $"  {relativePath}:{warning.Line}:{warning.Column} — {warning.Message}");
                    }
                }
            }

            return Task.FromResult(GDExitCode.Success);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(GDExitCode.Errors);
        }
    }
}
