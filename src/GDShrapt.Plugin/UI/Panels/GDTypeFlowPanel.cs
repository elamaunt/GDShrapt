using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using System.Threading.Tasks;
using static Godot.Control;

namespace GDShrapt.Plugin;

/// <summary>
/// Type Flow Explorer panel - visualizes type inference with interactive tabs and graph.
/// Shows how types flow through the code: where they come from (inflows) and where they go (outflows).
/// Features: symbol tabs, adaptive layout, constraints panel, quick fixes.
/// </summary>
internal partial class GDTypeFlowPanel : AcceptDialog
{
    // Services
    private GDScriptProject _project;
    private GDTypeFlowGraphBuilder _graphBuilder;
    private GDTypeFlowNavigationService _navigationService;
    private EditorInterface _editorInterface;
    private IGDTypeFlowHandler _typeFlowHandler;
    private IGDSymbolsHandler _symbolsHandler;

    // Main layout
    private VBoxContainer _mainContainer;
    private GDResponsiveContainer _responsiveContainer;

    // Header with symbol tabs
    private HBoxContainer _headerRow;
    private Button _backButton;
    private GDTypeFlowTabBar _tabBar;
    private Button _pinButton;
    private Label _contextLabel;  // Shows file > method > line context

    // Signature section
    private PanelContainer _signaturePanel;
    private VBoxContainer _signatureContainer;
    private RichTextLabel _signatureLabel;
    private GDConfidenceBadge _confidenceBadge;
    private Label _confidenceDescLabel;

    // Content area - switches based on layout mode
    private Control _contentArea;
    private GDTypeBreakdownPanel _breakdownPanel;

    // Constraints panel
    private GDConstraintsPanel _constraintsPanel;

    // Action bar
    private HBoxContainer _actionBar;
    private Label _suggestionLabel;
    private Button _quickFixButton;
    private Button _addGuardButton;
    private Button _exportButton;

    // State
    private GDScriptFile _currentScript;
    private string _currentSymbolName;
    private GDTypeFlowNode _rootNode;
    private GDTypeFlowNode _focusedNode;
    private Stack<GDTypeFlowNode> _navigationStack = new();
    private bool _uiCreated;
    private bool _followMode = true;
    private bool _isPinned = false;
    private GDResponsiveContainer.LayoutMode _currentLayoutMode = GDResponsiveContainer.LayoutMode.Normal;

    // Colors
    private static readonly Color KeywordColor = new(0.8f, 0.55f, 0.75f);   // Pink
    private static readonly Color TypeColor = new(0.4f, 0.76f, 0.65f);       // Cyan
    private static readonly Color ParamColor = new(0.85f, 0.85f, 0.75f);     // Light cream
    private static readonly Color SymbolColor = new(0.6f, 0.6f, 0.6f);       // Gray
    private static readonly Color DerivableColor = new(1.0f, 0.85f, 0.3f);   // Yellow for derivable types

    /// <summary>
    /// Event fired when user clicks on a file location to navigate.
    /// </summary>
    public event Action<string, int> NavigateToRequested;

    /// <summary>
    /// Event fired when user requests to add a type annotation.
    /// Parameters: script file, symbol name, suggested type, location.
    /// </summary>
    public event Action<GDScriptFile, GDTypeAnnotationPlan> AddTypeAnnotationRequested;

    /// <summary>
    /// Event fired when user requests to add a type guard.
    /// Parameters: script file, symbol name, union types.
    /// </summary>
    public event Action<GDScriptFile, string, IReadOnlyList<string>> AddTypeGuardRequested;

    /// <summary>
    /// Event fired when user requests to generate an interface from duck type constraints.
    /// Parameters: script file, duck type info.
    /// </summary>
    public event Action<GDScriptFile, GDDuckType> GenerateInterfaceRequested;

    public GDTypeFlowPanel()
    {
        Title = "Type Flow";
        Size = new Vector2I(650, 400);
        MinSize = new Vector2I(400, 350);  // Minimum size for the dialog (increased from 300 to fit content)
        Exclusive = false;
        Transient = true;
        Unresizable = false;
        OkButtonText = "Закрыть";
    }

    public override void _Ready()
    {
        EnsureUICreated();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event is InputEventKey key && key.Pressed)
        {
            HandleKeyInput(key);
        }
    }

    /// <summary>
    /// Initializes the panel with required dependencies.
    /// </summary>
    public void Initialize(GDScriptProject project, EditorInterface editorInterface, IGDTypeFlowHandler typeFlowHandler, IGDSymbolsHandler symbolsHandler)
    {
        _project = project;
        _editorInterface = editorInterface;
        _typeFlowHandler = typeFlowHandler;
        _symbolsHandler = symbolsHandler;
        _graphBuilder = new GDTypeFlowGraphBuilder(project, typeFlowHandler, symbolsHandler);
        _navigationService = new GDTypeFlowNavigationService(editorInterface, project);
    }

    /// <summary>
    /// Sets the project (for backward compatibility).
    /// </summary>
    public void SetProject(GDScriptProject project, IGDTypeFlowHandler typeFlowHandler, IGDSymbolsHandler symbolsHandler)
    {
        _project = project;
        _typeFlowHandler = typeFlowHandler;
        _symbolsHandler = symbolsHandler;
        _graphBuilder = new GDTypeFlowGraphBuilder(project, typeFlowHandler, symbolsHandler);
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

        // Add symbol tab if not in follow mode
        if (!_followMode || !_tabBar.IsCursorTabActive)
        {
            _tabBar.AddSymbolTab(_rootNode);
        }

        // Display the graph with root as focus
        DisplayNode(_rootNode);

        // Bring panel to front
        BringToFront();
    }

    /// <summary>
    /// Updates the panel for the current cursor position (Follow Mode).
    /// </summary>
    public void UpdateForCursorPosition(string symbolName, int line, GDScriptFile scriptFile)
    {
        if (!_followMode || !Visible || _isPinned)
            return;

        if (!_tabBar.IsCursorTabActive)
            return;

        // Don't update if same symbol
        if (_currentSymbolName == symbolName && _currentScript == scriptFile)
            return;

        _currentSymbolName = symbolName;
        _currentScript = scriptFile;

        if (_project == null || scriptFile == null)
            return;

        // Build the graph
        _rootNode = _graphBuilder.BuildGraph(symbolName, scriptFile);
        if (_rootNode == null)
            return;

        // Display without adding tab
        DisplayNode(_rootNode);
    }

    /// <summary>
    /// Brings the panel to the front of the window stack.
    /// </summary>
    public void BringToFront()
    {
        if (!Visible)
        {
            PopupCentered();
        }
        else
        {
            // Ensure the window is on top when already visible
            MoveToForeground();
        }
        GrabFocus();
    }

    /// <summary>
    /// Refreshes the current symbol by rebuilding the graph.
    /// Call this after the file has been modified (e.g., type annotation added).
    /// </summary>
    public void RefreshCurrentSymbol()
    {
        if (string.IsNullOrEmpty(_currentSymbolName) || _currentScript == null || _project == null)
            return;

        // Re-analyze the script to pick up changes
        _currentScript.Analyze();

        // Rebuild the graph
        _rootNode = _graphBuilder.BuildGraph(_currentSymbolName, _currentScript);
        if (_rootNode == null)
        {
            ShowNoDataMessage($"Symbol '{_currentSymbolName}' not found after refresh");
            return;
        }

        // Display the updated node
        DisplayNode(_rootNode);
    }

    #region UI Creation

    private void EnsureUICreated()
    {
        if (_uiCreated)
            return;

        _uiCreated = true;
        CreateUI();
    }

    private void CreateUI()
    {
        // Responsive container wraps everything
        // For AcceptDialog, we need to use FULL_RECT anchors to fill the client area
        _responsiveContainer = new GDResponsiveContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _responsiveContainer.LayoutModeChanged += OnLayoutModeChanged;
        AddChild(_responsiveContainer);

        // Set anchors to fill the entire available area (must be done after AddChild)
        _responsiveContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _responsiveContainer.OffsetLeft = 8;
        _responsiveContainer.OffsetTop = 8;
        _responsiveContainer.OffsetRight = -8;
        _responsiveContainer.OffsetBottom = -40;  // Leave space for the OK button at the bottom

        // Main vertical container - MUST be ExpandFill to fill the dialog
        _mainContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _mainContainer.AddThemeConstantOverride("separation", 6);
        _responsiveContainer.AddChild(_mainContainer);

        CreateHeader();
        CreateSignatureSection();
        CreateContentArea();
        CreateConstraintsPanel();
        CreateActionBar();
    }

    private void CreateHeader()
    {
        _headerRow = new HBoxContainer();
        _headerRow.AddThemeConstantOverride("separation", 4);

        // Back button
        _backButton = new Button
        {
            Text = "←",
            Visible = false,
            TooltipText = "Go back (Alt+Left)"
        };
        _backButton.AddThemeFontSizeOverride("font_size", 14);
        _backButton.Pressed += OnBackPressed;
        _headerRow.AddChild(_backButton);

        // Tab bar
        _tabBar = new GDTypeFlowTabBar
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _tabBar.SymbolTabActivated += OnSymbolTabActivated;
        _tabBar.SymbolTabClosed += OnSymbolTabClosed;
        _tabBar.CursorTabActivated += OnCursorTabActivated;
        _headerRow.AddChild(_tabBar);

        // Pin button
        _pinButton = new Button
        {
            Text = "📌",
            ToggleMode = true,
            TooltipText = "Pin panel (don't follow cursor)"
        };
        _pinButton.Toggled += OnPinToggled;
        _headerRow.AddChild(_pinButton);

        _mainContainer.AddChild(_headerRow);

        // Context label - shows file > method > line
        _contextLabel = new Label
        {
            Text = "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _contextLabel.AddThemeFontSizeOverride("font_size", 10);
        _contextLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        _mainContainer.AddChild(_contextLabel);

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

        _signatureContainer = new VBoxContainer();
        _signatureContainer.AddThemeConstantOverride("separation", 4);

        // Signature label
        _signatureLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SelectionEnabled = false,
            CustomMinimumSize = new Vector2(0, 24)
        };
        _signatureLabel.MetaClicked += OnSignatureMetaClicked;
        _signatureContainer.AddChild(_signatureLabel);

        // Confidence row
        var confidenceRow = new HBoxContainer();
        confidenceRow.AddThemeConstantOverride("separation", 8);

        _confidenceBadge = new GDConfidenceBadge();
        _confidenceBadge.SetConfidence(1.0f);
        confidenceRow.AddChild(_confidenceBadge);

        _confidenceDescLabel = new Label
        {
            Text = "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _confidenceDescLabel.AddThemeFontSizeOverride("font_size", 11);
        _confidenceDescLabel.AddThemeColorOverride("font_color", SymbolColor);
        confidenceRow.AddChild(_confidenceDescLabel);

        _signatureContainer.AddChild(confidenceRow);
        _signaturePanel.AddChild(_signatureContainer);
        _mainContainer.AddChild(_signaturePanel);
    }

    private void CreateContentArea()
    {
        // Content area that adapts to layout mode - MUST be ExpandFill (the only expanding child in _mainContainer)
        _contentArea = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 120)  // Minimum height to prevent content overflow
        };

        // Breakdown panel (tabs: Overview, Inflows, Outflows) - ExpandFill to take available space
        _breakdownPanel = new GDTypeBreakdownPanel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _breakdownPanel.NodeNavigationRequested += OnNodeNavigationRequested;
        _breakdownPanel.NodeOpenInTabRequested += OnNodeOpenInTabRequested;

        // Default: show breakdown panel
        _contentArea.AddChild(_breakdownPanel);

        _mainContainer.AddChild(_contentArea);
    }

    private void CreateConstraintsPanel()
    {
        _constraintsPanel = new GDConstraintsPanel();
        _constraintsPanel.Visible = false;
        _constraintsPanel.AddInterfaceRequested += OnAddInterfaceRequested;
        _constraintsPanel.NarrowTypeRequested += OnNarrowTypeRequested;
        _constraintsPanel.AddTypeGuardRequested += OnAddTypeGuardRequestedInternal;
        _mainContainer.AddChild(_constraintsPanel);
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
            Visible = false,
            TooltipText = "Add explicit type annotation to this symbol"
        };
        _quickFixButton.Pressed += OnQuickFixPressed;
        _actionBar.AddChild(_quickFixButton);

        _addGuardButton = new Button
        {
            Text = "Add type guard",
            Visible = false,
            TooltipText = "Add a type check to narrow the type"
        };
        _addGuardButton.Pressed += OnAddTypeGuardPressed;
        _actionBar.AddChild(_addGuardButton);

        _exportButton = new Button
        {
            Text = "Export",
            TooltipText = "Export type flow data to clipboard"
        };
        _exportButton.Pressed += OnExportPressed;
        _actionBar.AddChild(_exportButton);

        _mainContainer.AddChild(_actionBar);
    }

    #endregion

    #region Display Logic

    private void DisplayNode(GDTypeFlowNode node)
    {
        _focusedNode = node;

        if (node == null)
        {
            ShowNoDataMessage("No symbol selected");
            return;
        }

        // Debug output for semantic core data
        DebugLogNodeData(node);

        // Update context label (file > method > line)
        UpdateContextLabel(node);

        // Update signature
        UpdateSignature(node);

        // Update confidence
        UpdateConfidence(node);

        // Update breakdown panel
        _breakdownPanel.SetNode(node, node.SourceScript?.SemanticModel);

        // Update constraints panel
        _constraintsPanel.SetConstraints(node);

        // Update action bar
        UpdateActionBar(node);
    }

    /// <summary>
    /// Updates the context label with file > line > kind information.
    /// Helps developers understand where the symbol is located.
    /// </summary>
    private void UpdateContextLabel(GDTypeFlowNode node)
    {
        var parts = new List<string>();

        // File name
        if (node.SourceScript != null && !string.IsNullOrEmpty(node.SourceScript.FullPath))
        {
            parts.Add(System.IO.Path.GetFileName(node.SourceScript.FullPath));
        }

        // Line number
        if (node.Location?.IsValid == true)
        {
            parts.Add($"line {node.Location.StartLine + 1}");
        }

        // Node kind for context
        if (node.Kind != GDTypeFlowNodeKind.Unknown)
        {
            parts.Add(GetKindDescription(node.Kind));
        }

        _contextLabel.Text = parts.Count > 0 ? string.Join(" > ", parts) : "";
        _contextLabel.Visible = parts.Count > 0;
        // Remove from layout completely when hidden to avoid reserving space
        _contextLabel.CustomMinimumSize = parts.Count > 0 ? new Vector2(0, 16) : Vector2.Zero;
    }

    /// <summary>
    /// Gets a human-readable description for a node kind.
    /// </summary>
    private string GetKindDescription(GDTypeFlowNodeKind kind)
    {
        return kind switch
        {
            GDTypeFlowNodeKind.Parameter => "parameter",
            GDTypeFlowNodeKind.LocalVariable => "local variable",
            GDTypeFlowNodeKind.MemberVariable => "member variable",
            GDTypeFlowNodeKind.MethodCall => "method call",
            GDTypeFlowNodeKind.PropertyAccess => "property",
            GDTypeFlowNodeKind.ReturnValue => "return value",
            GDTypeFlowNodeKind.Literal => "literal",
            GDTypeFlowNodeKind.TypeAnnotation => "type annotation",
            _ => kind.ToString().ToLower()
        };
    }

    /// <summary>
    /// Debug output of all semantic data for a node before display.
    /// </summary>
    private void DebugLogNodeData(GDTypeFlowNode node)
    {
        Logger.Info("=== TypeFlow Debug: Semantic Core Data ===");
        Logger.Info($"Symbol: {node.Label}");
        Logger.Info($"Type: {node.Type ?? "null"}");
        Logger.Info($"Kind: {node.Kind}");
        Logger.Info($"Confidence: {node.Confidence:P0}");
        Logger.Info($"Description: {node.Description ?? "null"}");
        Logger.Info($"Location: {node.Location}");
        Logger.Info($"IsUnionType: {node.IsUnionType}");
        Logger.Info($"HasDuckConstraints: {node.HasDuckConstraints}");
        Logger.Info($"AreInflowsLoaded: {node.AreInflowsLoaded}");
        Logger.Info($"AreOutflowsLoaded: {node.AreOutflowsLoaded}");

        // Inflows
        Logger.Info($"--- Inflows ({node.Inflows.Count}) ---");
        foreach (var inflow in node.Inflows)
        {
            Logger.Info($"  [{inflow.Kind}] {inflow.Label}: {inflow.Type ?? "Variant"} (conf: {inflow.Confidence:P0})");
            if (!string.IsNullOrEmpty(inflow.Description))
                Logger.Info($"    Description: {inflow.Description}");
            if (inflow.Location != null)
                Logger.Info($"    Location: {inflow.Location}");
        }

        // Outflows
        Logger.Info($"--- Outflows ({node.Outflows.Count}) ---");
        foreach (var outflow in node.Outflows)
        {
            Logger.Info($"  [{outflow.Kind}] {outflow.Label}: {outflow.Type ?? "Variant"} (conf: {outflow.Confidence:P0})");
            if (!string.IsNullOrEmpty(outflow.Description))
                Logger.Info($"    Description: {outflow.Description}");
            if (outflow.Location != null)
                Logger.Info($"    Location: {outflow.Location}");
        }

        // Union type info
        if (node.IsUnionType && node.UnionTypeInfo != null)
        {
            Logger.Info($"--- Union Type Info ---");
            Logger.Info($"  Types: {string.Join(", ", node.UnionTypeInfo.Types.Select(t => t.DisplayName))}");
            Logger.Info($"  Union Sources: {node.UnionSources.Count}");
            foreach (var src in node.UnionSources)
            {
                Logger.Info($"    [{src.Kind}] {src.Label}: {src.Type}");
            }
        }

        // Duck type info
        if (node.HasDuckConstraints && node.DuckTypeInfo != null)
        {
            Logger.Info($"--- Duck Type Constraints ---");
            if (node.DuckTypeInfo.RequiredMethods.Count > 0)
                Logger.Info($"  Required methods: {string.Join(", ", node.DuckTypeInfo.RequiredMethods)}");
            if (node.DuckTypeInfo.RequiredProperties.Count > 0)
                Logger.Info($"  Required properties: {string.Join(", ", node.DuckTypeInfo.RequiredProperties)}");
        }

        // Semantic model data (if available)
        var semanticModel = node.SourceScript?.SemanticModel;
        if (semanticModel != null && !string.IsNullOrEmpty(node.Label))
        {
            Logger.Info($"--- Semantic Model Data ---");

            var symbol = semanticModel.FindSymbol(node.Label);
            if (symbol != null)
            {
                Logger.Info($"  Symbol.Name: {symbol.Name}");
                Logger.Info($"  Symbol.Kind: {symbol.Kind}");
                Logger.Info($"  Symbol.TypeName: {symbol.TypeName ?? "null"}");
            }
            else
            {
                Logger.Info($"  Symbol not found in semantic model");
            }

            var unionType = semanticModel.TypeSystem.GetUnionType(node.Label);
            if (unionType != null && unionType.IsUnion)
            {
                Logger.Info($"  TypeSystem.GetUnionType: {string.Join("|", unionType.Types.Select(t => t.DisplayName))}");
            }

            var duckType = semanticModel.TypeSystem.GetDuckType(node.Label);
            if (duckType != null && duckType.HasRequirements)
            {
                Logger.Info($"  TypeSystem.GetDuckType: methods={string.Join(",", duckType.RequiredMethods)} props={string.Join(",", duckType.RequiredProperties)}");
            }

            // Check narrowing at node's location
            if (node.AstNode != null)
            {
                var narrowedType = semanticModel.TypeSystem.GetNarrowedType(node.Label, node.AstNode);
                if (!string.IsNullOrEmpty(narrowedType))
                {
                    Logger.Info($"  SemanticModel.GetNarrowedType: {narrowedType}");
                }
            }
        }

        Logger.Info("=== End TypeFlow Debug ===");
    }

    private void UpdateSignature(GDTypeFlowNode node)
    {
        if (node?.SourceScript == null)
        {
            _signatureLabel.Text = $"[color=#cccccc]{node?.Label ?? "Unknown"}[/color]";
            return;
        }

        var semanticModel = node.SourceScript.SemanticModel;
        if (semanticModel == null)
        {
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

        var symbol = semanticModel.FindSymbol(node.Label);
        if (symbol?.DeclarationNode == null)
        {
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

        _signatureLabel.Text = BuildSignatureBBCode(symbol, semanticModel);
    }

    private void UpdateConfidence(GDTypeFlowNode node)
    {
        _confidenceBadge.SetConfidence(node.Confidence);

        var desc = node.Confidence switch
        {
            >= 0.9f => "High confidence (explicit type)",
            >= 0.7f => "Good confidence (inferred from context)",
            >= 0.5f => "Medium confidence (partial inference)",
            _ => "Low confidence (Variant or unknown)"
        };

        // Add source info
        if (node.Kind == GDTypeFlowNodeKind.TypeAnnotation)
            desc = "Explicit type annotation";
        else if (node.Kind == GDTypeFlowNodeKind.Literal)
            desc = "Inferred from literal value";
        else if (node.Kind == GDTypeFlowNodeKind.MethodCall)
            desc = "Inferred from method return type";

        _confidenceDescLabel.Text = desc;
    }

    private void UpdateActionBar(GDTypeFlowNode node)
    {
        // Show quick fix for low confidence
        var showQuickFix = node.Type == "Variant" || node.Confidence < 0.5f || node.IsUnionType;
        _quickFixButton.Visible = showQuickFix;

        // Type guard button is shown for union types - clicking shows preview
        // Single-file operations with preview are allowed in Base
        _addGuardButton.Visible = node.IsUnionType;

        // Update suggestion
        if (node.Type == "Variant" || node.Confidence < 0.5f)
        {
            _suggestionLabel.Text = "💡 Consider adding explicit type annotation";
            _suggestionLabel.Visible = true;
        }
        else if (node.IsUnionType)
        {
            var types = node.UnionTypeInfo?.Types.Take(3).Select(t => t.DisplayName).ToList() ?? new List<string>();
            _suggestionLabel.Text = $"💡 Union type: {string.Join(" | ", types)}";
            _suggestionLabel.Visible = true;
        }
        else if (node.HasDuckConstraints)
        {
            var count = (node.DuckTypeInfo?.RequiredMethods.Count ?? 0) + (node.DuckTypeInfo?.RequiredProperties.Count ?? 0);
            _suggestionLabel.Text = $"💡 Duck type with {count} constraints";
            _suggestionLabel.Visible = true;
        }
        else
        {
            _suggestionLabel.Visible = false;
        }
    }

    private void ShowNoDataMessage(string message)
    {
        _signatureLabel.Text = $"[color=#999999]{message}[/color]";
        _confidenceBadge.SetConfidence(0);
        _confidenceDescLabel.Text = "";
        _suggestionLabel.Visible = false;
        _quickFixButton.Visible = false;
        _addGuardButton.Visible = false;
        _breakdownPanel.ClearAll();
        _constraintsPanel.Clear();
    }

    #endregion

    #region Layout Mode

    private void OnLayoutModeChanged(GDResponsiveContainer.LayoutMode mode)
    {
        _currentLayoutMode = mode;
        ApplyLayoutMode(mode);
    }

    private void ApplyLayoutMode(GDResponsiveContainer.LayoutMode mode)
    {
        switch (mode)
        {
            case GDResponsiveContainer.LayoutMode.Minimal:
                // Hide tabs, show only signature
                _breakdownPanel.Visible = false;
                _constraintsPanel.SetCompactMode(true);
                _confidenceBadge.SetCompactMode(true);
                break;

            case GDResponsiveContainer.LayoutMode.Compact:
                // Show breakdown only
                _breakdownPanel.Visible = true;
                _breakdownPanel.SetCompactMode(true);
                _constraintsPanel.SetCompactMode(true);
                _confidenceBadge.SetCompactMode(true);
                break;

            case GDResponsiveContainer.LayoutMode.Normal:
            case GDResponsiveContainer.LayoutMode.Wide:
                // Show breakdown panel (Wide treated as Normal since canvas was removed)
                _breakdownPanel.Visible = true;
                _breakdownPanel.SetCompactMode(false);
                _constraintsPanel.SetCompactMode(false);
                _confidenceBadge.SetCompactMode(false);
                break;
        }
    }

    #endregion

    #region Signature Building

    private string BuildSignatureBBCode(Semantics.GDSymbolInfo symbol, GDSemanticModel analyzer)
    {
        var decl = symbol.DeclarationNode;

        if (decl is GDShrapt.Reader.GDMethodDeclaration method)
            return BuildMethodSignatureBBCode(method, analyzer);

        if (decl is GDShrapt.Reader.GDVariableDeclaration variable)
            return BuildVariableSignatureBBCode(variable, analyzer);

        if (decl is GDShrapt.Reader.GDParameterDeclaration param)
            return BuildParameterSignatureBBCode(param, analyzer);

        if (_focusedNode != null && IsExpressionNode(_focusedNode.Kind))
            return BuildNodeSignatureBBCode(_focusedNode);

        var typeInfo = analyzer.TypeSystem.GetType(decl);
        var typeStr = typeInfo.IsVariant ? "Variant" : typeInfo.DisplayName;
        return $"[color=#{ColorToHex(ParamColor)}]{symbol.Name}[/color]: [color=#{ColorToHex(TypeColor)}]{typeStr}[/color]";
    }

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

    private string BuildMethodSignatureBBCode(GDShrapt.Reader.GDMethodDeclaration method, GDSemanticModel analyzer)
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

            // Get parameter type: explicit annotation, or inferred union type from type guards/null checks
            string paramType;
            if (param.Type != null)
            {
                // Explicit type annotation
                paramType = param.Type.BuildName();
            }
            else
            {
                // No explicit type - try to get union type from type guards and null checks
                var unionType = analyzer.TypeSystem.GetUnionType(paramName);
                if (unionType != null && !unionType.IsEmpty)
                {
                    paramType = unionType.ToString();
                }
                else
                {
                    var paramTypeInfo = analyzer.TypeSystem.GetType(param);
                    paramType = paramTypeInfo.IsVariant ? "Variant" : paramTypeInfo.DisplayName;
                }
            }

            sb.Append($"[url={paramName}][color=#{ColorToHex(ParamColor)}]{paramName}[/color][/url]");
            sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
            sb.Append(FormatTypeBBCode(paramType, paramName));

            if (i < parameters.Count - 1)
                sb.Append($"[color=#{ColorToHex(SymbolColor)}], [/color]");
        }

        sb.Append($"[color=#{ColorToHex(SymbolColor)}])[/color]");

        // Get return type: try union type first for methods with multiple return types
        var returnUnion = analyzer.TypeSystem.GetUnionType(methodName);
        string returnType;
        if (returnUnion != null && !returnUnion.IsEmpty)
        {
            returnType = returnUnion.ToString();
        }
        else
        {
            var returnTypeInfo = analyzer.TypeSystem.GetType(method);
            returnType = returnTypeInfo.IsVariant ? "void" : returnTypeInfo.DisplayName;
        }
        sb.Append($" [color=#{ColorToHex(SymbolColor)}]→[/color] ");
        sb.Append(FormatTypeBBCode(returnType, "return"));

        return sb.ToString();
    }

    private string BuildVariableSignatureBBCode(GDShrapt.Reader.GDVariableDeclaration variable, GDSemanticModel analyzer)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[color=#{ColorToHex(KeywordColor)}]var[/color] ");

        var varName = variable.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{varName}[/color]");

        var varTypeInfo = analyzer.TypeSystem.GetType(variable);
        var varType = varTypeInfo.IsVariant ? "Variant" : varTypeInfo.DisplayName;
        sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
        sb.Append(FormatTypeBBCode(varType, varName));

        if (_focusedNode?.IsUnionType == true && _focusedNode.UnionTypeInfo != null)
        {
            var unionTypes = string.Join("|", _focusedNode.UnionTypeInfo.Types.Take(3).Select(t => t.DisplayName));
            if (_focusedNode.UnionTypeInfo.Types.Count > 3)
                unionTypes += "...";
            sb.Append($" [color=#888888]({unionTypes})[/color]");
        }

        if (_focusedNode?.HasDuckConstraints == true)
            sb.Append($" [color=#FFD700]duck[/color]");

        return sb.ToString();
    }

    private string BuildParameterSignatureBBCode(GDShrapt.Reader.GDParameterDeclaration param, GDSemanticModel analyzer)
    {
        var sb = new System.Text.StringBuilder();
        var paramName = param.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{paramName}[/color]");

        var paramTypeInfo = analyzer.TypeSystem.GetType(param);
        var paramType = paramTypeInfo.IsVariant ? "Variant" : paramTypeInfo.DisplayName;
        sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
        sb.Append(FormatTypeBBCode(paramType, paramName));

        return sb.ToString();
    }

    private string BuildNodeSignatureBBCode(GDTypeFlowNode node)
    {
        var sb = new System.Text.StringBuilder();

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
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{node.Label}[/color]");

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

        sb.Append($" [color=#{ColorToHex(SymbolColor)}]→[/color] ");
        sb.Append(FormatTypeBBCode(node.Type ?? "Variant", node.Label));

        if (!string.IsNullOrEmpty(node.Description))
            sb.Append($" [color=#666666]({node.Description})[/color]");

        return sb.ToString();
    }

    private static string ColorToHex(Color color)
    {
        var r = (int)(color.R * 255);
        var g = (int)(color.G * 255);
        var b = (int)(color.B * 255);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Formats a type for display, showing "Variant" as clickable "<Derivable>" in yellow with underline.
    /// </summary>
    private string FormatTypeBBCode(string typeName, string context = null)
    {
        if (typeName == "Variant" || string.IsNullOrEmpty(typeName))
        {
            // Show as clickable <Derivable> in yellow with underline
            var urlTarget = context ?? "derivable";
            return $"[url={urlTarget}][color=#{ColorToHex(DerivableColor)}][u]<Derivable>[/u][/color][/url]";
        }
        return $"[color=#{ColorToHex(TypeColor)}]{typeName}[/color]";
    }

    #endregion

    #region Event Handlers

    private void HandleKeyInput(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.Escape:
                Hide();
                break;

            case Key.Backspace:
            case Key.Left when key.AltPressed:
                OnBackPressed();
                break;

            case Key.Tab:
                _breakdownPanel.CycleTab();
                break;

            case Key.Enter:
                if (_focusedNode != null)
                    _navigationService?.NavigateToNode(_focusedNode);
                break;

            case Key.Key1 when key.AltPressed:
                _tabBar.ActivateCursorTab();
                break;
        }
    }

    private void OnSymbolTabActivated(GDTypeFlowNode node)
    {
        if (node != null)
        {
            _followMode = false;
            DisplayNode(node);
        }
    }

    private void OnSymbolTabClosed(GDTypeFlowNode node)
    {
        // If the closed tab was the focused node, clear or switch to cursor
        if (node != null && node == _focusedNode)
        {
            if (_tabBar.IsCursorTabActive)
            {
                _followMode = true;
            }
        }
    }

    private void OnCursorTabActivated()
    {
        _followMode = true;
        // Will update on next cursor change
    }

    private void OnPinToggled(bool pressed)
    {
        _isPinned = pressed;
        _pinButton.Text = pressed ? "📍" : "📌";
    }

    private void OnNodeNavigationRequested(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        _navigationService?.NavigateToNode(node);
    }

    private void OnNodeOpenInTabRequested(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        NavigateToNode(node);
    }

    private void NavigateToNode(GDTypeFlowNode node)
    {
        // Push current focus to navigation stack
        if (_focusedNode != null && _focusedNode != node)
        {
            _navigationStack.Push(_focusedNode);
            _backButton.Visible = true;
        }

        // Add tab and show
        _tabBar.AddSymbolTab(node);
        _followMode = false;

        // Rebuild graph around clicked node
        if (node.SourceScript != null)
        {
            var newRoot = _graphBuilder.BuildGraph(node.Label, node.SourceScript);
            if (newRoot != null)
            {
                _currentSymbolName = node.Label;
                _currentScript = node.SourceScript;
                DisplayNode(newRoot);
            }
        }

        _navigationService?.NavigateToNode(node);
    }

    private void OnSignatureMetaClicked(Variant meta)
    {
        var targetName = meta.AsString();
        if (string.IsNullOrEmpty(targetName) || _currentScript == null)
            return;

        // Handle click on <Derivable> type - trigger quick fix
        if (targetName == "derivable" || targetName == "return")
        {
            OnQuickFixPressed();
            return;
        }

        // For parameter names, try to navigate to parameter's type flow
        if (_focusedNode != null)
        {
            _navigationStack.Push(_focusedNode);
            _backButton.Visible = true;
        }

        ShowForSymbol(targetName, 0, _currentScript);
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

            // Activate the corresponding tab
            _tabBar.ActivateTabForNode(previousNode);

            var rebuiltNode = _graphBuilder.BuildGraph(previousNode.Label, previousNode.SourceScript);
            if (rebuiltNode != null)
            {
                DisplayNode(rebuiltNode);
            }

            _navigationService?.NavigateToNode(previousNode);
        }
    }

    private async void OnQuickFixPressed()
    {
        if (_focusedNode == null || _currentScript == null)
        {
            Logger.Info("Quick fix: No focused node or script");
            return;
        }

        var location = _focusedNode.Location;
        if (location == null || !location.IsValid)
        {
            Logger.Info($"Quick fix: Invalid location for '{_focusedNode.Label}'");
            return;
        }

        // Use refactoring service to plan the annotation
        var service = new GDAddTypeAnnotationsService();
        var options = new GDTypeAnnotationOptions
        {
            IncludeParameters = _focusedNode.Kind == GDTypeFlowNodeKind.Parameter,
            IncludeLocals = _focusedNode.Kind == GDTypeFlowNodeKind.LocalVariable,
            IncludeClassVariables = _focusedNode.Kind == GDTypeFlowNodeKind.MemberVariable,
            MinimumConfidence = GDTypeConfidence.Low // Accept all confidence levels
        };

        var result = service.PlanFile(_currentScript, options);

        if (result.Success && result.HasAnnotations)
        {
            var annotation = result.Annotations.FirstOrDefault(a => a.IdentifierName == _focusedNode.Label);
            if (annotation != null)
            {
                // Show preview dialog before applying
                await ShowQuickFixPreview(annotation);
            }
            else
            {
                Logger.Info($"Quick fix: No matching annotation found for '{_focusedNode.Label}'");
            }
        }
        else
        {
            Logger.Info($"Quick fix: Service returned no annotations. Error: {result.ErrorMessage}");
        }
    }

    private async Task ShowQuickFixPreview(GDTypeAnnotationPlan annotation)
    {
        var previewDialog = new RefactoringPreviewDialog();
        AddChild(previewDialog);

        try
        {
            var title = $"Add Type Annotation ({annotation.InferredType.Confidence})";
            var targetDescription = annotation.Target switch
            {
                TypeAnnotationTarget.ClassVariable => "class variable",
                TypeAnnotationTarget.LocalVariable => "local variable",
                TypeAnnotationTarget.Parameter => "parameter",
                _ => "declaration"
            };

            var originalCode = $"{annotation.IdentifierName}";
            var resultCode = $"{annotation.IdentifierName}: {annotation.InferredType.TypeName.DisplayName}";

            // Single-file apply with preview is allowed in Base
            var canApply = true;

            var dialogResult = await previewDialog.ShowForResult(
                title,
                $"// Add type annotation to {targetDescription}\n" +
                $"// Inferred type: {annotation.InferredType.TypeName.DisplayName}\n" +
                $"// Confidence: {annotation.InferredType.Confidence}\n" +
                $"// Reason: {annotation.InferredType.Reason ?? "Type inference"}\n\n" +
                originalCode,
                resultCode,
                canApply,
                "Apply");

            if (dialogResult.ShouldApply)
            {
                Logger.Info($"Quick fix: Applying ': {annotation.InferredType.TypeName.DisplayName}' to '{_focusedNode.Label}'");

                // Fire event for plugin to handle the actual application
                AddTypeAnnotationRequested?.Invoke(_currentScript, annotation);

                // Refresh the graph to reflect the change
                await Task.Delay(100); // Small delay to let file be saved
                RefreshCurrentSymbol();
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }

    private async void OnAddTypeGuardPressed()
    {
        await ShowTypeGuardPreview(_focusedNode);
    }

    private async void OnAddInterfaceRequested(GDTypeFlowNode node)
    {
        if (node?.DuckTypeInfo == null || _currentScript == null)
            return;

        await ShowInterfacePreview(node);
    }

    private async void OnNarrowTypeRequested(GDTypeFlowNode node)
    {
        if (node?.UnionTypeInfo == null || _currentScript == null)
            return;

        await ShowTypeGuardPreview(node);
    }

    private async void OnAddTypeGuardRequestedInternal(GDTypeFlowNode node)
    {
        if (node == null || _currentScript == null)
            return;

        await ShowTypeGuardPreview(node);
    }

    private async Task ShowTypeGuardPreview(GDTypeFlowNode node)
    {
        if (node == null || _currentScript == null)
            return;

        var types = node.UnionTypeInfo?.Types.Select(t => t.DisplayName).ToList() ?? new List<string>();
        if (types.Count == 0 && !string.IsNullOrEmpty(node.Type))
        {
            types.Add(node.Type);
        }

        var previewDialog = new RefactoringPreviewDialog();
        AddChild(previewDialog);

        try
        {
            var title = "Add Type Guard";
            var typesStr = string.Join(" | ", types.Take(5));
            if (types.Count > 5) typesStr += "...";

            // Generate example code for preview
            var originalCode = $"# Before type guard\n{node.Label}  # type: {typesStr}";
            var resultCode = $"# After type guard\nif {node.Label} is {types.FirstOrDefault() ?? "Type"}:\n    # {node.Label} is narrowed to {types.FirstOrDefault() ?? "Type"}\n    pass";

            // Type guard generation is not yet implemented - show preview only
            var canApply = false;

            var dialogResult = await previewDialog.ShowForResult(
                title,
                $"// Add type guard for union type\n" +
                $"// Symbol: {node.Label}\n" +
                $"// Types: {typesStr}\n\n" +
                originalCode,
                resultCode,
                canApply,
                "Apply");

            if (dialogResult.ShouldApply && canApply)
            {
                Logger.Info($"Type guard: Applying to '{node.Label}'");
                AddTypeGuardRequested?.Invoke(_currentScript, node.Label, types);
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }

    private async Task ShowInterfacePreview(GDTypeFlowNode node)
    {
        if (node?.DuckTypeInfo == null || _currentScript == null)
            return;

        var duckType = node.DuckTypeInfo;
        var previewDialog = new RefactoringPreviewDialog();
        AddChild(previewDialog);

        try
        {
            var title = "Generate Interface from Duck Type";
            var methodsList = string.Join(", ", duckType.RequiredMethods.Keys.Take(5));
            var propsList = string.Join(", ", duckType.RequiredProperties.Keys.Take(5));

            // Generate interface preview
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"class_name I{node.Label}Interface");
            sb.AppendLine();

            foreach (var prop in duckType.RequiredProperties.Take(10))
            {
                var propType = prop.Value != null ? prop.Value.DisplayName : "Variant";
                sb.AppendLine($"var {prop.Key}: {propType}");
            }

            if (duckType.RequiredProperties.Count > 0 && duckType.RequiredMethods.Count > 0)
                sb.AppendLine();

            foreach (var method in duckType.RequiredMethods.Take(10))
            {
                var paramCount = method.Value >= 0 ? method.Value : 0;
                var paramsStr = paramCount > 0
                    ? string.Join(", ", Enumerable.Range(0, paramCount).Select(i => $"arg{i}"))
                    : "";
                sb.AppendLine($"func {method.Key}({paramsStr}) -> Variant:");
                sb.AppendLine("    pass");
                sb.AppendLine();
            }

            var originalCode = $"# Duck type constraints for '{node.Label}'\n" +
                              $"# Required methods: {methodsList}\n" +
                              $"# Required properties: {propsList}";
            var resultCode = sb.ToString().TrimEnd();

            // Interface generation is not yet implemented - show preview only
            var canApply = false;

            var dialogResult = await previewDialog.ShowForResult(
                title,
                originalCode,
                resultCode,
                canApply,
                "Generate");

            if (dialogResult.ShouldApply && canApply)
            {
                Logger.Info($"Interface generation: Creating for '{node.Label}'");
                GenerateInterfaceRequested?.Invoke(_currentScript, duckType);
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
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

    #endregion
}
