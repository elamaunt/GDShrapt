using static Godot.Control;

namespace GDShrapt.Plugin;

/// <summary>
/// Type Flow Explorer panel - visualizes type inference as an interactive canvas-based graph.
/// Shows how types flow through the code: where they come from (inflows) and where they go (outflows).
/// Supports multi-level graphs, union type expansion, duck type constraints, and drag-to-pan navigation.
/// </summary>
internal partial class GDTypeFlowPanel : AcceptDialog
{
    // Services
    private GDScriptProject _project;
    private GDTypeFlowGraphBuilder _graphBuilder;
    private GDTypeFlowNavigationService _navigationService;
    private EditorInterface _editorInterface;

    // UI containers
    private VBoxContainer _mainContainer;
    private HBoxContainer _headerRow;
    private Button _backButton;
    private Label _titleLabel;
    private Button _fitButton;
    private Button _exportButton;

    // Signature section
    private PanelContainer _signaturePanel;
    private RichTextLabel _signatureLabel;

    // Canvas-based graph area
    private GDTypeFlowCanvas _canvas;
    private GDConstraintTooltip _constraintTooltip;

    // Action bar
    private HBoxContainer _actionBar;
    private Label _suggestionLabel;
    private Button _quickFixButton;

    // State
    private GDScriptFile _currentScript;
    private string _currentSymbolName;
    private GDTypeFlowNode _rootNode;
    private GDTypeFlowNode _focusedNode;
    private Stack<GDTypeFlowNode> _navigationStack = new();
    private bool _uiCreated;

    // Colors
    private static readonly Color KeywordColor = new(0.8f, 0.55f, 0.75f);   // Pink
    private static readonly Color TypeColor = new(0.4f, 0.76f, 0.65f);       // Cyan
    private static readonly Color ParamColor = new(0.85f, 0.85f, 0.75f);     // Light cream
    private static readonly Color SymbolColor = new(0.6f, 0.6f, 0.6f);       // Gray

    /// <summary>
    /// Event fired when user clicks on a file location to navigate.
    /// </summary>
    public event Action<string, int> NavigateToRequested;

    public GDTypeFlowPanel()
    {
        Title = "Type Flow";
        Size = new Vector2I(600, 500);
        Exclusive = false;
        Transient = true;
        OkButtonText = "Close";
    }

    public override void _Ready()
    {
        EnsureUICreated();
    }

    /// <summary>
    /// Initializes the panel with required dependencies.
    /// </summary>
    public void Initialize(GDScriptProject project, EditorInterface editorInterface)
    {
        _project = project;
        _editorInterface = editorInterface;
        _graphBuilder = new GDTypeFlowGraphBuilder(project);
        _navigationService = new GDTypeFlowNavigationService(editorInterface, project);
    }

    /// <summary>
    /// Sets the project (for backward compatibility).
    /// </summary>
    public void SetProject(GDScriptProject project)
    {
        _project = project;
        _graphBuilder = new GDTypeFlowGraphBuilder(project);
    }

    /// <summary>
    /// Shows type flow information for a symbol.
    /// </summary>
    public void ShowForSymbol(string symbolName, int line, GDScriptFile scriptFile)
    {
        EnsureUICreated();

        _currentSymbolName = symbolName;
        _currentScript = scriptFile;

        // Update window title
        Title = $"Type Flow: {symbolName}";
        _titleLabel.Text = symbolName;

        // Update back button visibility
        _backButton.Visible = _navigationStack.Count > 0;

        if (_project == null || scriptFile == null)
        {
            ShowNoDataMessage("No project or script available");
            return;
        }

        // Build the graph
        _rootNode = _graphBuilder.BuildGraph(symbolName, scriptFile);
        if (_rootNode == null)
        {
            ShowNoDataMessage($"Symbol '{symbolName}' not found");
            return;
        }

        // Display the graph with root as focus
        DisplayGraph(_rootNode);
    }

    private void EnsureUICreated()
    {
        if (_uiCreated)
            return;

        _uiCreated = true;
        CreateUI();
    }

    private void CreateUI()
    {
        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 8);
        AddChild(_mainContainer);

        CreateHeader();
        CreateSignatureSection();
        CreateCanvasArea();
        CreateActionBar();
    }

    private void CreateHeader()
    {
        _headerRow = new HBoxContainer();
        _headerRow.AddThemeConstantOverride("separation", 8);

        _backButton = new Button
        {
            Text = "←",
            Visible = false,
            TooltipText = "Go back to previous symbol"
        };
        _backButton.AddThemeFontSizeOverride("font_size", 16);
        _backButton.Pressed += OnBackPressed;
        _headerRow.AddChild(_backButton);

        _titleLabel = new Label
        {
            Text = "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        _headerRow.AddChild(_titleLabel);

        _fitButton = new Button
        {
            Text = "Fit",
            TooltipText = "Fit all nodes in view"
        };
        _fitButton.Pressed += OnFitPressed;
        _headerRow.AddChild(_fitButton);

        _exportButton = new Button { Text = "Export" };
        _exportButton.Pressed += OnExportPressed;
        _headerRow.AddChild(_exportButton);

        _mainContainer.AddChild(_headerRow);
        _mainContainer.AddChild(new HSeparator());
    }

    private void CreateSignatureSection()
    {
        _signaturePanel = new PanelContainer();
        var signatureStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.14f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        _signaturePanel.AddThemeStyleboxOverride("panel", signatureStyle);

        _signatureLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SelectionEnabled = false,
            CustomMinimumSize = new Vector2(0, 30)
        };
        _signatureLabel.MetaClicked += OnSignatureMetaClicked;
        _signaturePanel.AddChild(_signatureLabel);

        _mainContainer.AddChild(_signaturePanel);
    }

    private void CreateCanvasArea()
    {
        // Canvas for the graph (replaces scroll + flow containers)
        _canvas = new GDTypeFlowCanvas
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(400, 300)
        };

        // Wire up canvas events
        _canvas.BlockBodyClicked += OnBlockBodyClicked;
        _canvas.BlockLabelClicked += OnBlockLabelClicked;
        _canvas.EdgeClicked += OnEdgeClicked;
        _canvas.EdgeHoverStart += OnEdgeHoverStart;
        _canvas.EdgeHoverEnd += OnEdgeHoverEnd;

        _mainContainer.AddChild(_canvas);

        // Constraint tooltip (overlays on canvas)
        _constraintTooltip = new GDConstraintTooltip();
        AddChild(_constraintTooltip);
    }

    private void CreateActionBar()
    {
        _mainContainer.AddChild(new HSeparator());

        _actionBar = new HBoxContainer();
        _actionBar.AddThemeConstantOverride("separation", 8);

        _suggestionLabel = new Label
        {
            Text = "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false
        };
        _suggestionLabel.AddThemeFontSizeOverride("font_size", 11);
        _suggestionLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.6f, 0.9f));
        _actionBar.AddChild(_suggestionLabel);

        _quickFixButton = new Button
        {
            Text = "Add type annotation",
            Visible = false
        };
        _quickFixButton.Pressed += OnQuickFixPressed;
        _actionBar.AddChild(_quickFixButton);

        _mainContainer.AddChild(_actionBar);
    }

    private void DisplayGraph(GDTypeFlowNode focusNode)
    {
        _focusedNode = focusNode;

        // Update signature
        UpdateSignature(focusNode);

        // Display graph on canvas
        _canvas.DisplayGraph(focusNode);

        // Update suggestion
        UpdateSuggestion(focusNode);
    }

    private void UpdateSignature(GDTypeFlowNode node)
    {
        if (node?.SourceScript == null)
        {
            _signatureLabel.Text = $"[color=#cccccc]{node?.Label ?? "Unknown"}[/color]";
            return;
        }

        var analyzer = node.SourceScript.Analyzer;
        if (analyzer == null)
        {
            // Still try to show node info without analyzer
            if (IsExpressionNode(node.Kind))
            {
                _signatureLabel.Text = BuildNodeSignatureBBCode(node);
            }
            else
            {
                _signatureLabel.Text = $"[color=#cccccc]{node.Label}: {node.Type}[/color]";
            }
            return;
        }

        var symbol = analyzer.FindSymbol(node.Label);
        if (symbol?.DeclarationNode == null)
        {
            // Symbol not found - check if this is an expression node
            if (IsExpressionNode(node.Kind))
            {
                _signatureLabel.Text = BuildNodeSignatureBBCode(node);
            }
            else
            {
                _signatureLabel.Text = $"[color=#cccccc]{node.Label}: {node.Type}[/color]";
            }
            return;
        }

        // Build BBCode signature
        _signatureLabel.Text = BuildSignatureBBCode(symbol, analyzer);
    }

    private string BuildSignatureBBCode(GDShrapt.Semantics.GDSymbolInfo symbol, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var decl = symbol.DeclarationNode;

        if (decl is GDShrapt.Reader.GDMethodDeclaration method)
        {
            return BuildMethodSignatureBBCode(method, analyzer);
        }

        if (decl is GDShrapt.Reader.GDVariableDeclaration variable)
        {
            return BuildVariableSignatureBBCode(variable, analyzer);
        }

        if (decl is GDShrapt.Reader.GDParameterDeclaration param)
        {
            return BuildParameterSignatureBBCode(param, analyzer);
        }

        // For non-standard nodes (method calls, indexers, etc.) - use node-based signature
        if (_focusedNode != null && IsExpressionNode(_focusedNode.Kind))
        {
            return BuildNodeSignatureBBCode(_focusedNode);
        }

        var typeStr = analyzer.GetTypeForNode(decl) ?? "Variant";
        return $"[color=#{ColorToHex(ParamColor)}]{symbol.Name}[/color]: [color=#{ColorToHex(TypeColor)}]{typeStr}[/color]";
    }

    /// <summary>
    /// Checks if the node kind represents an expression rather than a declaration.
    /// </summary>
    private bool IsExpressionNode(GDTypeFlowNodeKind kind)
    {
        return kind == GDTypeFlowNodeKind.MethodCall ||
               kind == GDTypeFlowNodeKind.IndexerAccess ||
               kind == GDTypeFlowNodeKind.PropertyAccess ||
               kind == GDTypeFlowNodeKind.TypeCheck ||
               kind == GDTypeFlowNodeKind.NullCheck ||
               kind == GDTypeFlowNodeKind.Comparison ||
               kind == GDTypeFlowNodeKind.ReturnValue ||
               kind == GDTypeFlowNodeKind.Literal ||
               kind == GDTypeFlowNodeKind.Assignment;
    }

    private string BuildMethodSignatureBBCode(GDShrapt.Reader.GDMethodDeclaration method, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"[color=#{ColorToHex(KeywordColor)}]func[/color] ");

        var methodName = method.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{methodName}[/color]");

        sb.Append($"[color=#{ColorToHex(SymbolColor)}]([/color]");

        var parameters = method.Parameters?.ToList() ?? new List<GDShrapt.Reader.GDParameterDeclaration>();
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramName = param.Identifier?.Sequence ?? $"arg{i}";
            var paramType = analyzer.GetTypeForNode(param) ?? "Variant";

            // Make parameter clickable
            sb.Append($"[url={paramName}][color=#{ColorToHex(ParamColor)}]{paramName}[/color][/url]");
            sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
            sb.Append($"[color=#{ColorToHex(TypeColor)}]{paramType}[/color]");

            if (i < parameters.Count - 1)
            {
                sb.Append($"[color=#{ColorToHex(SymbolColor)}], [/color]");
            }
        }

        sb.Append($"[color=#{ColorToHex(SymbolColor)}])[/color]");

        var returnType = analyzer.GetTypeForNode(method) ?? "void";
        sb.Append($" [color=#{ColorToHex(SymbolColor)}]->[/color] ");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{returnType}[/color]");

        return sb.ToString();
    }

    private string BuildVariableSignatureBBCode(GDShrapt.Reader.GDVariableDeclaration variable, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"[color=#{ColorToHex(KeywordColor)}]var[/color] ");

        var varName = variable.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{varName}[/color]");

        var varType = analyzer.GetTypeForNode(variable) ?? "Variant";
        sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{varType}[/color]");

        // Show union type if present
        if (_focusedNode?.IsUnionType == true && _focusedNode.UnionTypeInfo != null)
        {
            var unionTypes = string.Join("|", _focusedNode.UnionTypeInfo.Types.Take(3));
            if (_focusedNode.UnionTypeInfo.Types.Count > 3)
                unionTypes += "...";
            sb.Append($" [color=#888888]({unionTypes})[/color]");
        }

        // Show duck type indicator if present
        if (_focusedNode?.HasDuckConstraints == true)
        {
            sb.Append($" [color=#FFD700]duck[/color]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds BBCode signature for non-standard nodes (method calls, indexers, etc.)
    /// that don't have a direct declaration.
    /// </summary>
    private string BuildNodeSignatureBBCode(GDTypeFlowNode node)
    {
        var sb = new System.Text.StringBuilder();

        // Show node kind as keyword
        var kindKeyword = node.Kind switch
        {
            GDTypeFlowNodeKind.MethodCall => "call",
            GDTypeFlowNodeKind.IndexerAccess => "index",
            GDTypeFlowNodeKind.PropertyAccess => "prop",
            GDTypeFlowNodeKind.TypeCheck => "is",
            GDTypeFlowNodeKind.NullCheck => "null?",
            GDTypeFlowNodeKind.Comparison => "cmp",
            GDTypeFlowNodeKind.ReturnValue => "return",
            _ => node.Kind.ToString().ToLower()
        };

        sb.Append($"[color=#{ColorToHex(KeywordColor)}]{kindKeyword}[/color] ");

        // Show label (expression)
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{node.Label}[/color]");

        // Show SourceType if available
        if (!string.IsNullOrEmpty(node.SourceType) && node.SourceType != "Variant")
        {
            sb.Append($" [color=#888888]on [/color]");
            sb.Append($"[color=#{ColorToHex(TypeColor)}]{node.SourceType}[/color]");
        }
        else if (!string.IsNullOrEmpty(node.SourceObjectName))
        {
            sb.Append($" [color=#888888]on [/color]");
            sb.Append($"[color=#{ColorToHex(ParamColor)}]{node.SourceObjectName}[/color]");
        }

        // Show result type
        sb.Append($" [color=#{ColorToHex(SymbolColor)}]→[/color] ");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{node.Type ?? "Variant"}[/color]");

        // Show description if available
        if (!string.IsNullOrEmpty(node.Description))
        {
            sb.Append($" [color=#666666]({node.Description})[/color]");
        }

        return sb.ToString();
    }

    private string BuildParameterSignatureBBCode(GDShrapt.Reader.GDParameterDeclaration param, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        var paramName = param.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{paramName}[/color]");

        var paramType = analyzer.GetTypeForNode(param) ?? "Variant";
        sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{paramType}[/color]");

        return sb.ToString();
    }

    private static string ColorToHex(Color color)
    {
        var r = (int)(color.R * 255);
        var g = (int)(color.G * 255);
        var b = (int)(color.B * 255);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    private void UpdateSuggestion(GDTypeFlowNode node)
    {
        if (node == null)
        {
            _suggestionLabel.Visible = false;
            _quickFixButton.Visible = false;
            return;
        }

        // Show suggestion for low confidence or union types
        if (node.Type == "Variant" || node.Confidence < 0.5f)
        {
            _suggestionLabel.Text = "Consider adding explicit type annotation";
            _suggestionLabel.Visible = true;
            _quickFixButton.Visible = true;
        }
        else if (node.IsUnionType)
        {
            _suggestionLabel.Text = $"Union type: Consider explicit type annotation `: {node.UnionTypeInfo?.CommonBaseType ?? node.UnionTypeInfo?.Types.FirstOrDefault() ?? "Type"}`";
            _suggestionLabel.Visible = true;
            _quickFixButton.Visible = true;
        }
        else if (node.HasDuckConstraints)
        {
            var duckInfo = node.DuckTypeInfo;
            var constraintCount = (duckInfo?.RequiredMethods.Count ?? 0) + (duckInfo?.RequiredProperties.Count ?? 0);
            _suggestionLabel.Text = $"Duck type with {constraintCount} constraints - consider explicit type";
            _suggestionLabel.Visible = true;
            _quickFixButton.Visible = true;
        }
        else
        {
            _suggestionLabel.Visible = false;
            _quickFixButton.Visible = false;
        }
    }

    private void ShowNoDataMessage(string message)
    {
        _signatureLabel.Text = $"[color=#999999]{message}[/color]";
        _suggestionLabel.Visible = false;
        _quickFixButton.Visible = false;
        _canvas.DisplayGraph(null);
    }

    #region Event Handlers

    private void OnBlockBodyClicked(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        // Navigate in editor (highlight the token)
        _navigationService?.NavigateToNode(node);
    }

    private void OnBlockLabelClicked(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        // Push current focus to navigation stack
        if (_focusedNode != null && _focusedNode != node)
        {
            _navigationStack.Push(_focusedNode);
            _backButton.Visible = true;
        }

        // Rebuild graph around clicked node
        if (node.SourceScript != null)
        {
            var newRoot = _graphBuilder.BuildGraph(node.Label, node.SourceScript);
            if (newRoot != null)
            {
                _currentSymbolName = node.Label;
                _currentScript = node.SourceScript;
                _titleLabel.Text = node.Label;
                DisplayGraph(newRoot);
            }
        }

        // Navigate in editor
        _navigationService?.NavigateToNode(node);
    }

    private void OnEdgeClicked(GDTypeFlowEdge edge)
    {
        if (edge?.Target == null)
            return;

        // Scroll canvas to show the target node
        _canvas.ScrollToNode(edge.Target);
    }

    private void OnEdgeHoverStart(GDTypeFlowEdge edge, Vector2 position)
    {
        if (edge?.Constraints != null && edge.Constraints.HasRequirements)
        {
            // Convert position to panel coordinates
            var panelPos = _canvas.GlobalPosition + position;
            _constraintTooltip.ShowForEdge(edge, panelPos);
        }
    }

    private void OnEdgeHoverEnd()
    {
        _constraintTooltip.Hide();
    }

    private void OnSignatureMetaClicked(Variant meta)
    {
        var paramName = meta.AsString();
        if (string.IsNullOrEmpty(paramName) || _currentScript == null)
            return;

        // Push current to navigation stack
        if (_focusedNode != null)
        {
            _navigationStack.Push(_focusedNode);
            _backButton.Visible = true;
        }

        // Navigate to the parameter
        ShowForSymbol(paramName, 0, _currentScript);
    }

    private void OnBackPressed()
    {
        if (_navigationStack.Count == 0)
            return;

        var previousNode = _navigationStack.Pop();
        _backButton.Visible = _navigationStack.Count > 0;

        if (previousNode?.SourceScript != null)
        {
            _currentSymbolName = previousNode.Label;
            _currentScript = previousNode.SourceScript;
            _titleLabel.Text = previousNode.Label;

            var rebuiltNode = _graphBuilder.BuildGraph(previousNode.Label, previousNode.SourceScript);
            if (rebuiltNode != null)
            {
                DisplayGraph(rebuiltNode);
            }

            // Navigate in editor
            _navigationService?.NavigateToNode(previousNode);
        }
    }

    private void OnFitPressed()
    {
        _canvas.FitAllNodes();
    }

    private void OnExportPressed()
    {
        if (_focusedNode == null)
            return;

        var exportData = new Godot.Collections.Dictionary
        {
            { "symbol", _currentSymbolName },
            { "type", _focusedNode.Type },
            { "confidence", _focusedNode.Confidence },
            { "isUnionType", _focusedNode.IsUnionType },
            { "hasDuckConstraints", _focusedNode.HasDuckConstraints },
            { "inflows", _focusedNode.Inflows.Count },
            { "outflows", _focusedNode.Outflows.Count },
            { "exportedAt", DateTime.UtcNow.ToString("O") }
        };

        var json = Json.Stringify(exportData, "  ");
        DisplayServer.ClipboardSet(json);

        Logger.Info($"Type flow data exported to clipboard for '{_currentSymbolName}'");
    }

    private void OnQuickFixPressed()
    {
        // TODO: Integrate with refactoring system to add type annotation
        Logger.Info($"Quick fix requested for '{_currentSymbolName}'");
    }

    #endregion
}
