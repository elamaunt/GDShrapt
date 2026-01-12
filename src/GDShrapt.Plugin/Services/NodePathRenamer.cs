using GDShrapt.Reader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin;

/// <summary>
/// Applies node path renaming changes to GDScript and scene files.
/// </summary>
internal class NodePathRenamer
{
    /// <summary>
    /// Applies all selected renaming changes.
    /// </summary>
    /// <param name="parameters">The renaming parameters from the dialog.</param>
    /// <param name="oldName">The original node name.</param>
    public void ApplyChanges(NodeRenamingParameters parameters, string oldName)
    {
        if (parameters?.SelectedReferences == null || string.IsNullOrEmpty(parameters.NewName))
            return;

        // Group references by file to avoid multiple writes
        var byFile = parameters.SelectedReferences.GroupBy(r => r.FilePath);

        foreach (var group in byFile)
        {
            var filePath = group.Key;
            if (string.IsNullOrEmpty(filePath))
                continue;

            var refs = group.ToList();
            var firstRef = refs.First();

            if (firstRef.Type == GDNodePathReference.RefType.GDScript)
            {
                // For GDScript, we modify the AST directly (already done via PathSpecifier)
                // The caller is responsible for saving the modified script
                RenameInGDScriptReferences(refs, parameters.NewName);
            }
            else
            {
                // For scene files, we do text-based replacement
                RenameInSceneFile(filePath, oldName, parameters.NewName);
            }
        }
    }

    /// <summary>
    /// Renames node references in GDScript by modifying the AST.
    /// </summary>
    private void RenameInGDScriptReferences(List<GDNodePathReference> references, string newName)
    {
        foreach (var reference in references)
        {
            if (reference.PathSpecifier != null)
            {
                // Modify the GDPathSpecifier directly in the AST
                reference.PathSpecifier.IdentifierValue = newName;
            }
        }

        // The modified scripts need to be saved by the caller
        // by updating controller.Text = @class.ToString();
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
            Logger.Error($"Scene file not found: {scenePath}");
            return;
        }

        try
        {
            var content = File.ReadAllText(scenePath);
            var modified = RenameInSceneContent(content, oldName, newName);

            if (content != modified)
            {
                File.WriteAllText(scenePath, modified);
                Logger.Info($"Updated scene file: {scenePath}");
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Failed to update scene file {scenePath}: {ex.Message}");
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
    public HashSet<GDScriptMap> GetModifiedScripts(IEnumerable<GDNodePathReference> references)
    {
        var scripts = new HashSet<GDScriptMap>();

        foreach (var reference in references)
        {
            if (reference.Type == GDNodePathReference.RefType.GDScript && reference.ScriptMap != null)
            {
                scripts.Add(reference.ScriptMap);
            }
        }

        return scripts;
    }
}
