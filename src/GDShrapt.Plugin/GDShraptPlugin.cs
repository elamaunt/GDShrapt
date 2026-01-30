using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using GDShrapt.CLI.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GDShrapt.Plugin.Tests")]
[assembly: InternalsVisibleTo("GDShrapt.Pro.Plugin")]

namespace GDShrapt.Plugin;

public partial class GDShraptPlugin : EditorPlugin
{
    private bool _inited;
    private GDScriptProject _scriptProject;
    private TabContainer _tabContainer;
    private Dictionary<Commands, Command> _commands;

    readonly ConditionalWeakTable<object, TabController> _weakTabControllersTable = new ConditionalWeakTable<object, TabController>();
    readonly ConditionalWeakTable<GDScriptFile, TabController> _weakScriptTabControllersTable = new ConditionalWeakTable<GDScriptFile, TabController>();

    readonly HashSet<MenuButton> _injectedButtons = new HashSet<MenuButton>();

    private ReferencesDock _referencesDock;
    private Action<string, int, int, int> _referencesDockNavigateHandler;
    private Control _referencesDockButton;
    private TodoTagsDock _todoTagsDock;
    private Control _todoTagsDockButton;
    private GDTodoTagsScanner _todoTagsScanner;
    private ProblemsDock _problemsDock;
    private Control _problemsDockButton;
    private AstViewerDock _astViewerDock;
    private Action<string, int, int, int> _astViewerDockNavigateHandler;
    private Control _astViewerDockButton;
    private ReplDock _replDock;
    private Control _replDockButton;

    // Diagnostics system
    private GDConfigManager _configManager;
    private GDCacheManager _cacheManager;
    private GDPluginDiagnosticService _diagnosticService;
    private GDBackgroundAnalyzer _backgroundAnalyzer;
    private NotificationPanel _notificationPanel;
    private GDQuickFixHandler _quickFixHandler;

    // Scene file watching (using events from GDSceneTypesProvider in Semantics)

    // Refactoring system
    private GDRefactoringActionProvider _refactoringActionProvider;

    // Formatting service
    private FormattingService _formattingService;

    // Type inference for completion (using Semantics)
    private GDTypeResolver _typeResolver;

    // Service registry for handlers (Rule 11 - all access through handlers)
    private GDServiceRegistry _serviceRegistry;

    // UI dialogs
    private AboutPanel _aboutPanel;
    private GDTypeFlowPanel _typeFlowPanel;

    // Project Settings integration
    private ProjectSettingsRegistry _settingsRegistry;

    internal GDScriptProject ScriptProject => _scriptProject;
    internal GDPluginDiagnosticService GDPluginDiagnosticService => _diagnosticService;
    internal GDConfigManager ConfigManager => _configManager;
    internal GDRefactoringActionProvider GDRefactoringActionProvider => _refactoringActionProvider;
    internal FormattingService FormattingService => _formattingService;
    internal GDQuickFixHandler GDQuickFixHandler => _quickFixHandler;
    internal GDTypeResolver TypeResolver => _typeResolver;
    internal IGDServiceRegistry ServiceRegistry => _serviceRegistry;

    public override void _Ready()
    {
        try
        {
            _scriptProject = new GDScriptProject(new GodotEditorProjectContext(), new GDScriptProjectOptions()
            {
                EnableSceneTypesProvider = true,
                EnableFileWatcher = true
            });

            // Initialize configuration
            _configManager = new GDConfigManager(_scriptProject.ProjectPath, watchForChanges: true);

            // Initialize Project Settings integration
            _settingsRegistry = new ProjectSettingsRegistry(_configManager);
            _settingsRegistry.RegisterAllSettings();
            _settingsRegistry.SyncToProjectSettings();
            ProjectSettings.Save();

            // Initialize caching
            if (_configManager.Config.Plugin?.Cache?.Enabled ?? true)
            {
                _cacheManager = new GDCacheManager(_scriptProject.ProjectPath);
            }

            // Initialize diagnostic service
            _diagnosticService = new GDPluginDiagnosticService(_scriptProject, _configManager, _cacheManager);
            _diagnosticService.OnDiagnosticsChanged += OnDiagnosticsChanged;
            _diagnosticService.OnProjectAnalysisCompleted += OnProjectAnalysisCompleted;

            // Initialize background analyzer
            _backgroundAnalyzer = new GDBackgroundAnalyzer(_diagnosticService, _scriptProject);

            // Initialize quick fix handler
            _quickFixHandler = new GDQuickFixHandler(_diagnosticService, _configManager);

            // Initialize scene file watching using GDSceneTypesProvider from Semantics
            _scriptProject.SceneTypesProvider.NodeRenameDetected += OnNodeRenameDetected;
            _scriptProject.SceneTypesProvider.EnableFileWatcher();

            // Initialize refactoring system
            _refactoringActionProvider = new GDRefactoringActionProvider();

            // Initialize formatting service
            _formattingService = new FormattingService(_configManager);

            _scriptProject.LoadScripts();
            _scriptProject.LoadScenes();

            // Initialize type resolver for completion (using Semantics)
            // Includes all providers: Godot types, project types, autoloads, and scene types
            _typeResolver = _scriptProject.CreateTypeResolver();

            // Initialize service registry for handlers (Rule 11 - all access through handlers)
            _serviceRegistry = new GDServiceRegistry();
            _serviceRegistry.LoadModules(_scriptProject, new GDBaseModule());

            _scriptProject.AnalyzeAll();

            // Initialize UI and commands after all services are ready
            Init();

            Logger.Debug("_Ready completed");
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            throw;
        }
    }

    public override void _EnterTree()
    {
        Logger.Debug("_EnterTree");

        SetProcessUnhandledInput(true);
        SetProcessUnhandledKeyInput(true);
        SetProcessInput(true);

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        // Unsubscribe from ScriptEditor events to prevent memory leak
        try
        {
            var scriptEditor = EditorInterface.Singleton?.GetScriptEditor();
            if (scriptEditor != null)
            {
                scriptEditor.VisibilityChanged -= OnVisibilityChanged;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error unsubscribing from ScriptEditor: {ex.Message}");
        }

        // Shutdown scene watcher
        if (_scriptProject?.SceneTypesProvider != null)
        {
            _scriptProject.SceneTypesProvider.NodeRenameDetected -= OnNodeRenameDetected;
            _scriptProject.SceneTypesProvider.DisableFileWatcher();
        }

        // Shutdown diagnostics system
        _backgroundAnalyzer?.Dispose();
        _backgroundAnalyzer = null;

        if (_diagnosticService != null)
        {
            _diagnosticService.OnDiagnosticsChanged -= OnDiagnosticsChanged;
            _diagnosticService.OnProjectAnalysisCompleted -= OnProjectAnalysisCompleted;
            _diagnosticService.Dispose();
            _diagnosticService = null;
        }

        _cacheManager?.Dispose();
        _cacheManager = null;

        // Unregister Project Settings
        _settingsRegistry?.UnregisterAllSettings();
        _settingsRegistry = null;

        _configManager?.Dispose();
        _configManager = null;

        DetachTabControllers();
        DetachMenuButtons();
        DetachDocks();
        DetachNotificationPanel();

        _scriptProject?.Dispose();
        _scriptProject = null;

        base._ExitTree();
        Logger.Debug("_ExitTree");
    }

    private double _settingsCheckTimer = 0;
    private const double SettingsCheckInterval = 2.0; // Check every 2 seconds

    public override void _Process(double delta)
    {
        // Periodically check if Project Settings have changed
        _settingsCheckTimer += delta;
        if (_settingsCheckTimer >= SettingsCheckInterval)
        {
            _settingsCheckTimer = 0;
            if (_settingsRegistry != null && _settingsRegistry.HasProjectSettingsChanged())
            {
                Logger.Debug("Project Settings changed, syncing to config...");
                _settingsRegistry.SyncFromProjectSettings();
                _configManager?.LoadConfig(); // Trigger config changed event
            }
        }

        base._Process(delta);
    }

    private void DetachNotificationPanel()
    {
        if (_notificationPanel != null)
        {
            _notificationPanel.FormatCodeRequested -= OnFormatCodeRequested;
            _notificationPanel.ShowAllProblemsRequested -= OnShowAllProblemsRequested;
            _notificationPanel.QueueFree();
            _notificationPanel = null;
        }
    }

    private void OnDiagnosticsChanged(GDDiagnosticsChangedEventArgs args)
    {
        Logger.Debug($"Diagnostics changed for {args.Script.FullPath}: {args.Diagnostics.Count} issues");

        // Update the overlay for the affected script
        foreach (var kvp in _weakTabControllersTable)
        {
            var controller = _weakTabControllersTable.GetValue(kvp, _ => null);
            if (controller?.GDPluginScriptReference?.FullPath == args.Script.FullPath)
            {
                controller.UpdateDiagnostics(args.Script);
            }
        }

        // Update notification panel
        UpdateNotificationPanel(args.Script.FullPath, args.Summary);
    }

    private void OnProjectAnalysisCompleted(GDPluginProjectAnalysisCompletedEventArgs args)
    {
        Logger.Info($"Project analysis completed: {args.Summary} ({args.FilesAnalyzed} files in {args.Duration.TotalMilliseconds:F0}ms)");

        // Show notification if there are issues
        if (args.Summary.HasIssues && (_configManager?.Config.Plugin?.Notifications?.ShowProjectSummaryOnStartup ?? true))
        {
            UpdateNotificationPanel(null, args.Summary);
        }
    }

    private void UpdateNotificationPanel(string? scriptPath, GDDiagnosticSummary summary)
    {
        if (_notificationPanel == null || !(_configManager.Config.Plugin?.Notifications?.Enabled ?? true))
            return;

        // Must update UI from main thread
        Callable.From(() =>
        {
            if (_notificationPanel == null)
                return;

            _notificationPanel.UpdateSummary(summary, scriptPath);

            // Position in corner
            var editor = EditorInterface.Singleton.GetScriptEditor();
            if (editor != null)
            {
                _notificationPanel.PositionInCorner(editor, NotificationPanel.Corner.BottomRight);
            }
        }).CallDeferred();
    }

    private void OnFormatCodeRequested()
    {
        Logger.Debug("Fix formatting requested from notification panel");

        if (!TryGetController(out var controller))
            return;

        controller.RequestCommand(Commands.FormatCode);
    }

    private void OnShowAllProblemsRequested()
    {
        Logger.Debug("Show all problems requested from notification panel");

        // Show the Problems dock
        if (_problemsDock != null && _problemsDockButton != null)
        {
            MakeBottomPanelItemVisible(_problemsDock);
        }
    }

    private void DetachDocks()
    {
        if (_referencesDock != null)
        {
            if (_referencesDockNavigateHandler != null)
                _referencesDock.NavigateToReference -= _referencesDockNavigateHandler;
            _referencesDockNavigateHandler = null;
            RemoveControlFromBottomPanel(_referencesDock);
            _referencesDock.QueueFree();
            _referencesDock = null;
        }

        if (_todoTagsDock != null)
        {
            _todoTagsDock.NavigateToItem -= OnNavigateToTodoItem;
            _todoTagsDock.OpenSettingsRequested -= OnTodoSettingsRequested;
            RemoveControlFromBottomPanel(_todoTagsDock);
            _todoTagsDock.QueueFree();
            _todoTagsDock = null;
        }

        if (_problemsDock != null)
        {
            _problemsDock.NavigateToItem -= OnNavigateToProblem;
            RemoveControlFromBottomPanel(_problemsDock);
            _problemsDock.QueueFree();
            _problemsDock = null;
        }

        if (_astViewerDock != null)
        {
            if (_astViewerDockNavigateHandler != null)
                _astViewerDock.NavigateToCode -= _astViewerDockNavigateHandler;
            _astViewerDockNavigateHandler = null;
            RemoveControlFromBottomPanel(_astViewerDock);
            _astViewerDock.QueueFree();
            _astViewerDock = null;
        }

        if (_replDock != null)
        {
            RemoveControlFromBottomPanel(_replDock);
            _replDock.QueueFree();
            _replDock = null;
        }

        _todoTagsScanner?.Dispose();
        _todoTagsScanner = null;
    }

    private void OnNavigateToTodoItem(string resourcePath, int line, int column)
    {
        OnNavigateToReference(resourcePath, line, column);
    }

    private void OnNavigateToProblem(string resourcePath, int line, int column)
    {
        OnNavigateToReference(resourcePath, line, column);
    }

    private void OnTodoSettingsRequested()
    {
        Logger.Debug("TODO Tags settings requested");
        var settingsPanel = new TodoTagsSettingsPanel();
        settingsPanel.Initialize(_configManager);
        settingsPanel.SettingsSaved += () =>
        {
            // Trigger rescan after settings are saved
            _ = _todoTagsScanner?.ScanProjectAsync();
        };

        // Add to the editor and show
        EditorInterface.Singleton.GetBaseControl().AddChild(settingsPanel);
        settingsPanel.PopupCentered();
    }

    private void OnNavigateToReference(string filePath, int line, int column)
    {
        OnNavigateToReference(filePath, line, column, column);
    }

    private void OnNavigateToReference(string filePath, int line, int column, int endColumn)
    {
        Logger.Debug($"Navigate to reference: {filePath}:{line}:{column}-{endColumn}");

        if (string.IsNullOrEmpty(filePath))
            return;

        // Convert to resource path if needed
        var resourcePath = filePath;
        if (System.IO.Path.IsPathRooted(filePath))
        {
            var projectPath = ProjectSettings.GlobalizePath("res://");
            if (filePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                resourcePath = "res://" + filePath.Substring(projectPath.Length).Replace("\\", "/");
            }
        }

        // Open the script
        var script = ResourceLoader.Load<Script>(resourcePath);
        if (script != null)
        {
            // Switch to Script editor tab before opening the script
            EditorInterface.Singleton.SetMainScreenEditor("Script");

            // Open and navigate to the line
            // Note: EditScript positions the caret at the specified line/column.
            // For token selection, we'd need access to the CodeEdit after EditScript completes,
            // but since EditScript may open a new tab, we defer selection via signal or delay.
            // Line is expected to be 1-based from caller (EditScript uses 1-based lines).
            EditorInterface.Singleton.EditScript(script, line, column);

            // TODO: Implement token selection after EditScript if needed
            // Currently, EditScript only sets caret position, not selection
        }
    }

    internal GDScriptFile? GetScript(string path)
    {
        return _scriptProject.GetScriptByResourcePath(path);
    }

    /// <summary>
    /// Stops all background activities including analysis and TODO scanning.
    /// Call this method when the plugin needs to gracefully stop all background work.
    /// </summary>
    public void StopAllActivities()
    {
        // Stop background analyzer (cancels pending work and waits for completion)
        _backgroundAnalyzer?.Stop();

        // Clear all diagnostics to reset state
        _diagnosticService?.ClearAllDiagnostics();
    }

    private void Init()
    {
        if (_inited)
            return;

        if (!FindTabContainer())
            return;

        _tabContainer.TabSelected += OnTabSelected;

        // Create the references dock
        _referencesDock = new ReferencesDock();
        _referencesDock.Initialize();
        _referencesDockNavigateHandler = (path, line, col, endCol) => OnNavigateToReference(path, line, col, endCol);
        _referencesDock.NavigateToReference += _referencesDockNavigateHandler;
        _referencesDockButton = AddControlToBottomPanel(_referencesDock, "Find References");

        // Create the TODO tags dock
        _todoTagsScanner = new GDTodoTagsScanner(_scriptProject, _configManager);
        _todoTagsDock = new TodoTagsDock();
        _todoTagsDock.Initialize(_todoTagsScanner, _configManager);
        _todoTagsDock.NavigateToItem += OnNavigateToTodoItem;
        _todoTagsDock.OpenSettingsRequested += OnTodoSettingsRequested;
        _todoTagsDockButton = AddControlToBottomPanel(_todoTagsDock, "TODO Tags");

        // Create the Problems dock
        _problemsDock = new ProblemsDock();
        _problemsDock.Initialize(_diagnosticService, _scriptProject);
        _problemsDock.NavigateToItem += OnNavigateToProblem;
        _problemsDockButton = AddControlToBottomPanel(_problemsDock, "Problems");

        // Create the AST Viewer dock (if enabled)
        _astViewerDock = new AstViewerDock();
        _astViewerDock.Initialize(this, _scriptProject);
        _astViewerDockNavigateHandler = (path, line, col, endCol) => OnNavigateToReference(path, line, col, endCol);
        _astViewerDock.NavigateToCode += _astViewerDockNavigateHandler;
        _astViewerDockButton = AddControlToBottomPanel(_astViewerDock, "AST Viewer");

        // Create the REPL dock
        _replDock = new ReplDock();
        _replDock.Initialize(this);
        _replDockButton = AddControlToBottomPanel(_replDock, "REPL");

        // Start TODO tags scan if enabled
        var todoConfig = _configManager.Config.Plugin?.TodoTags;
        if ((todoConfig?.Enabled ?? true) && (todoConfig?.ScanOnStartup ?? true))
        {
            _ = _todoTagsScanner.ScanProjectAsync();
        }

        var findReferencesCommand = new FindReferencesCommand(this);
        findReferencesCommand.SetReferencesDock(_referencesDock);

        _commands = new Dictionary<Commands, Command>()
        {
            { Commands.AutoComplete, new AutoCompleteCommand(this) },
            { Commands.ExtractMethod, new ExtractMethodCommand(this) },
            { Commands.FormatCode, new FormatCodeCommand(this) },
            { Commands.GoToDefinition, new GoToDefinitionCommand(this) },
            { Commands.RemoveComments, new RemoveCommentsCommand(this) },
            { Commands.Rename, new RenameIdentifierCommand(this) },
            { Commands.FindReferences, findReferencesCommand }
        };

        AttachTabControllersIfNeeded();

        EditorInterface.Singleton.GetScriptEditor().VisibilityChanged += OnVisibilityChanged;

        // Create notification panel
        CreateNotificationPanel();

        // Start background analysis
        if (_backgroundAnalyzer != null && _configManager.Config.Linting.Enabled)
        {
            _backgroundAnalyzer.Start();
            _backgroundAnalyzer.QueueProjectAnalysis();
        }

        _inited = true;
        Logger.Info("Initialized properly");
        return;
    }

    private void CreateNotificationPanel()
    {
        if (!(_configManager.Config.Plugin?.Notifications?.Enabled ?? true))
            return;

        var editor = EditorInterface.Singleton.GetScriptEditor();
        if (editor == null)
            return;

        _notificationPanel = new NotificationPanel();
        _notificationPanel.FormatCodeRequested += OnFormatCodeRequested;
        _notificationPanel.ShowAllProblemsRequested += OnShowAllProblemsRequested;

        // Add to editor as child
        //editor.AddChild(_notificationPanel);
        _notificationPanel.PositionInCorner(editor, NotificationPanel.Corner.BottomRight);
        _notificationPanel.Hide(); // Start hidden until there are issues

        Logger.Debug("Notification panel created");
    }

    private void OnVisibilityChanged()
    {
        Logger.Debug($"OnVisibilityChanged called for ScriptEditor");
        GetOrCreateTabControllerForCurrentTab();
        AttachTabControllersIfNeeded();
    }

    private void OnTabSelected(long index)
    {
        Logger.Debug($"OnTabSelected: {index}");
        GetOrCreateTabControllerForCurrentTab();
        UpdateCurrentTabScript();
        UpdateShrapterMenuButton();
    }

    private void UpdateCurrentTabScript()
    {
        Logger.Debug($"Trying update current tab script");

        var tab = _tabContainer.GetCurrentTabControl();

        if (tab == null)
            return;

        var controller = _weakTabControllersTable.GetOrDefault(tab);

        if (controller == null)
            return;

        var title = _tabContainer.GetTabTitle(_tabContainer.CurrentTab);
        Logger.Debug($"Updating script for tab '{title}'");

        var script = EditorInterface.Singleton.GetScriptEditor().GetCurrentScript();

        if (script == null)
            return;

        var map = _scriptProject.GetScriptByResourcePath(script.ResourcePath);

        if (map != null)
        {
            _weakScriptTabControllersTable.AddOrUpdate(map, controller);

            // Queue priority analysis for current file
            _backgroundAnalyzer?.QueueScriptAnalysis(map, priority: true);
        }

        controller.SetControlledScript(script);
    }

    internal TabController? OpenScript(GDScriptFile map)
    {
        Logger.Debug($"OpenTabForScript '{map.FullPath}'");
        AttachTabControllersIfNeeded();


        var controller = _weakScriptTabControllersTable.GetOrDefault(map);

        if (controller == null || !controller.IsInTree)
        {
            Logger.Debug($"Controller for this Script map is null or not in the tree.");
            controller = OpenNewScriptTab(map);
            _weakScriptTabControllersTable.AddOrUpdate(map, controller);
        }

        SelectTabForController(controller);

        return controller;
    }

    private void SelectTabForController(TabController controller)
    {
        Logger.Debug($"SelectTabForController requested");
        var newIndex = controller.Tab.GetIndex();
        Logger.Debug($"Requested tab with index: {newIndex}");

        _tabContainer.CurrentTab = newIndex;
    }

    internal TabController? OpenNewScriptTab(GDScriptFile map)
    {
        Logger.Debug($"OpenNewScript '{map.FullPath}'");
        var res = ResourceLoader.Load(map.FullPath);
        EditorInterface.Singleton.EditResource(res);
        AttachTabControllersIfNeeded();
        UpdateCurrentTabScript();

        return _weakScriptTabControllersTable.GetOrCreateValue(map);
    }

    private void AttachTabControllersIfNeeded()
    {
        Logger.Debug("AttachTabControllersIfNeeded requested");

        var count = _tabContainer.GetTabCount();
        for (int i = 0; i < count; i++)
        {
            var tab = _tabContainer.GetTabControl(i);

            var controller = _weakTabControllersTable.GetOrDefault(tab);

            if (tab.IsClass("ScriptTextEditor"))
            {
                if (controller == null)
                {
                    Logger.Debug($"Attach tab controller for tab '{_tabContainer.GetTabTitle(i)}'");
                    controller = new TabController(this, tab);
                    controller.ReferenceCountClicked += OnReferenceCountClicked;
                    _weakTabControllersTable.Add(tab, controller);
                }
            }
        };

        UpdateCurrentTabScript();
    }

    private void OnReferenceCountClicked(string symbolName, int line)
    {
        Logger.Debug($"Reference count clicked from overlay: '{symbolName}' at line {line}");

        // Show references in the dock
        if (_referencesDock == null)
            return;

        // Collect references for this symbol across the project
        var references = new List<ReferenceItem>();

        foreach (var script in _scriptProject.ScriptFiles)
        {
            if (script.Class == null)
                continue;

            var filePath = script.FullPath;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDShrapt.Reader.GDIdentifier id && id.Sequence == symbolName)
                {
                    var kind = DetermineReferenceKind(id);
                    var context = GetContextWithHighlight(id, symbolName, out var hlStart, out var hlEnd);

                    references.Add(new ReferenceItem(
                        filePath,
                        id.StartLine,
                        id.StartColumn,
                        id.EndColumn,
                        context,
                        kind,
                        hlStart,
                        hlEnd
                    ));
                }
            }
        }

        _referencesDock.ShowReferences(symbolName, references);

        // Make the dock visible
        if (_referencesDockButton != null)
        {
            MakeBottomPanelItemVisible(_referencesDock);
        }
    }

    private static GDPluginReferenceKind DetermineReferenceKind(GDShrapt.Reader.GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDShrapt.Reader.GDMethodDeclaration)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDVariableDeclaration)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDVariableDeclarationStatement)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDSignalDeclaration)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDParameterDeclaration)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDEnumDeclaration)
            return GDPluginReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDInnerClassDeclaration)
            return GDPluginReferenceKind.Declaration;

        // Check if it's a call
        if (parent is GDShrapt.Reader.GDIdentifierExpression idExpr)
        {
            if (idExpr.Parent is GDShrapt.Reader.GDCallExpression)
                return GDPluginReferenceKind.Call;
        }

        if (parent is GDShrapt.Reader.GDMemberOperatorExpression memberOp)
        {
            if (memberOp.Parent is GDShrapt.Reader.GDCallExpression)
                return GDPluginReferenceKind.Call;
        }

        return GDPluginReferenceKind.Read;
    }

    private static string GetContextWithHighlight(GDShrapt.Reader.GDIdentifier identifier, string symbolName, out int highlightStart, out int highlightEnd)
    {
        highlightStart = 0;
        highlightEnd = 0;

        var parent = identifier.Parent;

        if (parent is GDShrapt.Reader.GDMethodDeclaration method)
        {
            var text = $"func {method.Identifier?.Sequence ?? ""}(...)";
            highlightStart = 5; // After "func "
            highlightEnd = highlightStart + (method.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDShrapt.Reader.GDVariableDeclaration variable)
        {
            var text = $"var {variable.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (variable.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDShrapt.Reader.GDVariableDeclarationStatement localVar)
        {
            var text = $"var {localVar.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (localVar.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDShrapt.Reader.GDSignalDeclaration signal)
        {
            var text = $"signal {signal.Identifier?.Sequence ?? ""}";
            highlightStart = 7; // After "signal "
            highlightEnd = highlightStart + (signal.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDShrapt.Reader.GDParameterDeclaration param)
        {
            var text = $"param {param.Identifier?.Sequence ?? ""}";
            highlightStart = 6; // After "param "
            highlightEnd = highlightStart + (param.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        // For expressions, try to get statement context
        var current = parent;
        while (current != null && !(current is GDShrapt.Reader.GDStatement) && !(current is GDShrapt.Reader.GDClassMember))
        {
            current = current.Parent;
        }

        if (current != null)
        {
            var text = current.ToString();
            var wasTruncated = false;
            if (text.Length > 60)
            {
                text = text.Substring(0, 57) + "...";
                wasTruncated = true;
            }
            text = text.Trim().Replace("\n", " ").Replace("\r", "");

            // Find the symbol within the context text
            if (!string.IsNullOrEmpty(symbolName))
            {
                var idx = text.IndexOf(symbolName, StringComparison.Ordinal);
                if (idx >= 0 && (!wasTruncated || idx + symbolName.Length <= 57))
                {
                    highlightStart = idx;
                    highlightEnd = idx + symbolName.Length;
                }
            }

            return text;
        }

        return identifier.Sequence;
    }

    private void DetachTabControllers()
    {
        Logger.Debug("DetachTabControllers requested");

        var count = _tabContainer.GetTabCount();
        for (int i = 0; i < count; i++)
        {
            var tab = _tabContainer.GetTabControl(i);

            var controller = _weakTabControllersTable.GetOrDefault(tab);
            if (controller != null)
            {
                controller.ReferenceCountClicked -= OnReferenceCountClicked;
                controller.CleanUp();
            }
            _weakTabControllersTable.Remove(tab);
        };
    }

    private void DetachMenuButtons()
    {
        Logger.Debug("DetachMenuButtons requested");

        foreach (var item in _injectedButtons)
            item.GetParent()?.RemoveChild(item);

        _injectedButtons.Clear();
    }

    private bool FindTabContainer()
    {
        var editor = EditorInterface.Singleton.GetScriptEditor();

        if (editor == null)
            return false;

        // Recursively search for TabContainer in the script editor hierarchy
        // UI structure may vary between Godot versions
        _tabContainer = FindNodeOfType<TabContainer>(editor);

        if (_tabContainer != null)
        {
            Logger.Debug($"Found TabContainer: {_tabContainer.Name}");
            return true;
        }

        Logger.Warning("TabContainer not found in script editor hierarchy");
        return false;
    }

    private static T? FindNodeOfType<T>(Node root) where T : Node
    {
        if (root is T found)
            return found;

        foreach (var child in root.GetChildren())
        {
            var result = FindNodeOfType<T>(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private bool UpdateShrapterMenuButton()
    {
        var editor = EditorInterface.Singleton.GetScriptEditor();

        if (editor == null)
            return false;

        if (editor.GetChildCount() < 1)
            return false;

        var container = editor.GetChild(0); // VBoxContainer

        if (container.GetChildCount() < 1)
            return false;

        container = container.GetChild(0); // HBoxContainer

        if (container.GetChildCount() < 1)
            return false;

        Logger.Debug($"Updating menu buttons");

        foreach (var node in container.GetChildren().OfType<HBoxContainer>())
        {
            /*var oldButton = node
               .GetChildren()
               .OfType<GDShraptMenuButton>()
               .FirstOrDefault();

            if (oldButton == null)
            {
                var newButton = new GDShraptMenuButton(this);
                _injectedButtons.Add(newButton);
                node.AddChild(newButton);
            }
            else
            {
                // Just move existing button to end, don't recreate
                node.MoveChild(oldButton, node.GetChildCount() - 1);
            }*/

        }

        return true;
    }

    public override string _GetPluginName()
    {
        return "GDShrapt";
    }

    public override bool _HasMainScreen()
    {
        return false; // No main screen panel - using bottom docks instead
    }

    public override Texture2D _GetPluginIcon()
    {
        try
        {
            return EditorInterface.Singleton.GetBaseControl().GetThemeIcon("Script", "EditorIcons");
        }
        catch
        {
            return null;
        }
    }

    public override void _MakeVisible(bool visible)
    {
        // No main panel to show/hide - all content is in bottom docks
    }

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        Logger.Debug("ForwardCanvasGuiInput");
        return base._ForwardCanvasGuiInput(@event);
    }

    public override bool _Handles(GodotObject @object)
    {
        return @object is GDScript;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!_inited)
            return;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent)
            return;

        try
        {
            Logger.Debug($"_UnhandledKeyInput code {keyEvent.Keycode}:{keyEvent.Pressed}");

            TabController controller;

            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Tab)
            {
                if (!TryGetController(out controller))
                    return;
                controller.RequestCommand(Commands.AutoComplete);
                return;
            }

            if (keyEvent.IsEcho() || !keyEvent.Pressed)
                return;

            if (!TryGetController(out controller))
                return;

            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.P)
                controller.RequestCommand(Commands.RemoveComments);

            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.E)
                controller.RequestCommand(Commands.ExtractMethod);

            if (keyEvent.AltPressed && keyEvent.Keycode == Key.F)
                controller.RequestCommand(Commands.FormatCode);

            if ((keyEvent.CtrlPressed && keyEvent.Keycode == Key.R) || keyEvent.Keycode == Key.F2)
                controller.RequestCommand(Commands.Rename);

            if (keyEvent.Keycode == Key.F12)
                controller.RequestCommand(Commands.GoToDefinition);

            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.F12)
                controller.RequestCommand(Commands.GoToDefinition);

            base._UnhandledKeyInput(@event);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }
    }

    private bool TryGetController(out TabController controller)
    {
        controller = GetOrCreateTabControllerForCurrentTab();

        if (controller == null)
        {
            Logger.Debug("GDShraptController not found");
            return false;
        }

        return true;
    }

    private TabController GetOrCreateTabControllerForCurrentTab()
    {
        var currentTabControl = _tabContainer.GetCurrentTabControl();

        if (currentTabControl == null)
        {
            Logger.Debug("Tab control not found");
            return null;
        }

        var controller = _weakTabControllersTable.GetOrDefault(currentTabControl);

        if (controller == null)
        {
            Logger.Debug("Requesting new tab controller");
            AttachTabControllersIfNeeded();
            controller = _weakTabControllersTable.GetOrDefault(currentTabControl);
        }

        return controller;
    }

    internal void ExecuteCommand(Commands command, IScriptEditor controller)
    {
        _commands[command].Execute(controller).PrintIfFailed();
    }

    /// <summary>
    /// Shows the Type Flow panel for a symbol at the specified line.
    /// </summary>
    public void ShowTypeFlowPanel(string symbolName, int line, GDScriptFile scriptFile)
    {
        Logger.Info($"ShowTypeFlowPanel: {symbolName} at line {line}");

        try
        {
            // Create the window if it doesn't exist
            if (_typeFlowPanel == null)
            {
                _typeFlowPanel = new GDTypeFlowPanel();
                var typeFlowHandler = _serviceRegistry.GetService<IGDTypeFlowHandler>();
                var symbolsHandler = _serviceRegistry.GetService<IGDSymbolsHandler>();
                _typeFlowPanel.Initialize(_scriptProject, EditorInterface.Singleton, typeFlowHandler, symbolsHandler);
                _typeFlowPanel.NavigateToRequested += (path, lineNum) =>
                {
                    Logger.Info($"GDTypeFlowPanel navigate to: {path}:{lineNum}");
                    if (!string.IsNullOrEmpty(path))
                    {
                        OpenResource(path);
                    }
                };

                // Handle type annotation requests
                _typeFlowPanel.AddTypeAnnotationRequested += OnTypeFlowAddTypeAnnotation;

                // Handle type guard requests
                _typeFlowPanel.AddTypeGuardRequested += OnTypeFlowAddTypeGuard;

                // Handle interface generation requests
                _typeFlowPanel.GenerateInterfaceRequested += OnTypeFlowGenerateInterface;

                // Add window to editor base control so it's in the scene tree
                EditorInterface.Singleton.GetBaseControl().AddChild(_typeFlowPanel);
            }

            // Update project reference in case it changed
            var tfHandler = _serviceRegistry.GetService<IGDTypeFlowHandler>();
            var symHandler = _serviceRegistry.GetService<IGDSymbolsHandler>();
            _typeFlowPanel.SetProject(_scriptProject, tfHandler, symHandler);

            // Show for the symbol
            _typeFlowPanel.ShowForSymbol(symbolName, line, scriptFile);

            // Show centered
            _typeFlowPanel.PopupCentered();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error showing GDTypeFlowPanel: {ex.Message}");
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Handles type annotation request from the Type Flow panel.
    /// </summary>
    private void OnTypeFlowAddTypeAnnotation(GDScriptFile scriptFile, GDTypeAnnotationPlan annotation)
    {
        if (scriptFile == null || annotation?.Edit == null)
        {
            Logger.Warning("OnTypeFlowAddTypeAnnotation: Invalid parameters");
            return;
        }

        try
        {
            Logger.Info($"Applying type annotation: {annotation.IdentifierName}: {annotation.InferredType.TypeName}");

            // Open the script and apply the edit
            var resPath = ToResPath(scriptFile.FullPath);
            if (string.IsNullOrEmpty(resPath))
            {
                Logger.Warning($"Cannot convert path to res://: {scriptFile.FullPath}");
                return;
            }

            OpenResource(resPath);

            // Get the current editor and apply the edit
            var scriptEditor = EditorInterface.Singleton.GetScriptEditor();
            var currentEditor = scriptEditor?.GetCurrentEditor();
            if (currentEditor == null)
            {
                Logger.Warning("No current editor available");
                return;
            }

            var codeEdit = FindCodeEdit(currentEditor);
            if (codeEdit == null)
            {
                Logger.Warning("No CodeEdit found in current editor");
                return;
            }

            // Apply the edit directly to CodeEdit
            ApplyTextEditToCodeEdit(codeEdit, annotation.Edit);

            // Reload the script file to update the AST
            scriptFile.Reload(codeEdit.Text);

            Logger.Info($"Type annotation applied successfully to '{annotation.IdentifierName}'");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error applying type annotation: {ex.Message}");
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Handles type guard request from the Type Flow panel.
    /// </summary>
    private void OnTypeFlowAddTypeGuard(GDScriptFile scriptFile, string symbolName, IReadOnlyList<string> types)
    {
        if (scriptFile == null || string.IsNullOrEmpty(symbolName))
        {
            Logger.Warning("OnTypeFlowAddTypeGuard: Invalid parameters");
            return;
        }

        // Type guard generation is a Pro feature - silent in Base per Rule 12 (STATE.md)
        Logger.Debug($"Type guard requested for '{symbolName}' with types: {string.Join(", ", types)} - Pro feature, no-op in Base");
    }

    /// <summary>
    /// Handles interface generation request from the Type Flow panel.
    /// </summary>
    private void OnTypeFlowGenerateInterface(GDScriptFile scriptFile, GDDuckType duckType)
    {
        if (scriptFile == null || duckType == null)
        {
            Logger.Warning("OnTypeFlowGenerateInterface: Invalid parameters");
            return;
        }

        // Interface generation is a Pro feature - silent in Base per Rule 12 (STATE.md)
        var methodCount = duckType.RequiredMethods.Count;
        var propCount = duckType.RequiredProperties.Count;
        Logger.Debug($"Interface generation requested: {methodCount} methods, {propCount} properties - Pro feature, no-op in Base");
    }

    /// <summary>
    /// Applies a GDTextEdit to a CodeEdit control.
    /// </summary>
    private void ApplyTextEditToCodeEdit(CodeEdit codeEdit, GDTextEdit edit)
    {
        if (codeEdit == null || edit == null)
            return;

        if (string.IsNullOrEmpty(edit.OldText))
        {
            // Insert only - no text to replace
            codeEdit.SetCaretLine(edit.Line);
            codeEdit.SetCaretColumn(edit.Column);
            codeEdit.InsertTextAtCaret(edit.NewText ?? "");
        }
        else
        {
            // Replace existing text
            var oldTextLines = edit.OldText.Split('\n');
            var endLine = edit.Line + oldTextLines.Length - 1;
            var endColumn = oldTextLines.Length > 1
                ? oldTextLines[^1].Length
                : edit.Column + edit.OldText.Length;

            codeEdit.Select(edit.Line, edit.Column, endLine, endColumn);
            codeEdit.Cut();
            codeEdit.InsertTextAtCaret(edit.NewText ?? "");
        }
    }

    /// <summary>
    /// Converts an absolute file path to a res:// path.
    /// </summary>
    private string ToResPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return null;

        if (absolutePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            return absolutePath;

        var projectPath = ProjectSettings.GlobalizePath("res://");
        if (absolutePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = absolutePath.Substring(projectPath.Length);
            return "res://" + relativePath.Replace('\\', '/');
        }

        return null;
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

    public void OpenPreferences()
    {
        Logger.Debug("OpenPreferences requested");

        // Open Godot's native Project Settings window
        try
        {
            // Get the Project Settings window via the editor
            var projectSettingsDialog = EditorInterface.Singleton.GetBaseControl()
                .GetTree()
                .Root
                .GetNodeOrNull<Window>("Project Settings");

            if (projectSettingsDialog != null)
            {
                projectSettingsDialog.Show();
            }
            else
            {
                // Try alternative approach - use menu action
                // The Project Settings can be opened via Project menu
                OS.Alert("Open Project > Project Settings and search for 'GDShrapt' to configure the plugin.", "GDShrapt Settings");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not open Project Settings: {ex.Message}");
            OS.Alert("Open Project > Project Settings and search for 'GDShrapt' to configure the plugin.", "GDShrapt Settings");
        }
    }

    public void OpenAbout()
    {
        Logger.Debug("OpenAbout requested");

        try
        {
            // Create the panel if it doesn't exist
            if (_aboutPanel == null)
            {
                _aboutPanel = new AboutPanel();
                _aboutPanel.CloseRequested += () => _aboutPanel.Hide();
                AddChild(_aboutPanel);
            }

            // Center the window on screen
            var screenSize = DisplayServer.ScreenGetSize();
            var windowSize = _aboutPanel.Size;
            var centeredPos = new Vector2I(
                (screenSize.X - windowSize.X) / 2,
                (screenSize.Y - windowSize.Y) / 2
            );
            _aboutPanel.Position = centeredPos;

            // Show the panel
            _aboutPanel.PopupCentered();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error opening About dialog: {ex.Message}");
            Logger.Error(ex);
        }
    }

    public void OpenResource(string path)
    {
        Logger.Debug($"OpenResource '{path}'");

        try
        {
            var res = ResourceLoader.Load(path);
            EditorInterface.Singleton.EditResource(res);
        }
        catch (Exception)
        {
            Logger.Debug($"Unable to open resource");
        }
    }

    #region Scene File Watcher

    private void OnNodeRenameDetected(object? sender, GDShrapt.Semantics.GDNodeRenameDetectedEventArgs e)
    {
        Logger.Debug($"Detected {e.Renames.Count} node rename(s) in {e.ScenePath}");

        // Use Godot's deferred call to process on main thread
        Godot.Callable.From(async () =>
        {
            foreach (var rename in e.Renames)
            {
                try
                {
                    await ProcessNodeRename(rename);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing node rename: {ex.Message}");
                    Logger.Error(ex);
                }
            }
        }).CallDeferred();
    }

    private async System.Threading.Tasks.Task ProcessNodeRename(GDDetectedNodeRename rename)
    {
        // Find all GDScript references to the old name (using Semantics service)
        var referenceFinder = new GDNodePathReferenceFinder(_scriptProject);
        var references = referenceFinder
            .FindGDScriptReferences(rename.OldName)
            .ToList();

        if (!references.Any())
        {
            Logger.Debug($"No GDScript references found for '{rename.OldName}'");
            return;
        }

        Logger.Debug($"Found {references.Count} GDScript references for '{rename.OldName}'");

        // Show dialog
        var dialog = new NodeRenamingDialog();
        AddChild(dialog);

        // Set context - the scene already has the new name
        dialog.SetWarning($"Detected rename: '{rename.OldName}' -> '{rename.NewName}'");

        var result = await dialog.ShowForResult(rename.OldName, references);
        dialog.QueueFree();

        if (result == null || !result.SelectedReferences.Any())
        {
            Logger.Debug("Node rename cancelled or no references selected");
            return;
        }

        // Apply changes to GDScript files only (scene is already updated)
        // Use Semantics GDNodePathRenamer
        var renamer = new GDNodePathRenamer(_scriptProject);

        // Apply rename with the NEW name from the scene
        var renameResult = renamer.ApplyRename(result.SelectedReferences, rename.OldName, rename.NewName);

        if (!renameResult.Success)
        {
            Logger.Error($"Node rename failed: {renameResult.ErrorMessage}");
            return;
        }

        // Mark our own write to avoid triggering another rename detection
        _scriptProject.SceneTypesProvider?.MarkOwnWrite();

        // Save modified scripts
        var modifiedScripts = renamer.GetModifiedScripts(result.SelectedReferences);
        foreach (var script in modifiedScripts)
        {
            SaveModifiedScript(script);
        }

        Logger.Debug($"Applied rename: '{rename.OldName}' -> '{rename.NewName}' to {modifiedScripts.Count} scripts");
    }

    private void SaveModifiedScript(GDScriptFile ScriptFile)
    {
        if (ScriptFile?.Class == null)
            return;

        try
        {
            var content = ScriptFile.Class.ToString();
            System.IO.File.WriteAllText(ScriptFile.FullPath, content);
            Logger.Debug($"Saved modified script: {ScriptFile.FullPath}");

            // Reload the script in the editor if it's open
            ScriptFile.Reload();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save script {ScriptFile.FullPath}: {ex.Message}");
        }
    }

    #endregion
}
