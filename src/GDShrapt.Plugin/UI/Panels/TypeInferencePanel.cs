using static Godot.Control;

namespace GDShrapt.Plugin;

/// <summary>
/// Dialog for visualizing type inference information.
/// Shows Signature, Inferred Type, Call Sites, Dependencies, and Inference Limits
/// in collapsible sections. Parameters in signature are clickable.
/// </summary>
public partial class TypeInferencePanel : AcceptDialog
{
    // Main containers
    private VBoxContainer _mainVBox;
    private ScrollContainer _scrollContainer;
    private VBoxContainer _contentContainer;

    // Header
    private HBoxContainer _headerRow;
    private Button _backButton;
    private Label _titleLabel;
    private Button _exportButton;

    // Sections
    private InferenceSection _signatureSection;
    private InferenceSection _inferredTypeSection;
    private InferenceSection _callSitesSection;
    private InferenceSection _dependenciesSection;
    private InferenceSection _limitsSection;

    // Section content
    private RichTextLabel _signatureLabel;
    private Tree _inferredTypeTree;
    private Tree _callSitesTree;
    private Tree _dependenciesTree;
    private Tree _limitsTree;

    // State
    private GDScriptProject _project;
    private GDScriptFile _currentScript;
    private string _currentSymbolName;
    private int _currentLine;
    private bool _uiCreated = false;
    private Stack<NavigationEntry> _navigationStack = new();

    // Colors for confidence levels
    private static readonly Color HighConfidenceColor = new(0.3f, 0.8f, 0.3f);   // Green
    private static readonly Color MediumConfidenceColor = new(1.0f, 0.7f, 0.3f); // Orange
    private static readonly Color LowConfidenceColor = new(0.8f, 0.3f, 0.3f);    // Red
    private static readonly Color InfoColor = new(0.4f, 0.6f, 0.9f);              // Blue
    private static readonly Color KeywordColor = new(0.8f, 0.55f, 0.75f);         // Pink (func, var, etc.)
    private static readonly Color TypeColor = new(0.4f, 0.76f, 0.65f);            // Cyan
    private static readonly Color ParamColor = new(0.85f, 0.85f, 0.75f);          // Light cream
    private static readonly Color SymbolColor = new(0.6f, 0.6f, 0.6f);            // Gray (brackets, colons)

    /// <summary>
    /// Event fired when user clicks on a file location to navigate.
    /// </summary>
    public event Action<string, int> NavigateToRequested;

    public TypeInferencePanel()
    {
        Title = "Type Inference";
        Size = new Vector2I(650, 500);
        Exclusive = false;
        Transient = true;  // Always on top
        OkButtonText = "Close";
    }

    public override void _Ready()
    {
        EnsureUICreated();
    }

    private void EnsureUICreated()
    {
        if (_uiCreated)
            return;

        _uiCreated = true;
        CreateUI();
    }

    /// <summary>
    /// Sets the project for type inference analysis.
    /// </summary>
    public void SetProject(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Shows type inference information for a symbol.
    /// </summary>
    public void ShowForSymbol(string symbolName, int line, GDScriptFile scriptFile)
    {
        EnsureUICreated();

        _currentSymbolName = symbolName;
        _currentLine = line;
        _currentScript = scriptFile;

        // Update window title
        Title = $"Type Inference: {symbolName}";
        _titleLabel.Text = symbolName;

        // Update back button visibility
        _backButton.Visible = _navigationStack.Count > 0;

        // Clear previous data
        ClearAllTrees();

        if (_project == null || scriptFile == null)
        {
            ShowNoDataMessage();
            return;
        }

        // Populate sections with inference data
        PopulateSignature(scriptFile, symbolName);
        PopulateInferredType(scriptFile, symbolName);
        PopulateCallSites(scriptFile, symbolName);
        PopulateDependencies(scriptFile, symbolName);
        PopulateInferenceLimits(scriptFile, symbolName);
    }

    private void CreateUI()
    {
        _mainVBox = new VBoxContainer();
        _mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainVBox.AddThemeConstantOverride("separation", 8);
        AddChild(_mainVBox);

        // Header row with back button, title, and export
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

        _exportButton = new Button { Text = "Export JSON" };
        _exportButton.Pressed += OnExportPressed;
        _headerRow.AddChild(_exportButton);

        _mainVBox.AddChild(_headerRow);
        _mainVBox.AddChild(new HSeparator());

        // Scroll container for all sections
        _scrollContainer = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        _contentContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _contentContainer.AddThemeConstantOverride("separation", 4);

        // Create all sections
        CreateSignatureSection();
        CreateInferredTypeSection();
        CreateCallSitesSection();
        CreateDependenciesSection();
        CreateLimitsSection();

        _scrollContainer.AddChild(_contentContainer);
        _mainVBox.AddChild(_scrollContainer);
    }

    private void CreateSignatureSection()
    {
        _signatureLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SelectionEnabled = false,
            CustomMinimumSize = new Vector2(0, 30)
        };
        _signatureLabel.MetaClicked += OnSignatureMetaClicked;

        _signatureSection = new InferenceSection("SIGNATURE", _signatureLabel);
        _contentContainer.AddChild(_signatureSection);
    }

    private void CreateInferredTypeSection()
    {
        _inferredTypeTree = CreateTree(3);
        _inferredTypeTree.SetColumnTitle(0, "Type");
        _inferredTypeTree.SetColumnTitle(1, "Sources");
        _inferredTypeTree.SetColumnTitle(2, "Confidence");
        _inferredTypeTree.ItemActivated += OnInferredTypeActivated;

        _inferredTypeSection = new InferenceSection("INFERRED TYPE", _inferredTypeTree);
        _contentContainer.AddChild(_inferredTypeSection);
    }

    private void CreateCallSitesSection()
    {
        _callSitesTree = CreateTree(4);
        _callSitesTree.SetColumnTitle(0, "Location");
        _callSitesTree.SetColumnTitle(1, "Expression");
        _callSitesTree.SetColumnTitle(2, "Inferred Type");
        _callSitesTree.SetColumnTitle(3, "Confidence");
        _callSitesTree.ItemActivated += OnCallSiteActivated;

        _callSitesSection = new InferenceSection("CALL SITES", _callSitesTree);
        _contentContainer.AddChild(_callSitesSection);
    }

    private void CreateDependenciesSection()
    {
        _dependenciesTree = CreateTree(3);
        _dependenciesTree.SetColumnTitle(0, "Method");
        _dependenciesTree.SetColumnTitle(1, "Direction");
        _dependenciesTree.SetColumnTitle(2, "Dependency Type");
        _dependenciesTree.ItemActivated += OnDependencyActivated;

        _dependenciesSection = new InferenceSection("DEPENDENCIES", _dependenciesTree);
        _contentContainer.AddChild(_dependenciesSection);
    }

    private void CreateLimitsSection()
    {
        _limitsTree = CreateTree(2);
        _limitsTree.SetColumnTitle(0, "Issue");
        _limitsTree.SetColumnTitle(1, "Suggestion");

        _limitsSection = new InferenceSection("INFERENCE LIMITS", _limitsTree);
        _contentContainer.AddChild(_limitsSection);
    }

    private Tree CreateTree(int columns)
    {
        var tree = new Tree
        {
            CustomMinimumSize = new Vector2(0, 100),
            HideRoot = true
        };
        tree.Columns = columns;
        tree.SetColumnTitlesVisible(true);
        return tree;
    }

    private void ClearAllTrees()
    {
        ClearTree(_inferredTypeTree);
        ClearTree(_callSitesTree);
        ClearTree(_dependenciesTree);
        ClearTree(_limitsTree);
    }

    private void ClearTree(Tree tree)
    {
        if (tree == null) return;
        tree.Clear();
        tree.CreateItem(); // Root
    }

    private void ShowNoDataMessage()
    {
        _signatureLabel.Text = "[color=#999999]No data available[/color]";
        AddNoDataItem(_inferredTypeTree, "No type inference data");
        AddNoDataItem(_callSitesTree, "No call sites found");
        AddNoDataItem(_dependenciesTree, "No dependencies found");
        AddNoDataItem(_limitsTree, "Cannot analyze");
    }

    private void AddNoDataItem(Tree tree, string message)
    {
        var root = tree?.GetRoot();
        if (root == null) return;

        var item = tree.CreateItem(root);
        item.SetText(0, message);
        item.SetCustomColor(0, new Color(0.6f, 0.6f, 0.6f));
    }

    #region Signature Section

    private void PopulateSignature(GDScriptFile scriptFile, string symbolName)
    {
        var analyzer = EnsureAnalyzer(scriptFile);
        if (analyzer == null)
        {
            _signatureLabel.Text = $"[color=#cccccc]{symbolName}[/color]";
            return;
        }

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol?.DeclarationNode == null)
        {
            _signatureLabel.Text = $"[color=#cccccc]{symbolName}[/color]";
            return;
        }

        // Build signature with BBCode
        var signature = BuildSignatureBBCode(symbol, analyzer);
        _signatureLabel.Text = signature;
    }

    private string BuildSignatureBBCode(GDShrapt.Semantics.GDSymbolInfo symbol, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var decl = symbol.DeclarationNode;

        // Handle method declaration
        if (decl is GDShrapt.Reader.GDMethodDeclaration method)
        {
            return BuildMethodSignatureBBCode(method, analyzer);
        }

        // Handle variable declaration
        if (decl is GDShrapt.Reader.GDVariableDeclaration variable)
        {
            return BuildVariableSignatureBBCode(variable, analyzer);
        }

        // Handle parameter declaration
        if (decl is GDShrapt.Reader.GDParameterDeclaration param)
        {
            return BuildParameterSignatureBBCode(param, analyzer);
        }

        // Fallback
        var typeStr = analyzer.GetTypeForNode(decl) ?? "Variant";
        return $"[color=#{ColorToHex(ParamColor)}]{symbol.Name}[/color]: [color=#{ColorToHex(TypeColor)}]{typeStr}[/color]";
    }

    private string BuildMethodSignatureBBCode(GDShrapt.Reader.GDMethodDeclaration method, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        // func keyword
        sb.Append($"[color=#{ColorToHex(KeywordColor)}]func[/color] ");

        // Method name
        var methodName = method.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{methodName}[/color]");

        // Parameters
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

        // Return type
        var returnType = analyzer.GetTypeForNode(method) ?? "void";
        sb.Append($" [color=#{ColorToHex(SymbolColor)}]→[/color] ");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{returnType}[/color]");

        return sb.ToString();
    }

    private string BuildVariableSignatureBBCode(GDShrapt.Reader.GDVariableDeclaration variable, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        // var keyword
        sb.Append($"[color=#{ColorToHex(KeywordColor)}]var[/color] ");

        // Variable name
        var varName = variable.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{varName}[/color]");

        // Type
        var varType = analyzer.GetTypeForNode(variable) ?? "Variant";
        sb.Append($"[color=#{ColorToHex(SymbolColor)}]: [/color]");
        sb.Append($"[color=#{ColorToHex(TypeColor)}]{varType}[/color]");

        return sb.ToString();
    }

    private string BuildParameterSignatureBBCode(GDShrapt.Reader.GDParameterDeclaration param, GDShrapt.Semantics.GDScriptAnalyzer analyzer)
    {
        var sb = new System.Text.StringBuilder();

        // Parameter name
        var paramName = param.Identifier?.Sequence ?? "unknown";
        sb.Append($"[color=#{ColorToHex(ParamColor)}]{paramName}[/color]");

        // Type
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

    private void OnSignatureMetaClicked(Variant meta)
    {
        var paramName = meta.AsString();
        if (string.IsNullOrEmpty(paramName) || _currentScript == null)
            return;

        // Push current symbol to navigation stack
        _navigationStack.Push(new NavigationEntry
        {
            SymbolName = _currentSymbolName,
            Line = _currentLine,
            Script = _currentScript
        });

        // Navigate to the parameter
        ShowForSymbol(paramName, _currentLine, _currentScript);
    }

    private void OnBackPressed()
    {
        if (_navigationStack.Count == 0)
            return;

        var entry = _navigationStack.Pop();
        ShowForSymbol(entry.SymbolName, entry.Line, entry.Script);
    }

    #endregion

    #region Inferred Type Section

    private void PopulateInferredType(GDScriptFile scriptFile, string symbolName)
    {
        var root = _inferredTypeTree?.GetRoot();
        if (root == null) return;

        var analyzer = EnsureAnalyzer(scriptFile);
        if (analyzer == null)
        {
            AddNoDataItem(_inferredTypeTree, "Analyzer not available");
            return;
        }

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(_inferredTypeTree, "Symbol not found");
            return;
        }

        var typeStr = analyzer.GetTypeForNode(symbol.DeclarationNode);
        if (string.IsNullOrEmpty(typeStr))
        {
            AddNoDataItem(_inferredTypeTree, "Type not inferred");
            return;
        }

        // Check if it's a union type
        if (typeStr.Contains("|"))
        {
            var types = typeStr.Split('|').Select(t => t.Trim()).ToList();
            foreach (var type in types)
            {
                var item = _inferredTypeTree.CreateItem(root);
                item.SetText(0, type);
                item.SetText(1, "1 source");
                item.SetText(2, "Inferred");
                item.SetCustomColor(0, HighConfidenceColor);
            }

            // Add common base type if applicable
            var commonBase = FindCommonBaseType(types);
            if (!string.IsNullOrEmpty(commonBase))
            {
                var baseItem = _inferredTypeTree.CreateItem(root);
                baseItem.SetText(0, $"Common base: {commonBase}");
                baseItem.SetCustomColor(0, InfoColor);
            }
        }
        else
        {
            var item = _inferredTypeTree.CreateItem(root);
            item.SetText(0, typeStr);
            item.SetText(1, "Direct type");
            item.SetText(2, "High");
            item.SetCustomColor(0, HighConfidenceColor);
        }

        // Update section title with count
        var count = typeStr.Contains("|") ? typeStr.Split('|').Length : 1;
        _inferredTypeSection.UpdateTitle($"INFERRED TYPE ({count})");
    }

    private void OnInferredTypeActivated()
    {
        // Could expand to show sources for this type
    }

    #endregion

    #region Call Sites Section

    private void PopulateCallSites(GDScriptFile scriptFile, string symbolName)
    {
        var root = _callSitesTree?.GetRoot();
        if (root == null) return;

        var analyzer = EnsureAnalyzer(scriptFile);
        if (analyzer == null || _project == null)
        {
            AddNoDataItem(_callSitesTree, "Analyzer not available");
            return;
        }

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(_callSitesTree, "Symbol not found");
            return;
        }

        var refs = analyzer.GetReferencesTo(symbol);
        var count = 0;

        foreach (var reference in refs)
        {
            if (reference.ReferenceNode == null)
                continue;

            var item = _callSitesTree.CreateItem(root);
            item.SetText(0, $"{System.IO.Path.GetFileName(scriptFile.FullPath ?? "unknown")}:{reference.ReferenceNode.StartLine}");
            item.SetText(1, reference.ReferenceNode.ToString().Truncate(40));
            item.SetText(2, reference.InferredType ?? "Unknown");

            var confidence = reference.Confidence;
            var color = confidence switch
            {
                GDReferenceConfidence.Strict => HighConfidenceColor,
                GDReferenceConfidence.Potential => MediumConfidenceColor,
                _ => LowConfidenceColor
            };
            item.SetText(3, confidence.ToString());
            item.SetCustomColor(2, color);

            item.SetMetadata(0, new Godot.Collections.Dictionary
            {
                { "filePath", scriptFile.FullPath ?? "" },
                { "line", reference.ReferenceNode.StartLine }
            });

            count++;
        }

        if (count == 0)
        {
            AddNoDataItem(_callSitesTree, "No call sites found");
        }

        _callSitesSection.UpdateTitle($"CALL SITES ({count})");
    }

    private void OnCallSiteActivated()
    {
        var selected = _callSitesTree?.GetSelected();
        if (selected == null) return;

        var metadata = selected.GetMetadata(0);
        if (metadata.Obj is Godot.Collections.Dictionary dict)
        {
            var filePath = dict["filePath"].AsString();
            var line = dict["line"].AsInt32();
            NavigateToRequested?.Invoke(filePath, line);
        }
    }

    #endregion

    #region Dependencies Section

    private void PopulateDependencies(GDScriptFile scriptFile, string symbolName)
    {
        var root = _dependenciesTree?.GetRoot();
        if (root == null) return;

        var analyzer = EnsureAnalyzer(scriptFile);
        if (analyzer == null)
        {
            AddNoDataItem(_dependenciesTree, "Analyzer not available");
            return;
        }

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(_dependenciesTree, "Symbol not found");
            return;
        }

        var callsCount = 0;
        var calledByCount = 0;

        // Look for method calls from this symbol's context
        if (symbol.DeclarationNode != null)
        {
            var calls = symbol.DeclarationNode.AllNodes
                .OfType<GDShrapt.Reader.GDCallExpression>()
                .ToList();

            foreach (var call in calls)
            {
                var calledMethod = call.CallerExpression?.ToString();
                if (string.IsNullOrEmpty(calledMethod))
                    continue;

                var item = _dependenciesTree.CreateItem(root);
                item.SetText(0, calledMethod);
                item.SetText(1, "→ Calls");
                item.SetText(2, "Return type dependency");
                item.SetCustomColor(1, InfoColor);
                callsCount++;
            }
        }

        // Look for symbols that reference this symbol
        var refs = analyzer.GetReferencesTo(symbol);
        var callers = new HashSet<string>();

        foreach (var reference in refs)
        {
            var parent = reference.ReferenceNode?.Parent;
            while (parent != null)
            {
                if (parent is GDShrapt.Reader.GDMethodDeclaration method)
                {
                    var methodName = method.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(methodName) && methodName != symbolName && callers.Add(methodName))
                    {
                        var item = _dependenciesTree.CreateItem(root);
                        item.SetText(0, methodName);
                        item.SetText(1, "← Called by");
                        item.SetText(2, "Parameter dependency");
                        item.SetCustomColor(1, MediumConfidenceColor);
                        calledByCount++;
                    }
                    break;
                }
                parent = parent.Parent as GDShrapt.Reader.GDNode;
            }
        }

        if (callsCount == 0 && calledByCount == 0)
        {
            AddNoDataItem(_dependenciesTree, "No dependencies found");
        }

        _dependenciesSection.UpdateTitle($"DEPENDENCIES (→{callsCount} ←{calledByCount})");
    }

    private void OnDependencyActivated()
    {
        // Could navigate to the dependent method
    }

    #endregion

    #region Limits Section

    private void PopulateInferenceLimits(GDScriptFile scriptFile, string symbolName)
    {
        var root = _limitsTree?.GetRoot();
        if (root == null) return;

        var analyzer = EnsureAnalyzer(scriptFile);
        if (analyzer == null)
        {
            AddNoDataItem(_limitsTree, "Analyzer not available");
            return;
        }

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(_limitsTree, "Symbol not found");
            return;
        }

        var typeStr = analyzer.GetTypeForNode(symbol.DeclarationNode);
        if (typeStr == "Variant" || string.IsNullOrEmpty(typeStr))
        {
            var item = _limitsTree.CreateItem(root);
            item.SetText(0, $"ℹ {symbolName} → Variant");
            item.SetText(1, "Consider adding explicit type annotation");
            item.SetCustomColor(0, InfoColor);
            _limitsSection.UpdateTitle("INFERENCE LIMITS (1)");
            return;
        }

        var noLimitItem = _limitsTree.CreateItem(root);
        noLimitItem.SetText(0, "✓ No inference limits detected");
        noLimitItem.SetText(1, "Type was successfully inferred");
        noLimitItem.SetCustomColor(0, HighConfidenceColor);
        _limitsSection.UpdateTitle("INFERENCE LIMITS (0)");
    }

    #endregion

    #region Helpers

    private GDShrapt.Semantics.GDScriptAnalyzer? EnsureAnalyzer(GDScriptFile scriptFile)
    {
        if (scriptFile.Analyzer != null)
            return scriptFile.Analyzer;

        try
        {
            scriptFile.Analyze();
            return scriptFile.Analyzer;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to analyze script: {ex.Message}");
            return null;
        }
    }

    private static string FindCommonBaseType(List<string> types)
    {
        if (types.All(t => t.EndsWith("2D")))
            return "Node2D";
        if (types.All(t => t.EndsWith("3D")))
            return "Node3D";
        if (types.All(t => t.Contains("Button") || t.Contains("Control")))
            return "Control";
        return null;
    }

    private void OnExportPressed()
    {
        var exportData = new Godot.Collections.Dictionary
        {
            { "symbol", _currentSymbolName },
            { "line", _currentLine },
            { "exportedAt", DateTime.UtcNow.ToString("O") }
        };

        var json = Json.Stringify(exportData, "  ");
        DisplayServer.ClipboardSet(json);

        Logger.Info($"Type inference data exported to clipboard for '{_currentSymbolName}'");
    }

    #endregion
}

/// <summary>
/// Navigation entry for back button support.
/// </summary>
internal class NavigationEntry
{
    public string SymbolName { get; set; }
    public int Line { get; set; }
    public GDScriptFile Script { get; set; }
}

/// <summary>
/// Collapsible section for TypeInferencePanel.
/// </summary>
internal partial class InferenceSection : VBoxContainer
{
    private Button _headerButton;
    private Control _content;
    private string _baseTitle;
    private bool _collapsed = false;

    public InferenceSection(string title, Control content)
    {
        _baseTitle = title;

        _headerButton = new Button
        {
            Text = $"▼ {title}",
            Alignment = HorizontalAlignment.Left,
            Flat = true
        };
        _headerButton.AddThemeFontSizeOverride("font_size", 12);
        _headerButton.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _headerButton.Pressed += ToggleCollapse;

        AddChild(_headerButton);

        // Add some padding around content
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddChild(content);

        AddChild(margin);
        _content = margin;

        // Add a subtle separator
        var separator = new HSeparator();
        separator.AddThemeColorOverride("separator", new Color(0.3f, 0.3f, 0.3f));
        AddChild(separator);
    }

    public void UpdateTitle(string newTitle)
    {
        _baseTitle = newTitle;
        _headerButton.Text = (_collapsed ? "▶ " : "▼ ") + newTitle;
    }

    private void ToggleCollapse()
    {
        _collapsed = !_collapsed;
        _content.Visible = !_collapsed;
        _headerButton.Text = (_collapsed ? "▶ " : "▼ ") + _baseTitle;
    }
}

/// <summary>
/// String extension for truncating long strings.
/// </summary>
internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}
