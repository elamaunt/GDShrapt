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
                _formatter.WriteMessage(_output, $"[Dry run] Would rename {result.Edits.Count} occurrence(s) in {result.FileCount} file(s):");

                // Group edits by file for display
                var editsByFile = result.Edits.GroupBy(e => e.FilePath);
                foreach (var fileGroup in editsByFile)
                {
                    var relativePath = GetRelativePath(fileGroup.Key, projectRoot);
                    _formatter.WriteMessage(_output, $"  {relativePath}: {fileGroup.Count()} edit(s)");
                    foreach (var edit in fileGroup)
                    {
                        _formatter.WriteMessage(_output, $"    Line {edit.Line}: {edit.OldText} -> {edit.NewText}");
                    }
                }
            }
            else
            {
                // Apply the edits using GDRenameService
                var editsByFile = result.Edits.GroupBy(e => e.FilePath);
                foreach (var fileGroup in editsByFile)
                {
                    renameService.ApplyEditsToFile(fileGroup.Key, fileGroup);
                }

                _formatter.WriteMessage(_output, $"Renamed {result.Edits.Count} occurrence(s) in {result.FileCount} file(s).");
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
