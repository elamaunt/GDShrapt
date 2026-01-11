using Godot;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// AST Viewer dock for visualizing the syntax tree of GDScript files.
/// Displayed in the bottom panel.
/// </summary>
internal partial class AstViewerDock : Control
{
    private GDShraptPlugin _plugin;
    private GDProjectMap _projectMap;

    // UI controls
    private OptionButton _scriptSelector;
    private Button _refreshButton;
    private Button _parseCurrentButton;
    private Tree _astTree;
    private RichTextLabel _nodeDetails;
    private CheckButton _showTokensToggle;
    private Label _statusLabel;

    // Current state
    private GDScriptMap _currentScript;
    private Dictionary<TreeItem, GDNode> _treeItemToNode = new();

    /// <summary>
    /// Event fired when user wants to navigate to a code location.
    /// </summary>
    public event Action<string, int, int> NavigateToCode;

    private bool _initialized = false;

    public override void _Ready()
    {
        CreateUI();

        // If Initialize was called before _Ready, populate now
        if (_initialized)
        {
            PopulateScriptSelector();
        }
    }

    public void Initialize(GDShraptPlugin plugin, GDProjectMap projectMap)
    {
        _plugin = plugin;
        _projectMap = projectMap;
        _initialized = true;

        // Only populate if UI is ready
        if (_scriptSelector != null)
        {
            PopulateScriptSelector();
        }
    }

    private void CreateUI()
    {
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Toolbar
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 6);
        mainVBox.AddChild(toolbar);

        // Script selector
        toolbar.AddChild(new Label { Text = "Script:" });
        _scriptSelector = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0)
        };
        _scriptSelector.ItemSelected += OnScriptSelected;
        toolbar.AddChild(_scriptSelector);

        // Parse current script button
        _parseCurrentButton = new Button
        {
            Text = "Parse Current",
            TooltipText = "Parse the currently active script in the editor"
        };
        _parseCurrentButton.Pressed += OnParseCurrentPressed;
        toolbar.AddChild(_parseCurrentButton);

        // Refresh button
        _refreshButton = new Button
        {
            Text = "",
            TooltipText = "Refresh script list"
        };
        try
        {
            _refreshButton.Icon = EditorInterface.Singleton.GetBaseControl().GetThemeIcon("Reload", "EditorIcons");
        }
        catch { }
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);

        // Show tokens toggle
        _showTokensToggle = new CheckButton
        {
            Text = "Tokens",
            TooltipText = "Include token-level nodes in the tree",
            ButtonPressed = false
        };
        _showTokensToggle.Toggled += OnShowTokensToggled;
        toolbar.AddChild(_showTokensToggle);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        toolbar.AddChild(spacer);

        // Status label
        _statusLabel = new Label
        {
            Text = "Select a script"
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        toolbar.AddChild(_statusLabel);

        // Split container for tree and details
        var splitContainer = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        mainVBox.AddChild(splitContainer);

        // Left side: AST Tree
        _astTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HideRoot = false,
            SelectMode = Tree.SelectModeEnum.Single,
            CustomMinimumSize = new Vector2(300, 0)
        };
        _astTree.ItemSelected += OnTreeItemSelected;
        _astTree.ItemActivated += OnTreeItemActivated;
        splitContainer.AddChild(_astTree);

        // Right side: Node details
        var detailsScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(250, 0)
        };
        splitContainer.AddChild(detailsScroll);

        _nodeDetails = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _nodeDetails.AddThemeColorOverride("default_color", new Color(0.9f, 0.9f, 0.9f));
        detailsScroll.AddChild(_nodeDetails);

        // Set initial split offset
        splitContainer.SplitOffset = 350;
    }

    private void PopulateScriptSelector()
    {
        _scriptSelector.Clear();
        _scriptSelector.AddItem("-- Select --", 0);

        if (_projectMap == null)
            return;

        var scripts = _projectMap.Scripts.OrderBy(s => s.Reference.FullPath).ToList();
        int index = 1;
        foreach (var script in scripts)
        {
            var fileName = System.IO.Path.GetFileName(script.Reference.FullPath);
            _scriptSelector.AddItem(fileName, index);
            _scriptSelector.SetItemMetadata(index, script.Reference.FullPath);
            index++;
        }

        _statusLabel.Text = $"{scripts.Count} scripts";
    }

    private void OnScriptSelected(long index)
    {
        if (index == 0)
        {
            _currentScript = null;
            _astTree.Clear();
            _nodeDetails.Text = "";
            return;
        }

        var path = _scriptSelector.GetItemMetadata((int)index).AsString();
        if (string.IsNullOrEmpty(path))
            return;

        var script = _projectMap?.Scripts.FirstOrDefault(s => s.Reference.FullPath == path);
        if (script != null)
        {
            _currentScript = script;
            BuildAstTree();
        }
    }

    private void OnParseCurrentPressed()
    {
        var scriptEditor = EditorInterface.Singleton.GetScriptEditor();
        var currentScript = scriptEditor?.GetCurrentScript();

        if (currentScript != null)
        {
            var resourcePath = currentScript.ResourcePath;
            var fullPath = ProjectSettings.GlobalizePath(resourcePath);

            var script = _projectMap?.Scripts.FirstOrDefault(s => s.Reference.FullPath == fullPath);
            if (script != null)
            {
                _currentScript = script;

                // Find and select in dropdown
                for (int i = 0; i < _scriptSelector.ItemCount; i++)
                {
                    if (_scriptSelector.GetItemMetadata(i).AsString() == fullPath)
                    {
                        _scriptSelector.Select(i);
                        break;
                    }
                }

                BuildAstTree();
            }
            else
            {
                _statusLabel.Text = "Script not in project";
            }
        }
        else
        {
            _statusLabel.Text = "No script open";
        }
    }

    private void OnRefreshPressed()
    {
        PopulateScriptSelector();
        if (_currentScript != null)
        {
            BuildAstTree();
        }
    }

    private void OnShowTokensToggled(bool pressed)
    {
        if (_currentScript != null)
        {
            BuildAstTree();
        }
    }

    private void BuildAstTree()
    {
        _astTree.Clear();
        _treeItemToNode.Clear();
        _nodeDetails.Text = "";

        if (_currentScript?.Class == null)
        {
            _statusLabel.Text = "No AST available";
            return;
        }

        var root = _astTree.CreateItem();
        root.SetText(0, $"GDClassDeclaration ({_currentScript.TypeName})");
        root.SetCustomColor(0, new Color(0.4f, 0.8f, 1.0f));
        _treeItemToNode[root] = _currentScript.Class;

        try
        {
            AddNodeToTree(root, _currentScript.Class);
            _statusLabel.Text = $"{_treeItemToNode.Count} nodes";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            Logger.Error($"AstViewerDock: Error building tree: {ex}");
        }
    }

    private void AddNodeToTree(TreeItem parent, GDNode node)
    {
        bool showTokens = _showTokensToggle.ButtonPressed;

        foreach (var child in node.AllNodes)
        {
            if (!showTokens && IsTokenNode(child))
                continue;

            var item = _astTree.CreateItem(parent);
            var nodeType = child.GetType().Name;
            var preview = GetNodePreview(child);

            if (!string.IsNullOrEmpty(preview))
            {
                item.SetText(0, $"{nodeType}: {preview}");
            }
            else
            {
                item.SetText(0, nodeType);
            }

            item.SetCustomColor(0, GetNodeColor(child));
            _treeItemToNode[item] = child;

            AddNodeToTree(item, child);
        }
    }

    private bool IsTokenNode(GDNode node)
    {
        var typeName = node.GetType().Name;
        return typeName.EndsWith("Token") ||
               typeName.StartsWith("GD") && typeName.Length > 2 && char.IsLower(typeName[2]);
    }

    private string GetNodePreview(GDNode node)
    {
        var preview = node switch
        {
            GDMethodDeclaration method => method.Identifier?.Sequence ?? "anonymous",
            GDVariableDeclaration variable => variable.Identifier?.Sequence ?? "anonymous",
            GDIfStatement _ => "if",
            GDForStatement forStmt => $"for {forStmt.Variable?.Sequence}",
            GDWhileStatement _ => "while",
            GDMatchStatement _ => "match",
            GDReturnExpression _ => "return",
            GDCallExpression call => call.CallerExpression?.ToString() ?? "call",
            GDBoolExpression b => b.Value.ToString().ToLowerInvariant(),
            GDStringNode str => TruncateString(str.ToString(), 30),
            GDNumberExpression num => num.Number?.Sequence ?? "",
            GDIdentifierExpression idExpr => idExpr.Identifier?.Sequence ?? "",
            _ => ""
        };

        return preview;
    }

    private string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return "";

        str = str.Replace("\n", "\\n").Replace("\r", "");

        if (str.Length > maxLength)
            return str.Substring(0, maxLength) + "...";

        return str;
    }

    private Color GetNodeColor(GDNode node)
    {
        return node switch
        {
            GDMethodDeclaration => new Color(0.4f, 0.8f, 0.4f),
            GDVariableDeclaration => new Color(0.6f, 0.8f, 1.0f),
            GDClassMember => new Color(0.8f, 0.6f, 0.4f),
            GDIfStatement or GDForStatement or GDWhileStatement or GDMatchStatement => new Color(0.8f, 0.4f, 0.8f),
            GDIdentifierExpression => new Color(0.6f, 0.9f, 0.9f),
            GDCallExpression => new Color(1.0f, 0.7f, 0.3f),
            GDExpression => new Color(1.0f, 0.8f, 0.4f),
            GDStatement => new Color(0.9f, 0.9f, 0.9f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }

    private void OnTreeItemSelected()
    {
        var selected = _astTree.GetSelected();
        if (selected == null)
            return;

        if (_treeItemToNode.TryGetValue(selected, out var node))
        {
            ShowNodeDetails(node);
        }
    }

    private void OnTreeItemActivated()
    {
        var selected = _astTree.GetSelected();
        if (selected == null)
            return;

        if (_treeItemToNode.TryGetValue(selected, out var node))
        {
            if (_currentScript != null)
            {
                NavigateToCode?.Invoke(
                    _currentScript.Reference.ResourcePath,
                    node.StartLine,
                    node.StartColumn
                );
            }
        }
    }

    private void ShowNodeDetails(GDNode node)
    {
        var details = new System.Text.StringBuilder();

        details.AppendLine($"[b]Type:[/b] {node.GetType().Name}");
        details.AppendLine();

        details.AppendLine("[b]Location:[/b]");
        details.AppendLine($"  Line {node.StartLine + 1}:{node.StartColumn} - {node.EndLine + 1}:{node.EndColumn}");
        details.AppendLine();

        var sourceText = node.ToString();
        if (!string.IsNullOrEmpty(sourceText))
        {
            var truncated = TruncateString(sourceText, 300);
            details.AppendLine("[b]Source:[/b]");
            details.AppendLine($"[code]{EscapeBBCode(truncated)}[/code]");
            details.AppendLine();
        }

        AddTypeSpecificDetails(details, node);

        var childCount = node.AllNodes.Count();
        details.AppendLine($"[b]Children:[/b] {childCount}");

        _nodeDetails.Text = details.ToString();
    }

    private void AddTypeSpecificDetails(System.Text.StringBuilder details, GDNode node)
    {
        switch (node)
        {
            case GDMethodDeclaration method:
                details.AppendLine($"[b]Method:[/b] {method.Identifier?.Sequence}");
                details.AppendLine($"  Params: {method.Parameters?.Count ?? 0}");
                if (method.ReturnType != null)
                    details.AppendLine($"  Returns: {method.ReturnType}");
                details.AppendLine();
                break;

            case GDVariableDeclaration variable:
                details.AppendLine($"[b]Variable:[/b] {variable.Identifier?.Sequence}");
                if (variable.Type != null)
                    details.AppendLine($"  Type: {variable.Type}");
                details.AppendLine();
                break;

            case GDIdentifierExpression idExpr:
                details.AppendLine($"[b]Identifier:[/b] {idExpr.Identifier?.Sequence}");
                details.AppendLine();
                break;

            case GDCallExpression call:
                details.AppendLine($"[b]Call:[/b] {call.CallerExpression}");
                details.AppendLine($"  Args: {call.Parameters?.Count ?? 0}");
                details.AppendLine();
                break;
        }
    }

    private string EscapeBBCode(string text)
    {
        return text.Replace("[", "[lb]").Replace("]", "[rb]");
    }
}
