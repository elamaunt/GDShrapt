namespace GDShrapt.Plugin;

/// <summary>
/// Handles navigation between the Type Flow panel and the Godot editor.
/// Provides synchronization so that clicking a node in the panel navigates to the source code.
/// </summary>
internal class GDTypeFlowNavigationService
{
    private readonly EditorInterface _editorInterface;
    private readonly GDScriptProject _project;

    /// <summary>
    /// Event fired when navigation to a node is requested.
    /// The panel subscribes to this to update its focus state.
    /// </summary>
    public event Action<GDTypeFlowNode> OnNodeNavigated;

    public GDTypeFlowNavigationService(EditorInterface editorInterface, GDScriptProject project)
    {
        _editorInterface = editorInterface;
        _project = project;
    }

    /// <summary>
    /// Navigates the editor to the source location of a type flow node.
    /// </summary>
    /// <param name="node">The node to navigate to.</param>
    public void NavigateToNode(GDTypeFlowNode node)
    {
        if (node?.Location == null || !node.Location.IsValid)
        {
            Logger.Debug("Cannot navigate: node or location is null/invalid");
            return;
        }

        NavigateToLocation(node.Location);
        OnNodeNavigated?.Invoke(node);
    }

    /// <summary>
    /// Navigates to a specific source location.
    /// </summary>
    /// <param name="location">The location to navigate to.</param>
    public void NavigateToLocation(GDSourceLocation location)
    {
        if (location == null || !location.IsValid)
            return;

        try
        {
            // Open the script file in the editor
            if (!OpenScriptInEditor(location.FilePath))
            {
                Logger.Warning($"Could not open script: {location.FilePath}");
                return;
            }

            // Get the script editor and set cursor/selection
            var scriptEditor = GetCurrentScriptEditor();
            if (scriptEditor != null)
            {
                // Set the cursor position
                scriptEditor.SetCaretLine(location.StartLine);
                scriptEditor.SetCaretColumn(location.StartColumn);

                // Select the token/range
                scriptEditor.Select(
                    location.StartLine,
                    location.StartColumn,
                    location.EndLine,
                    location.EndColumn
                );

                // Center the viewport on the selection
                scriptEditor.CenterViewportToCaret();

                Logger.Debug($"Navigated to {location}");
            }
            else
            {
                Logger.Debug("Script editor not available for selection");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Navigation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates to a symbol by name, searching in the project.
    /// </summary>
    /// <param name="symbolName">The name of the symbol to find.</param>
    /// <param name="preferredFile">Optional preferred file to search first.</param>
    /// <returns>True if navigation was successful.</returns>
    public bool NavigateToSymbol(string symbolName, GDScriptFile preferredFile = null)
    {
        if (string.IsNullOrEmpty(symbolName))
            return false;

        // Try preferred file first
        if (preferredFile != null)
        {
            var location = FindSymbolInScript(symbolName, preferredFile);
            if (location != null)
            {
                NavigateToLocation(location);
                return true;
            }
        }

        // Search in project
        if (_project != null)
        {
            foreach (var script in _project.ScriptFiles)
            {
                var location = FindSymbolInScript(symbolName, script);
                if (location != null)
                {
                    NavigateToLocation(location);
                    return true;
                }
            }
        }

        Logger.Debug($"Symbol not found: {symbolName}");
        return false;
    }

    /// <summary>
    /// Finds a symbol in a script and returns its location.
    /// </summary>
    private GDSourceLocation FindSymbolInScript(string symbolName, GDScriptFile script)
    {
        try
        {
            var semanticModel = script.SemanticModel;
            if (semanticModel == null)
            {
                script.Analyze();
                semanticModel = script.SemanticModel;
            }

            if (semanticModel == null)
                return null;

            var symbol = semanticModel.FindSymbol(symbolName);
            if (symbol?.DeclarationNode != null)
            {
                return GDSourceLocation.FromNode(symbol.DeclarationNode, script.FullPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error finding symbol in {script.FullPath}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Opens a script file in the Godot editor.
    /// </summary>
    /// <param name="filePath">The absolute path to the script file.</param>
    /// <returns>True if the script was opened successfully.</returns>
    private bool OpenScriptInEditor(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            return false;

        try
        {
            // Convert to Godot resource path if needed
            var resourcePath = ConvertToResourcePath(filePath);
            if (string.IsNullOrEmpty(resourcePath))
            {
                Logger.Debug($"Could not convert to resource path: {filePath}");
                return false;
            }

            var script = GD.Load<Script>(resourcePath);
            if (script != null)
            {
                _editorInterface.EditScript(script);
                return true;
            }
            else
            {
                Logger.Debug($"Could not load script: {resourcePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error opening script: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Converts an absolute file path to a Godot resource path (res://...).
    /// </summary>
    private string ConvertToResourcePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return null;

        // Normalize path separators
        absolutePath = absolutePath.Replace('\\', '/');

        // Get the project root
        var projectRoot = ProjectSettings.GlobalizePath("res://").Replace('\\', '/');

        if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = absolutePath.Substring(projectRoot.Length);
            return "res://" + relativePath;
        }

        // Try to find if it's already a res:// path
        if (absolutePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath;
        }

        Logger.Debug($"Path not in project: {absolutePath}");
        return null;
    }

    /// <summary>
    /// Gets the current script editor (CodeEdit) from the Godot editor.
    /// </summary>
    private CodeEdit GetCurrentScriptEditor()
    {
        try
        {
            var scriptEditor = _editorInterface.GetScriptEditor();
            if (scriptEditor == null)
                return null;

            var currentEditor = scriptEditor.GetCurrentEditor();
            if (currentEditor == null)
                return null;

            // The current editor is a ScriptEditorBase, we need to find the CodeEdit child
            return FindCodeEdit(currentEditor);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Error getting script editor: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Recursively finds a CodeEdit control in a node hierarchy.
    /// </summary>
    private CodeEdit FindCodeEdit(Node parent)
    {
        if (parent is CodeEdit codeEdit)
            return codeEdit;

        foreach (var child in parent.GetChildren())
        {
            var found = FindCodeEdit(child);
            if (found != null)
                return found;
        }

        return null;
    }
}
