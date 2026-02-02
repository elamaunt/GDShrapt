using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of a node path rename operation.
/// </summary>
public sealed class GDNodeRenameResult
{
    /// <summary>
    /// Whether the rename was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// List of files that were modified.
    /// </summary>
    public IReadOnlyList<string> ModifiedFiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// List of GDScript files whose ASTs were modified (need saving).
    /// </summary>
    public IReadOnlyList<GDScriptReference> ModifiedScripts { get; init; } = Array.Empty<GDScriptReference>();

    /// <summary>
    /// Number of edits applied.
    /// </summary>
    public int EditCount { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static GDNodeRenameResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };

    public static GDNodeRenameResult Empty() =>
        new() { Success = true, EditCount = 0 };
}

/// <summary>
/// Applies node path renaming changes to GDScript and scene files.
/// </summary>
public class GDNodePathRenamer
{
    private readonly GDScriptProject _project;
    private readonly IGDLogger _logger;

    public GDNodePathRenamer(GDScriptProject project)
    {
        _project = project;
        _logger = project.Logger;
    }

    /// <summary>
    /// Applies renaming changes to selected references.
    /// </summary>
    /// <param name="references">References to rename.</param>
    /// <param name="oldName">The original node name.</param>
    /// <param name="newName">The new node name.</param>
    /// <returns>Result of the rename operation.</returns>
    public GDNodeRenameResult ApplyRename(
        IEnumerable<GDNodePathReference> references,
        string oldName,
        string newName)
    {
        if (string.IsNullOrEmpty(newName))
            return GDNodeRenameResult.Failed("New name cannot be empty");

        if (oldName == newName)
            return GDNodeRenameResult.Empty();

        var modifiedFiles = new HashSet<string>();
        var modifiedScripts = new HashSet<GDScriptReference>();
        int editCount = 0;

        // Group references by file to avoid multiple writes
        var byFile = new Dictionary<string, List<GDNodePathReference>>();
        foreach (var r in references)
        {
            if (string.IsNullOrEmpty(r.FilePath))
                continue;

            if (!byFile.TryGetValue(r.FilePath, out var list))
            {
                list = new List<GDNodePathReference>();
                byFile[r.FilePath] = list;
            }
            list.Add(r);
        }

        foreach (var (filePath, refs) in byFile)
        {
            var firstRef = refs[0];

            if (firstRef.Type == GDNodePathReference.RefType.GDScript)
            {
                // For GDScript, modify the AST directly via PathSpecifier
                foreach (var reference in refs)
                {
                    if (reference.PathSpecifier != null)
                    {
                        reference.PathSpecifier.IdentifierValue = newName;
                        editCount++;
                    }

                    if (reference.ScriptReference != null)
                    {
                        modifiedScripts.Add(reference.ScriptReference);
                    }
                }
                modifiedFiles.Add(filePath);
            }
            else
            {
                // For scene files, do text-based replacement
                RenameInSceneFile(filePath, oldName, newName);
                editCount += refs.Count;
                modifiedFiles.Add(filePath);
            }
        }

        return new GDNodeRenameResult
        {
            Success = true,
            ModifiedFiles = new List<string>(modifiedFiles),
            ModifiedScripts = new List<GDScriptReference>(modifiedScripts),
            EditCount = editCount
        };
    }

    /// <summary>
    /// Renames node references in a scene file using regex replacement.
    /// </summary>
    /// <param name="scenePath">Full path to the scene file.</param>
    /// <param name="oldName">Original node name.</param>
    /// <param name="newName">New node name.</param>
    public void RenameInSceneFile(string scenePath, string oldName, string newName)
    {
        if (!File.Exists(scenePath))
        {
            _logger.Error($"Scene file not found: {scenePath}");
            return;
        }

        try
        {
            // Mark own write so FileWatcher doesn't trigger events
            _project.SceneTypesProvider?.MarkOwnWrite();

            var content = File.ReadAllText(scenePath);
            var modified = RenameInSceneContent(content, oldName, newName);

            if (content != modified)
            {
                File.WriteAllText(scenePath, modified);
                _logger.Info($"Updated scene file: {scenePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update scene file {scenePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Renames node references in scene file content.
    /// </summary>
    /// <param name="content">The scene file content.</param>
    /// <param name="oldName">Original node name.</param>
    /// <param name="newName">New node name.</param>
    /// <returns>Modified content.</returns>
    public string RenameInSceneContent(string content, string oldName, string newName)
    {
        var escapedOld = Regex.Escape(oldName);
        var result = content;

        // 1. Replace node name declaration: [node name="OldName" ...]
        result = Regex.Replace(result,
            $@"(\[node\s+name=""){escapedOld}("")",
            $"$1{newName}$2");

        // 2. Replace exact parent path: parent="OldName"
        result = Regex.Replace(result,
            $@"(parent=""){escapedOld}("")",
            $"$1{newName}$2");

        // 3. Replace parent path prefix: parent="OldName/..."
        result = Regex.Replace(result,
            $@"(parent=""){escapedOld}/",
            $"$1{newName}/");

        // 4. Replace parent path middle: parent=".../OldName/..."
        result = Regex.Replace(result,
            $@"(parent=""[^""]*)/({escapedOld})/",
            $"$1/{newName}/");

        // 5. Replace parent path suffix: parent=".../OldName"
        result = Regex.Replace(result,
            $@"(parent=""[^""]*)/({escapedOld})("")",
            $"$1/{newName}$3");

        return result;
    }

    /// <summary>
    /// Gets the set of GDScript files that were modified and need to be saved.
    /// </summary>
    public HashSet<GDScriptFile> GetModifiedScripts(IEnumerable<GDNodePathReference> references)
    {
        var scripts = new HashSet<GDScriptFile>();

        foreach (var reference in references)
        {
            if (reference.Type == GDNodePathReference.RefType.GDScript &&
                reference.ScriptReference != null)
            {
                var script = _project.GetScript(reference.ScriptReference);
                if (script != null)
                {
                    scripts.Add(script);
                }
            }
        }

        return scripts;
    }

    /// <summary>
    /// Saves modified GDScript files to disk.
    /// </summary>
    public void SaveModifiedScripts(IEnumerable<GDScriptFile> scripts)
    {
        foreach (var script in scripts)
        {
            if (script.Class == null)
                continue;

            try
            {
                var content = script.Class.ToString();
                File.WriteAllText(script.Reference.FullPath, content);
                _logger.Info($"Saved script: {script.Reference.FullPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save script {script.Reference.FullPath}: {ex.Message}");
            }
        }
    }
}
