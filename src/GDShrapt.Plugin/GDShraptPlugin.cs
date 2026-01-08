using Godot;
using GDShrapt.Plugin.Api;
using GDShrapt.Plugin.Api.Internal;
using GDShrapt.Plugin.Cache;
using GDShrapt.Plugin.Config;
using GDShrapt.Plugin.Diagnostics;
using GDShrapt.Plugin.Refactoring;
using GDShrapt.Plugin.TodoTags;
using GDShrapt.Plugin.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("GDShrapt.Plugin.Tests")]

namespace GDShrapt.Plugin;

public partial class GDShraptPlugin : EditorPlugin
{
    private bool _inited;
    private GDProjectMap _projectMap;
    private TabContainer _tabContainer;
    private Dictionary<Commands, Command> _commands;
    readonly ConditionalWeakTable<object, TabController> _weakTabControllersTable = new ConditionalWeakTable<object, TabController>();

    readonly HashSet<MenuButton> _injectedButtons = new HashSet<MenuButton>();
    readonly HashSet<Button> _injectedSupportButtons = new HashSet<Button>();

    private ReferencesDock _referencesDock;
    private Control _referencesDockButton;
    private ApiDocumentationDock _documentationDock;
    private Control _documentationDockButton;
    private TodoTagsDock _todoTagsDock;
    private Control _todoTagsDockButton;
    private TodoTagsScanner _todoTagsScanner;
    private ProblemsDock _problemsDock;
    private Control _problemsDockButton;
    private AstViewerDock _astViewerDock;
    private Control _astViewerDockButton;

    // Diagnostics system
    private ConfigManager _configManager;
    private CacheManager _cacheManager;
    private DiagnosticService _diagnosticService;
    private BackgroundAnalyzer _backgroundAnalyzer;
    private NotificationPanel _notificationPanel;
    private QuickFixHandler _quickFixHandler;

    // Scene file watching (using events from GDSceneTypesProvider in Semantics)

    // Refactoring system
    private RefactoringActionProvider _refactoringActionProvider;

    // Formatting service
    private Formatting.FormattingService _formattingService;

    // Type inference for completion
    private GodotTypesProvider _godotTypesProvider;
    private TypeResolver _typeResolver;

    // UI dialogs
    private AboutPanel _aboutPanel;

    // Project Settings integration
    private ProjectSettingsRegistry _settingsRegistry;

    internal GDProjectMap ProjectMap => _projectMap;
    internal DiagnosticService DiagnosticService => _diagnosticService;
    internal ConfigManager ConfigManager => _configManager;
    internal RefactoringActionProvider RefactoringActionProvider => _refactoringActionProvider;
    internal Formatting.FormattingService FormattingService => _formattingService;
    internal QuickFixHandler QuickFixHandler => _quickFixHandler;
    internal GodotTypesProvider GodotTypesProvider => _godotTypesProvider;
    internal TypeResolver TypeResolver => _typeResolver;

    public override void _Ready()
    {
        try
        {
            _projectMap = new GDProjectMap();

            // Initialize configuration
            _configManager = new ConfigManager(_projectMap.ProjectPath);

            // Initialize Project Settings integration
            _settingsRegistry = new ProjectSettingsRegistry(_configManager);
            _settingsRegistry.RegisterAllSettings();
            _settingsRegistry.SyncToProjectSettings();
            ProjectSettings.Save();

            // Initialize caching
            if (_configManager.Config.Cache.Enabled)
            {
                _cacheManager = new CacheManager(_projectMap.ProjectPath);
            }

            // Initialize diagnostic service
            _diagnosticService = new DiagnosticService(_projectMap, _configManager, _cacheManager);
            _diagnosticService.OnDiagnosticsChanged += OnDiagnosticsChanged;
            _diagnosticService.OnProjectAnalysisCompleted += OnProjectAnalysisCompleted;

            // Initialize background analyzer
            _backgroundAnalyzer = new BackgroundAnalyzer(_diagnosticService, _projectMap);

            // Initialize quick fix handler
            _quickFixHandler = new QuickFixHandler(_diagnosticService);

            // Initialize public API
            var services = new GDShraptServicesImpl(_projectMap);
            GDShraptApi.Initialize(services);

            // Initialize scene file watching using GDSceneTypesProvider from Semantics
            _projectMap.SceneTypesProvider.NodeRenameDetected += OnNodeRenameDetected;
            _projectMap.SceneTypesProvider.EnableFileWatcher();

            // Initialize refactoring system
            _refactoringActionProvider = new RefactoringActionProvider();

            // Initialize formatting service
            _formattingService = new Formatting.FormattingService(_configManager);

            // Initialize type resolver for completion
            _godotTypesProvider = new GodotTypesProvider();
            var projectTypesProvider = new ProjectTypesProvider(_projectMap);
            _typeResolver = new TypeResolver(_godotTypesProvider, projectTypesProvider, _projectMap.SceneTypesProvider);

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
        // Shutdown public API first
        GDShraptApi.Shutdown();

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
        if (_projectMap?.SceneTypesProvider != null)
        {
            _projectMap.SceneTypesProvider.NodeRenameDetected -= OnNodeRenameDetected;
            _projectMap.SceneTypesProvider.DisableFileWatcher();
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

        _projectMap?.Dispose();
        _projectMap = null;

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

    private void OnDiagnosticsChanged(DiagnosticsChangedEventArgs args)
    {
        Logger.Debug($"Diagnostics changed for {args.Script.FullPath}: {args.Diagnostics.Count} issues");

        // Update the overlay for the affected script
        foreach (var kvp in _weakTabControllersTable)
        {
            var controller = _weakTabControllersTable.GetValue(kvp, _ => null);
            if (controller?.ScriptReference?.FullPath == args.Script.FullPath)
            {
                controller.UpdateDiagnostics(args.Script);
            }
        }

        // Update notification panel
        UpdateNotificationPanel(args.Script.FullPath, args.Summary);
    }

    private void OnProjectAnalysisCompleted(ProjectAnalysisCompletedEventArgs args)
    {
        Logger.Info($"Project analysis completed: {args.Summary} ({args.FilesAnalyzed} files in {args.Duration.TotalMilliseconds:F0}ms)");

        // Show notification if there are issues
        if (args.Summary.HasIssues && _configManager?.Config.Notifications.ShowProjectSummaryOnStartup == true)
        {
            UpdateNotificationPanel(null, args.Summary);
        }
    }

    private void UpdateNotificationPanel(string? scriptPath, DiagnosticSummary summary)
    {
        if (_notificationPanel == null || !_configManager.Config.Notifications.Enabled)
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
            _referencesDock.NavigateToReference -= OnNavigateToReference;
            RemoveControlFromBottomPanel(_referencesDock);
            _referencesDock.QueueFree();
            _referencesDock = null;
        }

        if (_documentationDock != null)
        {
            RemoveControlFromBottomPanel(_documentationDock);
            _documentationDock.QueueFree();
            _documentationDock = null;
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
            _astViewerDock.NavigateToCode -= OnNavigateToReference;
            RemoveControlFromBottomPanel(_astViewerDock);
            _astViewerDock.QueueFree();
            _astViewerDock = null;
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
        Logger.Debug($"Navigate to reference: {filePath}:{line}:{column}");

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
            EditorInterface.Singleton.EditScript(script, line + 1, column);
        }
    }

    internal GDScriptMap GetScriptMap(string path)
    {
        return _projectMap.GetScriptMapByResourcePath(path);
    }

    public void StopAllActivities()
    {
        // TODO
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
        _referencesDock.NavigateToReference += OnNavigateToReference;
        _referencesDockButton = AddControlToBottomPanel(_referencesDock, "Find References");

        // Create the API documentation dock
        _documentationDock = new ApiDocumentationDock();
        _documentationDockButton = AddControlToBottomPanel(_documentationDock, "API Documentation");

        // Create the TODO tags dock
        _todoTagsScanner = new TodoTagsScanner(_projectMap, _configManager);
        _todoTagsDock = new TodoTagsDock();
        _todoTagsDock.Initialize(_todoTagsScanner, _configManager);
        _todoTagsDock.NavigateToItem += OnNavigateToTodoItem;
        _todoTagsDock.OpenSettingsRequested += OnTodoSettingsRequested;
        _todoTagsDockButton = AddControlToBottomPanel(_todoTagsDock, "TODO Tags");

        // Create the Problems dock
        _problemsDock = new ProblemsDock();
        _problemsDock.Initialize(_diagnosticService, _projectMap);
        _problemsDock.NavigateToItem += OnNavigateToProblem;
        _problemsDockButton = AddControlToBottomPanel(_problemsDock, "Problems");

        // Create the AST Viewer dock (if enabled)
        _astViewerDock = new AstViewerDock();
        _astViewerDock.Initialize(this, _projectMap);
        _astViewerDock.NavigateToCode += OnNavigateToReference;
        _astViewerDockButton = AddControlToBottomPanel(_astViewerDock, "AST Viewer");

        // Start TODO tags scan if enabled
        if (_configManager.Config.TodoTags.Enabled && _configManager.Config.TodoTags.ScanOnStartup)
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
        if (!_configManager.Config.Notifications.Enabled)
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

        var map = _projectMap.GetScriptMapByResourcePath(script.ResourcePath);

        if (map != null)
        {
            map.TabController = controller;

            // Queue priority analysis for current file
            _backgroundAnalyzer?.QueueScriptAnalysis(map.Reference, priority: true);
        }

        controller.SetControlledScript(script);
    }

    internal TabController OpenScript(GDScriptMap map)
    {
        Logger.Debug($"OpenTabForScript '{map.Reference.FullPath}'");
        AttachTabControllersIfNeeded();

        var controller = map.TabController;

        if (controller == null || !controller.IsInTree)
        {
            Logger.Debug($"Controller for this Script map is null or not in the tree.");
            controller = map.TabController = OpenNewScriptTab(map);
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

    internal TabController OpenNewScriptTab(GDScriptMap map)
    {
        Logger.Debug($"OpenNewScript '{map.Reference.FullPath}'");
        var res = ResourceLoader.Load(map.Reference.FullPath);
        EditorInterface.Singleton.EditResource(res);
        AttachTabControllersIfNeeded();
        UpdateCurrentTabScript();
        return map.TabController;
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

        foreach (var script in _projectMap.Scripts)
        {
            if (script.Class == null)
                continue;

            var filePath = script.Reference?.FullPath;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDShrapt.Reader.GDIdentifier id && id.Sequence == symbolName)
                {
                    var kind = DetermineReferenceKind(id);
                    var context = GetContext(id);

                    references.Add(new ReferenceItem(
                        filePath,
                        id.StartLine,
                        id.StartColumn,
                        context,
                        kind
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

    private static UI.ReferenceKind DetermineReferenceKind(GDShrapt.Reader.GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDShrapt.Reader.GDMethodDeclaration)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDVariableDeclaration)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDVariableDeclarationStatement)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDSignalDeclaration)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDParameterDeclaration)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDEnumDeclaration)
            return UI.ReferenceKind.Declaration;
        if (parent is GDShrapt.Reader.GDInnerClassDeclaration)
            return UI.ReferenceKind.Declaration;

        // Check if it's a call
        if (parent is GDShrapt.Reader.GDIdentifierExpression idExpr)
        {
            if (idExpr.Parent is GDShrapt.Reader.GDCallExpression)
                return UI.ReferenceKind.Call;
        }

        if (parent is GDShrapt.Reader.GDMemberOperatorExpression memberOp)
        {
            if (memberOp.Parent is GDShrapt.Reader.GDCallExpression)
                return UI.ReferenceKind.Call;
        }

        return UI.ReferenceKind.Read;
    }

    private static string GetContext(GDShrapt.Reader.GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        if (parent is GDShrapt.Reader.GDMethodDeclaration method)
            return $"func {method.Identifier?.Sequence ?? ""}(...)";

        if (parent is GDShrapt.Reader.GDVariableDeclaration variable)
            return $"var {variable.Identifier?.Sequence ?? ""}";

        if (parent is GDShrapt.Reader.GDVariableDeclarationStatement localVar)
            return $"var {localVar.Identifier?.Sequence ?? ""}";

        if (parent is GDShrapt.Reader.GDSignalDeclaration signal)
            return $"signal {signal.Identifier?.Sequence ?? ""}";

        if (parent is GDShrapt.Reader.GDParameterDeclaration param)
            return $"param {param.Identifier?.Sequence ?? ""}";

        // For expressions, try to get statement context
        var current = parent;
        while (current != null && !(current is GDShrapt.Reader.GDStatement) && !(current is GDShrapt.Reader.GDClassMember))
        {
            current = current.Parent;
        }

        if (current != null)
        {
            var text = current.ToString();
            if (text.Length > 60)
                text = text.Substring(0, 57) + "...";
            return text.Trim().Replace("\n", " ").Replace("\r", "");
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

        foreach (var item in _injectedSupportButtons)
        {
            // Also remove the spacer that was added before the button
            var parent = item.GetParent();
            if (parent != null)
            {
                var spacer = parent.GetNodeOrNull<Control>("GDShraptSpacer");
                if (spacer != null)
                {
                    parent.RemoveChild(spacer);
                    spacer.QueueFree();
                }
            }
            item.GetParent()?.RemoveChild(item);
        }

        _injectedSupportButtons.Clear();
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
            var oldButton = node
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
            }

            // Add Support button if not already present - positioned to the right
            var existingSupportButton = node
               .GetChildren()
               .OfType<SupportButton>()
               .FirstOrDefault();

            var existingSpacer = node.GetNodeOrNull("GDShraptSpacer");

            if (existingSupportButton == null)
            {
                // Add a spacer to push the button to the right
                var spacer = new Control
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Name = "GDShraptSpacer"
                };
                node.AddChild(spacer);

                var supportButton = new SupportButton();
                _injectedSupportButtons.Add(supportButton);
                node.AddChild(supportButton);
            }
            else
            {
                // Move spacer and button to the end to keep them on the right
                if (existingSpacer != null)
                {
                    node.MoveChild(existingSpacer, node.GetChildCount() - 1);
                }
                node.MoveChild(existingSupportButton, node.GetChildCount() - 1);
            }
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

    private async System.Threading.Tasks.Task ProcessNodeRename(GDShrapt.Semantics.GDDetectedNodeRename rename)
    {
        // Find all GDScript references to the old name
        var referenceFinder = new NodePathReferenceFinder(_projectMap, _projectMap.SceneTypesProvider);
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
        var renamer = new NodePathRenamer();

        // Use the NEW name from the scene
        result.NewName = rename.NewName;
        renamer.ApplyChanges(result, rename.OldName);

        // Mark our own write to avoid triggering another rename detection
        _projectMap.SceneTypesProvider.MarkOwnWrite();

        // Save modified scripts
        var modifiedScripts = renamer.GetModifiedScripts(result.SelectedReferences);
        foreach (var script in modifiedScripts)
        {
            SaveModifiedScript(script);
        }

        Logger.Debug($"Applied rename: '{rename.OldName}' -> '{rename.NewName}' to {modifiedScripts.Count} scripts");
    }

    private void SaveModifiedScript(GDScriptMap scriptMap)
    {
        if (scriptMap?.Class == null || scriptMap.Reference == null)
            return;

        try
        {
            var content = scriptMap.Class.ToString();
            System.IO.File.WriteAllText(scriptMap.Reference.FullPath, content);
            Logger.Debug($"Saved modified script: {scriptMap.Reference.FullPath}");

            // Reload the script in the editor if it's open
            scriptMap.Reload();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save script {scriptMap.Reference.FullPath}: {ex.Message}");
        }
    }

    #endregion
}
