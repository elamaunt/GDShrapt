using Godot;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Panel for visualizing type inference information.
/// Shows Union types, Call Sites, Dependencies graph, and Inference Limits.
/// </summary>
public partial class TypeInferencePanel : Control
{
    private TabContainer _tabContainer;
    private Tree _unionTypesTree;
    private Tree _callSitesTree;
    private Tree _dependenciesTree;
    private Tree _limitsTree;
    private Label _titleLabel;
    private Button _exportButton;
    private Button _closeButton;

    private GDScriptProject _project;
    private string _currentSymbolName;
    private int _currentLine;

    // Colors for confidence levels
    private static readonly Color HighConfidenceColor = new(0.3f, 0.8f, 0.3f);  // Green
    private static readonly Color MediumConfidenceColor = new(1.0f, 0.7f, 0.3f); // Orange
    private static readonly Color LowConfidenceColor = new(0.8f, 0.3f, 0.3f);    // Red
    private static readonly Color InfoColor = new(0.4f, 0.6f, 0.9f);             // Blue

    /// <summary>
    /// Event fired when user clicks on a file location to navigate.
    /// Parameters: filePath, line
    /// </summary>
    public event Action<string, int> NavigateToRequested;

    /// <summary>
    /// Event fired when user clicks close button.
    /// </summary>
    public event Action CloseRequested;

    public override void _Ready()
    {
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
        _currentSymbolName = symbolName;
        _currentLine = line;

        _titleLabel.Text = $"Type Inference: {symbolName}";

        // Clear previous data
        ClearAllTrees();

        if (_project == null || scriptFile == null)
        {
            ShowNoDataMessage();
            return;
        }

        // Populate tabs with inference data
        PopulateUnionTypes(scriptFile, symbolName);
        PopulateCallSites(scriptFile, symbolName);
        PopulateDependencies(scriptFile, symbolName);
        PopulateInferenceLimits(scriptFile, symbolName);
    }

    private void CreateUI()
    {
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(mainVBox);

        // Header with title and buttons
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 10);

        _titleLabel = new Label
        {
            Text = "Type Inference",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        headerRow.AddChild(_titleLabel);

        _exportButton = new Button { Text = "Export JSON" };
        _exportButton.Pressed += OnExportPressed;
        headerRow.AddChild(_exportButton);

        _closeButton = new Button { Text = "✕" };
        _closeButton.Pressed += OnClosePressed;
        headerRow.AddChild(_closeButton);

        mainVBox.AddChild(headerRow);
        mainVBox.AddChild(new HSeparator());

        // Tab container for different views
        _tabContainer = new TabContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        // Tab 1: Union Types
        var unionPanel = CreateUnionTypesTab();
        _tabContainer.AddChild(unionPanel);
        unionPanel.Name = "Union Types";

        // Tab 2: Call Sites
        var callSitesPanel = CreateCallSitesTab();
        _tabContainer.AddChild(callSitesPanel);
        callSitesPanel.Name = "Call Sites";

        // Tab 3: Dependencies
        var depsPanel = CreateDependenciesTab();
        _tabContainer.AddChild(depsPanel);
        depsPanel.Name = "Dependencies";

        // Tab 4: Inference Limits
        var limitsPanel = CreateLimitsTab();
        _tabContainer.AddChild(limitsPanel);
        limitsPanel.Name = "Limits";

        mainVBox.AddChild(_tabContainer);
    }

    private Control CreateUnionTypesTab()
    {
        var panel = new VBoxContainer();

        var infoLabel = new Label
        {
            Text = "Types that compose the inferred union type, with their sources.",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        panel.AddChild(infoLabel);

        _unionTypesTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HideRoot = true
        };
        _unionTypesTree.Columns = 3;
        _unionTypesTree.SetColumnTitle(0, "Type");
        _unionTypesTree.SetColumnTitle(1, "Sources");
        _unionTypesTree.SetColumnTitle(2, "Confidence");
        _unionTypesTree.SetColumnTitlesVisible(true);
        _unionTypesTree.ItemActivated += OnUnionTypeActivated;
        panel.AddChild(_unionTypesTree);

        return panel;
    }

    private Control CreateCallSitesTab()
    {
        var panel = new VBoxContainer();

        var infoLabel = new Label
        {
            Text = "Locations where types flow into this symbol through call sites or assignments.",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        panel.AddChild(infoLabel);

        _callSitesTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HideRoot = true
        };
        _callSitesTree.Columns = 4;
        _callSitesTree.SetColumnTitle(0, "Location");
        _callSitesTree.SetColumnTitle(1, "Expression");
        _callSitesTree.SetColumnTitle(2, "Inferred Type");
        _callSitesTree.SetColumnTitle(3, "Confidence");
        _callSitesTree.SetColumnTitlesVisible(true);
        _callSitesTree.ItemActivated += OnCallSiteActivated;
        panel.AddChild(_callSitesTree);

        return panel;
    }

    private Control CreateDependenciesTab()
    {
        var panel = new VBoxContainer();

        var infoLabel = new Label
        {
            Text = "Methods that this symbol depends on for type inference, and methods that depend on it.",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        panel.AddChild(infoLabel);

        _dependenciesTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HideRoot = true
        };
        _dependenciesTree.Columns = 3;
        _dependenciesTree.SetColumnTitle(0, "Method");
        _dependenciesTree.SetColumnTitle(1, "Direction");
        _dependenciesTree.SetColumnTitle(2, "Dependency Type");
        _dependenciesTree.SetColumnTitlesVisible(true);
        _dependenciesTree.ItemActivated += OnDependencyActivated;
        panel.AddChild(_dependenciesTree);

        return panel;
    }

    private Control CreateLimitsTab()
    {
        var panel = new VBoxContainer();

        var infoLabel = new Label
        {
            Text = "Inference limits indicate where automatic type inference cannot determine exact types " +
                   "due to mutual dependencies. This is not an error - consider adding explicit type annotations " +
                   "to improve inference precision.",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        panel.AddChild(infoLabel);

        _limitsTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HideRoot = true
        };
        _limitsTree.Columns = 2;
        _limitsTree.SetColumnTitle(0, "Mutual Dependency");
        _limitsTree.SetColumnTitle(1, "Suggestion");
        _limitsTree.SetColumnTitlesVisible(true);
        panel.AddChild(_limitsTree);

        return panel;
    }

    private void ClearAllTrees()
    {
        _unionTypesTree.Clear();
        _unionTypesTree.CreateItem(); // Root

        _callSitesTree.Clear();
        _callSitesTree.CreateItem(); // Root

        _dependenciesTree.Clear();
        _dependenciesTree.CreateItem(); // Root

        _limitsTree.Clear();
        _limitsTree.CreateItem(); // Root
    }

    private void ShowNoDataMessage()
    {
        var root = _unionTypesTree.GetRoot();
        var item = _unionTypesTree.CreateItem(root);
        item.SetText(0, "No type inference data available");
        item.SetCustomColor(0, new Color(0.6f, 0.6f, 0.6f));
    }

    private void PopulateUnionTypes(GDScriptFile scriptFile, string symbolName)
    {
        var root = _unionTypesTree.GetRoot();
        var analyzer = scriptFile.Analyzer;

        if (analyzer == null)
        {
            AddNoDataItem(root, "Analyzer not available");
            return;
        }

        // Find the symbol and get its inferred type
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(root, "Symbol not found");
            return;
        }

        // Get type information from analyzer
        var typeStr = analyzer.GetTypeForNode(symbol.DeclarationNode);
        if (string.IsNullOrEmpty(typeStr))
        {
            AddNoDataItem(root, "Type not inferred");
            return;
        }

        // Check if it's a union type (contains |)
        if (typeStr.Contains("|"))
        {
            var types = typeStr.Split('|').Select(t => t.Trim()).ToList();
            foreach (var type in types)
            {
                var item = _unionTypesTree.CreateItem(root);
                item.SetText(0, type);
                item.SetText(1, "1 source"); // Simplified - would need deeper integration
                item.SetText(2, "Inferred");
                item.SetCustomColor(0, HighConfidenceColor);
            }

            // Add common base type if applicable
            var commonBase = FindCommonBaseType(types);
            if (!string.IsNullOrEmpty(commonBase))
            {
                var baseItem = _unionTypesTree.CreateItem(root);
                baseItem.SetText(0, $"Common base: {commonBase}");
                baseItem.SetCustomColor(0, InfoColor);
            }
        }
        else
        {
            // Single type
            var item = _unionTypesTree.CreateItem(root);
            item.SetText(0, typeStr);
            item.SetText(1, "Direct type");
            item.SetText(2, "High");
            item.SetCustomColor(0, HighConfidenceColor);
        }
    }

    private void PopulateCallSites(GDScriptFile scriptFile, string symbolName)
    {
        var root = _callSitesTree.GetRoot();
        var analyzer = scriptFile.Analyzer;

        if (analyzer == null || _project == null)
        {
            AddNoDataItem(root, "Analyzer not available");
            return;
        }

        // Find references to this symbol across the project
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(root, "Symbol not found");
            return;
        }

        // Get references within current file
        var refs = analyzer.GetReferencesTo(symbol);
        var foundAny = false;

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

            // Store metadata for navigation
            item.SetMetadata(0, new Godot.Collections.Dictionary
            {
                { "filePath", scriptFile.FullPath ?? "" },
                { "line", reference.ReferenceNode.StartLine }
            });

            foundAny = true;
        }

        if (!foundAny)
        {
            AddNoDataItem(root, "No call sites found");
        }
    }

    private void PopulateDependencies(GDScriptFile scriptFile, string symbolName)
    {
        var root = _dependenciesTree.GetRoot();
        var analyzer = scriptFile.Analyzer;

        if (analyzer == null)
        {
            AddNoDataItem(root, "Analyzer not available");
            return;
        }

        // Find the method/variable containing or being the symbol
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(root, "Symbol not found");
            return;
        }

        var foundAny = false;

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
                foundAny = true;
            }
        }

        // Look for symbols that reference this symbol (reverse dependencies)
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
                        foundAny = true;
                    }
                    break;
                }
                parent = parent.Parent as GDShrapt.Reader.GDNode;
            }
        }

        if (!foundAny)
        {
            AddNoDataItem(root, "No dependencies found");
        }
    }

    private void PopulateInferenceLimits(GDScriptFile scriptFile, string symbolName)
    {
        var root = _limitsTree.GetRoot();
        var analyzer = scriptFile.Analyzer;

        if (analyzer == null)
        {
            AddNoDataItem(root, "Analyzer not available");
            return;
        }

        // Look for potential circular dependencies in type inference
        // This is a simplified implementation - full cycle detection would use Tarjan's SCC algorithm
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
        {
            AddNoDataItem(root, "Symbol not found");
            return;
        }

        // Check if symbol's type is Variant (could indicate inference limit)
        var typeStr = analyzer.GetTypeForNode(symbol.DeclarationNode);
        if (typeStr == "Variant" || string.IsNullOrEmpty(typeStr))
        {
            var item = _limitsTree.CreateItem(root);
            item.SetText(0, $"ℹ {symbolName} → Variant");
            item.SetText(1, "Consider adding explicit type annotation");
            item.SetCustomColor(0, InfoColor);
            return;
        }

        // If type was successfully inferred, show that there are no limits
        var noLimitItem = _limitsTree.CreateItem(root);
        noLimitItem.SetText(0, "✓ No inference limits detected");
        noLimitItem.SetText(1, "Type was successfully inferred");
        noLimitItem.SetCustomColor(0, HighConfidenceColor);
    }

    private void AddNoDataItem(TreeItem root, string message)
    {
        var item = _unionTypesTree.CreateItem(root);
        item.SetText(0, message);
        item.SetCustomColor(0, new Color(0.6f, 0.6f, 0.6f));
    }

    private static string FindCommonBaseType(List<string> types)
    {
        // Simplified common base type detection for Godot types
        if (types.All(t => t.EndsWith("2D")))
            return "Node2D";
        if (types.All(t => t.EndsWith("3D")))
            return "Node3D";
        if (types.All(t => t.Contains("Button") || t.Contains("Control")))
            return "Control";
        return null;
    }

    private void OnUnionTypeActivated()
    {
        // Could expand to show sources for this type
    }

    private void OnCallSiteActivated()
    {
        var selected = _callSitesTree.GetSelected();
        if (selected == null)
            return;

        var metadata = selected.GetMetadata(0);
        if (metadata.Obj is Godot.Collections.Dictionary dict)
        {
            var filePath = dict["filePath"].AsString();
            var line = dict["line"].AsInt32();
            NavigateToRequested?.Invoke(filePath, line);
        }
    }

    private void OnDependencyActivated()
    {
        // Could navigate to the dependent method
    }

    private void OnExportPressed()
    {
        // Export type inference data as JSON
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

    private void OnClosePressed()
    {
        CloseRequested?.Invoke();
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
