using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Renames a symbol across the project.
/// Uses GDRenameService from Semantics for rename logic.
/// </summary>
public class GDRenameCommand : IGDCommand
{
    private readonly string _oldName;
    private readonly string _newName;
    private readonly string? _filePath;
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly bool _dryRun;

    public string Name => "rename";
    public string Description => "Rename a symbol across the project";

    public GDRenameCommand(
        string oldName,
        string newName,
        string projectPath,
        string? filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        bool dryRun = false)
    {
        _oldName = oldName;
        _newName = newName;
        _projectPath = projectPath;
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _dryRun = dryRun;
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
                return Task.FromResult(2);
            }

            using var project = GDProjectLoader.LoadProject(projectRoot);

            // Use GDRenameService from Semantics
            var renameService = new GDRenameService(project);

            // Validate new name
            if (!renameService.ValidateIdentifier(_newName, out var validationError))
            {
                _formatter.WriteError(_output, validationError ?? $"Invalid identifier: {_newName}");
                return Task.FromResult(2);
            }

            // Plan the rename
            var result = renameService.PlanRename(_oldName, _newName, _filePath);

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
                return Task.FromResult(1);
            }

            if (result.Edits.Count == 0)
            {
                _formatter.WriteMessage(_output, $"No occurrences of '{_oldName}' found.");
                return Task.FromResult(0);
            }

            if (_dryRun)
            {
                // Show all edits in dry-run mode
                _formatter.WriteMessage(_output, $"[Dry run] Would rename {result.Edits.Count} occurrence(s) in {result.FileCount} file(s):");

                // Show strict edits
                if (result.StrictEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n  Strict references ({result.StrictEdits.Count}):");
                    var strictByFile = result.StrictEdits.GroupBy(e => e.FilePath);
                    foreach (var fileGroup in strictByFile)
                    {
                        var relativePath = GetRelativePath(fileGroup.Key, projectRoot);
                        _formatter.WriteMessage(_output, $"    {relativePath}:");
                        foreach (var edit in fileGroup)
                        {
                            _formatter.WriteMessage(_output, $"      Line {edit.Line}: {edit.OldText} -> {edit.NewText}");
                        }
                    }
                }

                // Show potential edits (Pro-only)
                if (result.PotentialEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n  Potential references ({result.PotentialEdits.Count}) [Pro only]:");
                    var potentialByFile = result.PotentialEdits.GroupBy(e => e.FilePath);
                    foreach (var fileGroup in potentialByFile)
                    {
                        var relativePath = GetRelativePath(fileGroup.Key, projectRoot);
                        _formatter.WriteMessage(_output, $"    {relativePath}:");
                        foreach (var edit in fileGroup)
                        {
                            _formatter.WriteMessage(_output, $"      Line {edit.Line}: {edit.OldText} -> {edit.NewText} ({edit.ConfidenceReason ?? "duck-typed"})");
                        }
                    }
                }
            }
            else
            {
                // Base CLI: Apply only Strict edits (Potential/NameMatch require Pro)
                if (result.StrictEdits.Count == 0)
                {
                    _formatter.WriteMessage(_output, $"No confirmed references found for '{_oldName}'.");
                    if (result.PotentialEdits.Count > 0)
                    {
                        _formatter.WriteMessage(_output, $"{result.PotentialEdits.Count} potential reference(s) found. Use GDShrapt Pro to apply them.");
                    }
                    return Task.FromResult(0);
                }

                // Apply Strict edits only
                var strictByFile = result.StrictEdits.GroupBy(e => e.FilePath);
                foreach (var fileGroup in strictByFile)
                {
                    renameService.ApplyEditsToFile(fileGroup.Key, fileGroup);
                }

                var strictFileCount = result.StrictEdits.Select(e => e.FilePath).Distinct().Count();
                _formatter.WriteMessage(_output, $"Renamed {result.StrictEdits.Count} confirmed reference(s) in {strictFileCount} file(s).");

                // Inform about Potential edits (Pro-only)
                if (result.PotentialEdits.Count > 0)
                {
                    _formatter.WriteMessage(_output, $"\n{result.PotentialEdits.Count} additional potential reference(s) found.");
                    _formatter.WriteMessage(_output, "Use GDShrapt Pro with --confidence=potential to apply them.");
                }
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
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
