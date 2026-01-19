using Godot;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Bottom dock panel for displaying all diagnostics (errors, warnings, hints) from the project.
/// </summary>
[Tool]
internal partial class ProblemsDock : Control
{
    private Label _headerLabel;
    private Tree _resultsTree;
    private OptionButton _groupByOption;
    private OptionButton _filterBySeverityOption;
    private Button _refreshButton;
    private CheckButton _autoRefreshToggle;
    private Label _statusLabel;

    private GDPluginDiagnosticService? _diagnosticService;
    private GDScriptProject? _ScriptProject;
    private GDProblemsGroupingMode _groupingMode = GDProblemsGroupingMode.ByFile;
    private GDDiagnosticSeverity? _filterSeverity; // null = all severities

    /// <summary>
    /// Event fired when user wants to navigate to a diagnostic.
    /// </summary>
    public event Action<string, int, int>? NavigateToItem;

    public override void _Ready()
    {
        Logger.Info("ProblemsDock._Ready() called");
        Name = "Problems";
        CreateUI();
    }

    /// <summary>
    /// Initializes the dock with required dependencies.
    /// </summary>
    public void Initialize(GDPluginDiagnosticService diagnosticService, GDScriptProject ScriptProject)
    {
        Logger.Info("ProblemsDock.Initialize() called");
        _diagnosticService = diagnosticService;
        _ScriptProject = ScriptProject;

        // Ensure UI is created (since _Ready may not be called)
        if (_filterBySeverityOption == null)
            CreateUI();

        // Subscribe to diagnostic events
        _diagnosticService.OnDiagnosticsChanged += OnDiagnosticsChanged;
        _diagnosticService.OnProjectAnalysisCompleted += OnProjectAnalysisCompleted;

        // Initial refresh
        RefreshDisplay();
    }

    private void CreateUI()
    {
        // Prevent double creation
        if (_resultsTree != null)
            return;

        Logger.Info($"ProblemsDock.CreateUI() called, GetChildCount={GetChildCount()}");

        // Main container
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Toolbar
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(toolbar);

        // Header label
        _headerLabel = new Label
        {
            Text = "Problems",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        toolbar.AddChild(_headerLabel);

        // Group by dropdown
        toolbar.AddChild(new Label { Text = "Group:" });
        _groupByOption = new OptionButton();
        _groupByOption.AddItem("By File", (int)GDProblemsGroupingMode.ByFile);
        _groupByOption.AddItem("By Severity", (int)GDProblemsGroupingMode.BySeverity);
        _groupByOption.AddItem("By Category", (int)GDProblemsGroupingMode.ByCategory);
        _groupByOption.ItemSelected += OnGroupByChanged;
        toolbar.AddChild(_groupByOption);

        // Filter by severity dropdown
        toolbar.AddChild(new Label { Text = "Filter:" });
        _filterBySeverityOption = new OptionButton();
        _filterBySeverityOption.AddItem("All", 0);
        _filterBySeverityOption.AddItem("Errors", 1);
        _filterBySeverityOption.AddItem("Warnings", 2);
        _filterBySeverityOption.AddItem("Hints", 3);
        _filterBySeverityOption.ItemSelected += OnFilterChanged;
        toolbar.AddChild(_filterBySeverityOption);

        // Auto-refresh toggle
        _autoRefreshToggle = new CheckButton
        {
            Text = "Auto",
            ButtonPressed = true,
            TooltipText = "Auto-refresh when diagnostics change"
        };
        toolbar.AddChild(_autoRefreshToggle);

        // Refresh button
        _refreshButton = new Button
        {
            Text = "",
            TooltipText = "Refresh diagnostics"
        };
        try
        {
            _refreshButton.Icon = EditorInterface.Singleton.GetBaseControl().GetThemeIcon("Reload", "EditorIcons");
        }
        catch { }
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Results tree
        _resultsTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HideRoot = true,
            SelectMode = Tree.SelectModeEnum.Single,
            Columns = 4
        };
        _resultsTree.SetColumnTitle(0, "Severity");
        _resultsTree.SetColumnTitle(1, "Message");
        _resultsTree.SetColumnTitle(2, "File");
        _resultsTree.SetColumnTitle(3, "Line");
        _resultsTree.SetColumnExpand(0, false);
        _resultsTree.SetColumnExpand(1, true);
        _resultsTree.SetColumnExpand(2, false);
        _resultsTree.SetColumnExpand(3, false);
        _resultsTree.SetColumnCustomMinimumWidth(0, 70);
        _resultsTree.SetColumnCustomMinimumWidth(2, 150);
        _resultsTree.SetColumnCustomMinimumWidth(3, 50);
        _resultsTree.ItemActivated += OnItemActivated;
        mainVBox.AddChild(_resultsTree);

        // Status bar
        _statusLabel = new Label
        {
            Text = "Ready",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        mainVBox.AddChild(_statusLabel);

        // Minimum size
        CustomMinimumSize = new Vector2(400, 200);
    }

    private void OnDiagnosticsChanged(GDDiagnosticsChangedEventArgs args)
    {
        if (!_autoRefreshToggle.ButtonPressed)
            return;

        Callable.From(RefreshDisplay).CallDeferred();
    }

    private void OnProjectAnalysisCompleted(GDPluginProjectAnalysisCompletedEventArgs args)
    {
        Callable.From(RefreshDisplay).CallDeferred();
    }

    private void RefreshDisplay()
    {
        _resultsTree.Clear();

        if (_diagnosticService == null)
        {
            _headerLabel.Text = "Problems";
            return;
        }

        var allDiagnostics = _diagnosticService.GetAllDiagnostics();

        // Apply filter
        var filteredDiagnostics = _filterSeverity == null
            ? allDiagnostics
            : allDiagnostics.Where(d => d.Severity == _filterSeverity).ToList();

        var summary = _diagnosticService.GetProjectSummary();
        _headerLabel.Text = $"Problems ({summary.ErrorCount} errors, {summary.WarningCount} warnings, {summary.HintCount} hints)";

        if (filteredDiagnostics.Count == 0)
        {
            UpdateStatus("No problems found");
            return;
        }

        var root = _resultsTree.CreateItem();

        switch (_groupingMode)
        {
            case GDProblemsGroupingMode.ByFile:
                DisplayGroupedByFile(root, filteredDiagnostics);
                break;
            case GDProblemsGroupingMode.BySeverity:
                DisplayGroupedBySeverity(root, filteredDiagnostics);
                break;
            case GDProblemsGroupingMode.ByCategory:
                DisplayGroupedByCategory(root, filteredDiagnostics);
                break;
        }

        UpdateStatus($"Found {filteredDiagnostics.Count} problems");
    }

    private void DisplayGroupedByFile(TreeItem root, IEnumerable<GDPluginDiagnostic> diagnostics)
    {
        var byFile = diagnostics
            .Where(d => d.Script != null)
            .GroupBy(d => d.Script!.FullPath)
            .OrderBy(g => g.Key);

        foreach (var fileGroup in byFile)
        {
            var fileName = System.IO.Path.GetFileName(fileGroup.Key);
            var errorCount = fileGroup.Count(d => d.Severity == GDDiagnosticSeverity.Error);
            var warningCount = fileGroup.Count(d => d.Severity == GDDiagnosticSeverity.Warning);

            var fileItem = _resultsTree.CreateItem(root);
            fileItem.SetText(0, "");
            fileItem.SetText(1, $"{fileName} ({errorCount}E, {warningCount}W)");
            fileItem.SetText(2, "");
            fileItem.SetText(3, "");

            try
            {
                fileItem.SetIcon(0, EditorInterface.Singleton.GetBaseControl().GetThemeIcon("GDScript", "EditorIcons"));
            }
            catch { }

            fileItem.Collapsed = false;

            foreach (var diag in fileGroup.OrderBy(d => d.StartLine))
            {
                CreateDiagnosticRow(fileItem, diag);
            }
        }
    }

    private void DisplayGroupedBySeverity(TreeItem root, IEnumerable<GDPluginDiagnostic> diagnostics)
    {
        var severities = new[] { GDDiagnosticSeverity.Error, GDDiagnosticSeverity.Warning, GDDiagnosticSeverity.Info, GDDiagnosticSeverity.Hint };

        foreach (var severity in severities)
        {
            var group = diagnostics.Where(d => d.Severity == severity).ToList();
            if (group.Count == 0)
                continue;

            var severityItem = _resultsTree.CreateItem(root);
            severityItem.SetText(0, GetSeverityText(severity));
            severityItem.SetText(1, $"({group.Count})");
            severityItem.SetText(2, "");
            severityItem.SetText(3, "");
            severityItem.SetCustomColor(0, GetSeverityColor(severity));

            try
            {
                severityItem.SetIcon(0, EditorInterface.Singleton.GetBaseControl().GetThemeIcon(GetSeverityIcon(severity), "EditorIcons"));
            }
            catch { }

            severityItem.Collapsed = false;

            foreach (var diag in group.OrderBy(d => d.Script?.FullPath).ThenBy(d => d.StartLine))
            {
                CreateDiagnosticRow(severityItem, diag);
            }
        }
    }

    private void DisplayGroupedByCategory(TreeItem root, IEnumerable<GDPluginDiagnostic> diagnostics)
    {
        var byCategory = diagnostics
            .GroupBy(d => d.Category)
            .OrderBy(g => g.Key);

        foreach (var categoryGroup in byCategory)
        {
            var categoryItem = _resultsTree.CreateItem(root);
            categoryItem.SetText(0, GetCategoryText(categoryGroup.Key));
            categoryItem.SetText(1, $"({categoryGroup.Count()})");
            categoryItem.SetText(2, "");
            categoryItem.SetText(3, "");
            categoryItem.Collapsed = false;

            foreach (var diag in categoryGroup.OrderBy(d => d.Script?.FullPath).ThenBy(d => d.StartLine))
            {
                CreateDiagnosticRow(categoryItem, diag);
            }
        }
    }

    private void CreateDiagnosticRow(TreeItem parent, GDPluginDiagnostic diag)
    {
        var row = _resultsTree.CreateItem(parent);
        row.SetText(0, GetSeverityText(diag.Severity));
        row.SetText(1, $"[{diag.RuleId}] {diag.Message}");
        row.SetText(2, diag.Script != null ? System.IO.Path.GetFileName(diag.Script.FullPath) : "");
        row.SetText(3, (diag.StartLine + 1).ToString());

        // Store for navigation
        row.SetMetadata(0, ProjectSettings.LocalizePath(diag.Script?.FullPath ?? ""));
        row.SetMetadata(1, diag.StartLine);
        row.SetMetadata(2, diag.StartColumn);

        row.SetCustomColor(0, GetSeverityColor(diag.Severity));

        try
        {
            row.SetIcon(0, EditorInterface.Singleton.GetBaseControl().GetThemeIcon(GetSeverityIcon(diag.Severity), "EditorIcons"));
        }
        catch { }
    }

    private void OnItemActivated()
    {
        var selected = _resultsTree.GetSelected();
        if (selected == null)
            return;

        var resourcePath = selected.GetMetadata(0);
        var line = selected.GetMetadata(1);
        var column = selected.GetMetadata(2);

        if (resourcePath.VariantType == Variant.Type.String &&
            line.VariantType == Variant.Type.Int &&
            column.VariantType == Variant.Type.Int)
        {
            var path = resourcePath.AsString();
            if (!string.IsNullOrEmpty(path))
            {
                NavigateToItem?.Invoke(path, (int)line + 1, (int)column);
            }
        }
    }

    private void OnGroupByChanged(long index)
    {
        _groupingMode = (GDProblemsGroupingMode)index;
        RefreshDisplay();
    }

    private void OnFilterChanged(long index)
    {
        _filterSeverity = index switch
        {
            0 => null,
            1 => GDDiagnosticSeverity.Error,
            2 => GDDiagnosticSeverity.Warning,
            3 => GDDiagnosticSeverity.Hint,
            _ => null
        };
        RefreshDisplay();
    }

    private async void OnRefreshPressed()
    {
        _statusLabel.Text = "Analyzing...";
        _refreshButton.Disabled = true;

        try
        {
            if (_diagnosticService != null)
            {
                await _diagnosticService.AnalyzeProjectAsync(forceRefresh: true);
            }
        }
        finally
        {
            _refreshButton.Disabled = false;
            RefreshDisplay();
        }
    }

    private void UpdateStatus(string status)
    {
        _statusLabel.Text = status;
    }

    private static string GetSeverityText(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => "Error",
            GDDiagnosticSeverity.Warning => "Warning",
            GDDiagnosticSeverity.Info => "Info",
            GDDiagnosticSeverity.Hint => "Hint",
            _ => "Unknown"
        };
    }

    private static Color GetSeverityColor(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => new Color(1.0f, 0.4f, 0.4f),
            GDDiagnosticSeverity.Warning => new Color(1.0f, 0.8f, 0.2f),
            GDDiagnosticSeverity.Info => new Color(0.6f, 0.8f, 1.0f),
            GDDiagnosticSeverity.Hint => new Color(0.6f, 0.8f, 1.0f),
            _ => new Color(0.8f, 0.8f, 0.8f)
        };
    }

    private static string GetSeverityIcon(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => "StatusError",
            GDDiagnosticSeverity.Warning => "StatusWarning",
            GDDiagnosticSeverity.Info => "StatusSuccess",
            GDDiagnosticSeverity.Hint => "StatusSuccess",
            _ => "StatusSuccess"
        };
    }

    private static string GetCategoryText(GDDiagnosticCategory category)
    {
        return category switch
        {
            GDDiagnosticCategory.Syntax => "Syntax",
            GDDiagnosticCategory.Style => "Style",
            GDDiagnosticCategory.Formatting => "Formatting",
            GDDiagnosticCategory.Performance => "Performance",
            GDDiagnosticCategory.BestPractice => "Best Practice",
            GDDiagnosticCategory.Correctness => "Correctness",
            _ => "Other"
        };
    }
}

/// <summary>
/// How to group problems in the problems dock.
/// </summary>
internal enum GDProblemsGroupingMode
{
    ByFile,
    BySeverity,
    ByCategory
}
