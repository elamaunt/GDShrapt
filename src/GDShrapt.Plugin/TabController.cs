using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

internal partial class TabController : GodotObject
{
    readonly Control _tab; // ScriptTextEditor
    readonly GDShraptPlugin _plugin;
    readonly System.Collections.Generic.Dictionary<int, Action> _popupActions = new System.Collections.Generic.Dictionary<int, Action>();

    PopupMenu? _codePopupMenu;
    TextEdit? _textEdit;
    Script? _script;
    ReferenceCountOverlay? _referenceOverlay;
    ErrorLensOverlay? _errorLensOverlay;
    QuickActionsPopup? _quickActionsPopup;
    QuickFixesPopup? _quickFixesPopup;
    RefactoringContextBuilder? _contextBuilder;
    CompletionPopup? _completionPopup;
    CompletionService? _completionService;
    CompletionContextBuilder? _completionContextBuilder;

    public Script? ControlledScript => _script;
    public Control Tab => _tab;

    IScriptEditor? _editor;
    public IScriptEditor? Editor => _editor ?? CreateEditorIfNeeded();

    /// <summary>
    /// Event fired when user clicks on a reference count in the overlay.
    /// </summary>
    public event Action<string, int>? ReferenceCountClicked;

    public GDPluginScriptReference? GDPluginScriptReference
    {
        get
        {
            if (_script == null)
                return null;
            return _plugin.ProjectMap.GetScriptMapByResourcePath(_script.ResourcePath)?.Reference;
        }
    }

    public TabController(GDShraptPlugin plugin, Control tab)
    {
        _tab = tab;
        _plugin = plugin;

        if (_tab.HasSignal("edited_script_changed"))
        {
            _tab.Connect("edited_script_changed", Callable.From(OnEditedScriptChanged));
        }
        _tab.VisibilityChanged += OnVisibilityChanged;

        OnVisibilityChanged();
    }

    public bool IsInTree => _tab.IsInsideTree();
    public bool IsVisibleInTree => _tab.IsVisibleInTree();

    private void OnVisibilityChanged()
    {
        CreateEditorIfNeeded();

        // Try to find popup menu, and retry with delay if not found
        if (!FindCodePopupMenu())
        {
            // PopupMenu might be created later, try again after a short delay
            CallDeferred(nameof(TryFindPopupMenuDeferred));
        }
    }

    private void TryFindPopupMenuDeferred()
    {
        if (!FindCodePopupMenu())
        {
            Logger.Debug("TabController: PopupMenu still not found after deferred call");
        }
    }

    private IScriptEditor? CreateEditorIfNeeded()
    {
        if (_editor != null)
            return _editor;

        if (_tab.GetChildCount() < 1)
        {
            Logger.Debug("CreateEditorIfNeeded: Tab has no children");
            return null;
        }

        // Try to find TextEdit anywhere in the tab hierarchy
        var textEdit = FindTextEdit(_tab);
        if (textEdit == null)
        {
            Logger.Debug($"CreateEditorIfNeeded: TextEdit not found in tab hierarchy. Tab children: {_tab.GetChildCount()}");
            LogNodeHierarchy(_tab, 0);
            return null;
        }

        _textEdit = textEdit;
        Logger.Info($"CreateEditorIfNeeded: TextEdit found: {textEdit.Name}");

        _editor = new GDScriptEditor(this, _textEdit, _plugin, _tab);

        // Connect to text edit events for completion
        ConnectTextEditEvents();

        // Find the CodeTextEditor parent for overlay
        var codeTextEditor = FindCodeTextEditorParent(textEdit);
        if (codeTextEditor != null)
        {
            CreateReferenceOverlay(codeTextEditor);
        }

        return _editor;
    }

    /// <summary>
    /// Finds the CodeTextEditor parent of a TextEdit.
    /// </summary>
    private static Node? FindCodeTextEditorParent(Node node)
    {
        var parent = node.GetParent();
        while (parent != null)
        {
            if (parent.GetClass() == "CodeTextEditor" || parent.Name.ToString().Contains("CodeTextEditor"))
                return parent;
            parent = parent.GetParent();
        }
        return node.GetParent(); // Fallback to direct parent
    }

    /// <summary>
    /// Logs node hierarchy for debugging.
    /// </summary>
    private static void LogNodeHierarchy(Node node, int depth)
    {
        if (depth > 5) return; // Limit depth

        var indent = new string(' ', depth * 2);
        Logger.Debug($"{indent}- {node.Name} ({node.GetClass()})");

        foreach (var child in node.GetChildren())
        {
            LogNodeHierarchy(child, depth + 1);
        }
    }

    /// <summary>
    /// Recursively searches for TextEdit in the node hierarchy.
    /// </summary>
    private static TextEdit? FindTextEdit(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is TextEdit textEdit)
                return textEdit;

            var found = FindTextEdit(child);
            if (found != null)
                return found;
        }
        return null;
    }

    private void CreateReferenceOverlay(Node container)
    {
        if (_textEdit == null || _referenceOverlay != null)
            return;

        Logger.Debug("Creating reference count overlay");

        _referenceOverlay = new ReferenceCountOverlay();
        container.AddChild(_referenceOverlay);

        // Position overlay over the TextEdit
        _referenceOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        // Attach to editor
        _referenceOverlay.AttachToEditor(_textEdit, _plugin.ProjectMap);

        // Wire up click event
        _referenceOverlay.ReferenceCountClicked += OnReferenceCountClicked;

        // Set initial script if available
        if (_script != null)
        {
            var scriptMap = _plugin.ProjectMap.GetScriptMapByResourcePath(_script.ResourcePath);
            _referenceOverlay.SetScript(scriptMap);
        }

        // Create Error Lens overlay
        CreateErrorLensOverlay(container);
    }

    private void CreateErrorLensOverlay(Node container)
    {
        if (_textEdit == null || _errorLensOverlay != null)
            return;

        Logger.Info("Creating error lens overlay");

        _errorLensOverlay = new ErrorLensOverlay();
        container.AddChild(_errorLensOverlay);

        // Position overlay over the TextEdit
        _errorLensOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        // Attach to editor
        _errorLensOverlay.AttachToEditor(_textEdit);

        // Set initial script if available
        if (_script != null)
        {
            var scriptMap = _plugin.ProjectMap.GetScriptMapByResourcePath(_script.ResourcePath);
            _errorLensOverlay.SetScript(scriptMap);
        }
    }

    private void ConnectTextEditEvents()
    {
        if (_textEdit == null)
            return;

        // Connect to text changed for completion filtering
        _textEdit.TextChanged += OnTextEditTextChanged;

        // Connect to GUI input for trigger detection
        _textEdit.GuiInput += OnTextEditGuiInput;
    }

    private void DisconnectTextEditEvents()
    {
        if (_textEdit == null)
            return;

        _textEdit.TextChanged -= OnTextEditTextChanged;
        _textEdit.GuiInput -= OnTextEditGuiInput;
    }

    private void OnTextEditTextChanged()
    {
        // Update completion filter if popup is visible
        if (_completionPopup != null && _completionPopup.Visible && _textEdit != null)
        {
            var cursorCol = _textEdit.GetCaretColumn();
            var cursorLine = _textEdit.GetCaretLine();
            var lineText = _textEdit.GetLine(cursorLine);

            // Extract current word prefix
            var wordStart = cursorCol;
            while (wordStart > 0 && IsIdentifierChar(lineText[wordStart - 1]))
            {
                wordStart--;
            }

            var prefix = lineText.Substring(wordStart, cursorCol - wordStart);
            _completionPopup.UpdateFilter(prefix);
        }
    }

    private void OnTextEditGuiInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Handle completion popup keyboard input first
            if (_completionPopup != null && _completionPopup.Visible)
            {
                if (_completionPopup.HandleKeyInput(keyEvent))
                {
                    _textEdit?.GetViewport()?.SetInputAsHandled();
                    return;
                }
            }

            // Trigger completion on '.'
            if (keyEvent.Keycode == Key.Period && !keyEvent.CtrlPressed && !keyEvent.AltPressed)
            {
                // Delay to allow the '.' to be inserted first
                CallDeferred(nameof(TriggerMemberCompletion));
            }
            // Trigger completion on Ctrl+Space
            else if (keyEvent.Keycode == Key.Space && keyEvent.CtrlPressed && !keyEvent.AltPressed)
            {
                TriggerCompletion();
                _textEdit?.GetViewport()?.SetInputAsHandled();
            }
            // Close completion on Escape
            else if (keyEvent.Keycode == Key.Escape && _completionPopup?.Visible == true)
            {
                _completionPopup.Hide();
                _textEdit?.GetViewport()?.SetInputAsHandled();
            }
        }
    }

    private void TriggerMemberCompletion()
    {
        TriggerCompletion(CompletionTriggerKind.TriggerCharacter, '.');
    }

    /// <summary>
    /// Triggers code completion at the current cursor position.
    /// </summary>
    internal void TriggerCompletion(CompletionTriggerKind triggerKind = CompletionTriggerKind.Invoked, char? triggerChar = null)
    {
        if (_textEdit == null || _script == null)
            return;

        var scriptMap = _plugin.ProjectMap.GetScriptMapByResourcePath(_script.ResourcePath);
        if (scriptMap == null)
            return;

        // Initialize completion service if needed
        EnsureCompletionServiceInitialized();
        if (_completionService == null || _completionContextBuilder == null)
            return;

        // Build completion context
        var sourceCode = _textEdit.Text;
        var cursorLine = _textEdit.GetCaretLine();
        var cursorCol = _textEdit.GetCaretColumn();

        var context = _completionContextBuilder.Build(
            scriptMap,
            sourceCode,
            cursorLine,
            cursorCol,
            triggerKind,
            triggerChar);

        // Don't show completions in comments or strings
        if (context.ShouldSuppress)
        {
            Logger.Debug("TabController: Completion suppressed (in string/comment)");
            return;
        }

        // Get completions
        var completions = _completionService.GetCompletions(context);
        if (completions.Count == 0)
        {
            Logger.Debug("TabController: No completions available");
            _completionPopup?.Hide();
            return;
        }

        Logger.Debug($"TabController: Got {completions.Count} completions");

        // Create popup if needed
        if (_completionPopup == null)
        {
            _completionPopup = new CompletionPopup();
            _completionPopup.SetTextEdit(_textEdit);
            _completionPopup.ItemSelected += OnCompletionItemSelected;
            _completionPopup.Cancelled += OnCompletionCancelled;
            _tab.AddChild(_completionPopup);
        }

        // Show completions
        var cursorPos = GetCursorScreenPosition();
        _completionPopup.ShowCompletions(
            completions,
            cursorPos,
            context.WordStartColumn,
            cursorLine,
            context.WordPrefix);
    }

    private void EnsureCompletionServiceInitialized()
    {
        if (_completionService != null && _completionContextBuilder != null)
            return;

        var typeResolver = _plugin.TypeResolver;
        var godotTypesProvider = _plugin.GodotTypesProvider;

        if (typeResolver == null || godotTypesProvider == null)
        {
            Logger.Debug("TabController: Cannot initialize completion - missing dependencies");
            return;
        }

        _completionService = new CompletionService(_plugin.ProjectMap, typeResolver, godotTypesProvider);
        _completionContextBuilder = new CompletionContextBuilder(_plugin.ProjectMap, typeResolver);

        Logger.Info("TabController: Completion service initialized");
    }

    private void OnCompletionItemSelected(CompletionItem item)
    {
        Logger.Debug($"TabController: Completion item selected: {item.Label}");
    }

    private void OnCompletionCancelled()
    {
        Logger.Debug("TabController: Completion cancelled");
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private void OnReferenceCountClicked(string symbolName, int line)
    {
        Logger.Info($"Reference count clicked: '{symbolName}' at line {line}");
        ReferenceCountClicked?.Invoke(symbolName, line);
    }

    internal void RequestGodotLookup()
    {
        Logger.Info("Godot 'lookup symbol' requested");
        _codePopupMenu?.EmitSignal("id_pressed", 43);
    }

    private bool FindCodePopupMenu()
    {
        // Check if cached popup is still valid
        if (_codePopupMenu != null && GodotObject.IsInstanceValid(_codePopupMenu))
            return true;

        _codePopupMenu = null;

        if (_tab.GetChildCount() < 1)
            return false;

        // PopupMenu in ScriptTextEditor is a direct child of the tab
        foreach (var child in _tab.GetChildren())
        {
            if (child is PopupMenu popup)
            {
                _codePopupMenu = popup;
                break;
            }
        }

        if (_codePopupMenu == null)
        {
            Logger.Debug("TabController: PopupMenu not found yet");
            return false;
        }

        Logger.Debug($"TabController: PopupMenu found: {_codePopupMenu.Name}");

        // Connect signals if not already connected
        var aboutToPopupCallable = Callable.From(OnAboutToShow);
        var idPressedCallable = Callable.From<long>(OnItemPressed);

        if (!_codePopupMenu.IsConnected("about_to_popup", aboutToPopupCallable))
        {
            _codePopupMenu.AboutToPopup += OnAboutToShow;
            Logger.Debug("TabController: Connected AboutToPopup signal");
        }

        if (!_codePopupMenu.IsConnected("id_pressed", idPressedCallable))
        {
            _codePopupMenu.IdPressed += OnItemPressed;
            Logger.Debug("TabController: Connected IdPressed signal");
        }

        return true;
    }

    internal void SetControlledScript(Script script)
    {
        _script = script;
        Logger.Debug($"Controlled script updated: '{script.ResourcePath}'");

        var scriptMap = _plugin.ProjectMap.GetScriptMapByResourcePath(script.ResourcePath);

        // Update overlays with new script
        _referenceOverlay?.SetScript(scriptMap);
        _errorLensOverlay?.SetScript(scriptMap);

        // Update diagnostics from DiagnosticService if available
        if (scriptMap != null)
        {
            UpdateDiagnostics(scriptMap.Reference);
        }
    }

    /// <summary>
    /// Updates the error lens overlay with diagnostics from DiagnosticService.
    /// </summary>
    internal void UpdateDiagnostics(GDPluginScriptReference script)
    {
        if (_errorLensOverlay == null)
            return;

        var diagnosticService = _plugin.DiagnosticService;
        if (diagnosticService == null)
            return;

        var diagnostics = diagnosticService.GetDiagnostics(script);
        _errorLensOverlay.SetDiagnostics(diagnostics);
    }

    /// <summary>
    /// Updates diagnostics for the current script.
    /// </summary>
    internal void RefreshDiagnostics()
    {
        if (_script == null)
            return;

        var scriptMap = _plugin.ProjectMap.GetScriptMapByResourcePath(_script.ResourcePath);
        if (scriptMap != null)
        {
            UpdateDiagnostics(scriptMap.Reference);
        }
    }

    private void OnEditedScriptChanged()
    {
        Logger.Debug("OnEditedScriptChanged called");
        if (_editor == null)
            return;

        _editor.ReloadScriptFromText();
    }

    private void OnAboutToShow()
    {
        Logger.Info("OnAboutToShow called");

        // Try to create editor if not exists
        CreateEditorIfNeeded();

        if (_editor == null)
        {
            Logger.Info("OnAboutToShow: _editor is null, skipping menu items");
            return;
        }

        if (_codePopupMenu == null)
        {
            Logger.Info("OnAboutToShow: _codePopupMenu is null, skipping menu items");
            return;
        }

        Logger.Info("OnAboutToShow: Adding GDShrapt menu items");
        _codePopupMenu.AddSeparator("GDShrapt");

        AddPopupMenuButton("Find references", "Find_references", () => _plugin.ExecuteCommand(Commands.FindReferences, _editor), 10001, Key.F12, true);
        AddPopupMenuButton("Go to definition", "Go_to_definition", () => _plugin.ExecuteCommand(Commands.GoToDefinition, _editor), 10002, Key.F12);
        AddPopupMenuButton("Rename", "Rename", () => _plugin.ExecuteCommand(Commands.Rename, _editor), 10003, Key.R, true);
        AddPopupMenuButton("Extract method", "Extract_method", () => _plugin.ExecuteCommand(Commands.ExtractMethod, _editor), 10004, Key.E, true);
        AddPopupMenuButton("Quick Actions...", "Quick_actions", ShowQuickActions, 10005, Key.Period, true);

        // Add dynamic refactoring actions
        AddDynamicRefactoringMenuItems();
    }

    private void AddDynamicRefactoringMenuItems()
    {
        if (_codePopupMenu == null || _editor == null)
            return;

        var provider = _plugin.RefactoringActionProvider;
        if (provider == null)
            return;

        var context = BuildRefactoringContext();
        if (context == null)
            return;

        var availableActions = provider.GetAvailableActions(context).ToList();
        if (!availableActions.Any())
            return;

        _codePopupMenu.AddSeparator("Refactoring");

        var baseIndex = 20000;
        foreach (var action in availableActions.Take(5)) // Limit to top 5 actions
        {
            var index = baseIndex++;
            var actionCopy = action; // Capture for closure
            _codePopupMenu.AddItem(action.DisplayName, index);
            _popupActions[index] = () => ExecuteRefactoringAction(actionCopy, context);
        }
    }

    private async void ExecuteRefactoringAction(IRefactoringAction action, RefactoringContext context)
    {
        Logger.Info($"TabController: Executing refactoring action '{action.Id}'");
        try
        {
            await action.ExecuteAsync(context);
            Logger.Info($"TabController: Refactoring action '{action.Id}' completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"TabController: Error executing refactoring action '{action.Id}': {ex.Message}");
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Shows the quick actions popup at the cursor position (Ctrl+.).
    /// First tries to show quick fixes, then falls back to refactoring actions.
    /// </summary>
    internal void ShowQuickActions()
    {
        Logger.Info("TabController: ShowQuickActions requested");

        // Try to create editor if not exists
        CreateEditorIfNeeded();

        if (_editor == null || _textEdit == null)
        {
            Logger.Info($"TabController: Cannot show quick actions - _editor={_editor != null}, _textEdit={_textEdit != null}");
            return;
        }

        // First try to show quick fixes if any are available
        if (TryShowQuickFixes())
        {
            return;
        }

        // Fall back to refactoring actions
        var context = BuildRefactoringContext();

        // Create popup if needed
        if (_quickActionsPopup == null)
        {
            _quickActionsPopup = new QuickActionsPopup();
            _quickActionsPopup.SetProvider(_plugin.RefactoringActionProvider);
            _tab.AddChild(_quickActionsPopup);
        }

        // Get cursor position on screen
        var cursorPos = GetCursorScreenPosition();

        // Show actions (popup handles null context gracefully)
        if (context != null)
        {
            _quickActionsPopup.ShowActions(context, cursorPos);
        }
        else
        {
            Logger.Info("TabController: No context available, showing empty popup");
            _quickActionsPopup.ShowNoActionsMessage(cursorPos);
        }
    }

    /// <summary>
    /// Tries to show quick fixes popup. Returns true if fixes were found and popup shown.
    /// </summary>
    private bool TryShowQuickFixes()
    {
        if (_textEdit == null || _script == null)
            return false;

        var quickFixHandler = _plugin.QuickFixHandler;
        if (quickFixHandler == null)
            return false;

        var scriptRef = GDPluginScriptReference;
        if (scriptRef == null)
            return false;

        var cursorLine = _textEdit.GetCaretLine();
        var cursorCol = _textEdit.GetCaretColumn();

        // Check if there are fixes at cursor position or on the line
        var fixes = quickFixHandler.GetFixesAtPosition(scriptRef, cursorLine, cursorCol);
        if (fixes.Count == 0)
        {
            fixes = quickFixHandler.GetFixesOnLine(scriptRef, cursorLine);
        }

        if (fixes.Count == 0)
            return false;

        // Create popup if needed
        if (_quickFixesPopup == null)
        {
            _quickFixesPopup = new QuickFixesPopup();
            _quickFixesPopup.SetHandler(quickFixHandler);
            _quickFixesPopup.SetTextEdit(_textEdit);
            _quickFixesPopup.FixApplied += OnQuickFixApplied;
            _tab.AddChild(_quickFixesPopup);
        }

        // Show fixes
        var cursorPos = GetCursorScreenPosition();
        var sourceCode = _textEdit.Text;
        _quickFixesPopup.ShowFixes(scriptRef, cursorLine, cursorCol, sourceCode, cursorPos);

        Logger.Info($"TabController: Showing {fixes.Count} quick fixes");
        return true;
    }

    /// <summary>
    /// Shows quick fixes popup explicitly (can be bound to a separate hotkey if needed).
    /// </summary>
    internal void ShowQuickFixes()
    {
        Logger.Info("TabController: ShowQuickFixes requested");

        if (_textEdit == null || _script == null)
        {
            Logger.Info("TabController: Cannot show quick fixes - no editor");
            return;
        }

        var quickFixHandler = _plugin.QuickFixHandler;
        if (quickFixHandler == null)
        {
            Logger.Info("TabController: Cannot show quick fixes - no handler");
            return;
        }

        var scriptRef = GDPluginScriptReference;
        if (scriptRef == null)
        {
            Logger.Info("TabController: Cannot show quick fixes - no script reference");
            return;
        }

        // Create popup if needed
        if (_quickFixesPopup == null)
        {
            _quickFixesPopup = new QuickFixesPopup();
            _quickFixesPopup.SetHandler(quickFixHandler);
            _quickFixesPopup.SetTextEdit(_textEdit);
            _quickFixesPopup.FixApplied += OnQuickFixApplied;
            _tab.AddChild(_quickFixesPopup);
        }

        // Get cursor info
        var cursorLine = _textEdit.GetCaretLine();
        var cursorCol = _textEdit.GetCaretColumn();
        var cursorPos = GetCursorScreenPosition();
        var sourceCode = _textEdit.Text;

        _quickFixesPopup.ShowFixes(scriptRef, cursorLine, cursorCol, sourceCode, cursorPos);
    }

    private void OnQuickFixApplied(string newCode)
    {
        Logger.Info("TabController: Quick fix applied, refreshing diagnostics");

        // Trigger re-analysis of the script
        if (_script != null)
        {
            var binding = _plugin.ProjectMap.GetBindingByResourcePath(_script.ResourcePath);
            if (binding != null)
            {
                // Re-analyze in background
                _ = _plugin.DiagnosticService?.AnalyzeScriptAsync(binding);
            }
        }
    }

    private Vector2 GetCursorScreenPosition()
    {
        if (_textEdit == null)
            return Vector2.Zero;

        var cursorLine = _textEdit.GetCaretLine();
        var cursorCol = _textEdit.GetCaretColumn();

        // Get the rect of the cursor position
        var caretPos = _textEdit.GetRectAtLineColumn(cursorLine, cursorCol);
        var globalPos = _textEdit.GlobalPosition + caretPos.Position;

        // Offset slightly below the cursor
        globalPos.Y += caretPos.Size.Y + 5;

        return globalPos;
    }

    private RefactoringContext BuildRefactoringContext()
    {
        if (_editor == null)
            return null;

        _contextBuilder ??= new RefactoringContextBuilder(_plugin.ProjectMap, _plugin);
        return _contextBuilder.Build(_editor);
    }

    private void OnItemPressed(long id)
    {
        Logger.Info($"OnItemPressed {id}");
        if (_popupActions.TryGetValue((int)id, out Action? exec))
            exec();
    }

    private void AddPopupMenuButton(string name, string action, Action exec, int index, Key keyCode, bool control = false, bool alt = false)
    {
        if (_codePopupMenu == null)
            return;

        _codePopupMenu.AddItem(name, index);
        _popupActions[index] = exec;

        if (!InputMap.HasAction(action))
        {
            Logger.Debug($"Adding new action: '{action}'");
            InputMap.AddAction(action);

            var newEvent = new InputEventKey();

            if (control)
                newEvent.CtrlPressed = true;
            if (alt)
                newEvent.AltPressed = true;

            newEvent.Keycode = keyCode;
            InputMap.ActionAddEvent(action, newEvent);
        }

        var events = InputMap.ActionGetEvents(action);
        var @event = events?.Count > 0 ? events[0] : null;

        Logger.Debug($"{name} event {@event}");

        var itemIndex = _codePopupMenu.GetItemIndex(index);

        if (@event != null)
        {
            var shortcut = new Shortcut();
            var shortcutEvents = new Godot.Collections.Array<InputEvent> { @event };
            shortcut.Events = (Godot.Collections.Array)shortcutEvents;
            _codePopupMenu.SetItemShortcut(itemIndex, shortcut);
        }
    }

    internal void RequestCommand(Commands command)
    {
        CreateEditorIfNeeded();

        if (_editor == null)
        {
            Logger.Info($"Unable to create controller");
            return;
        }

        _plugin.ExecuteCommand(command, _editor);
    }

    internal void CleanUp()
    {
        _tab.VisibilityChanged -= OnVisibilityChanged;

        // Disconnect Godot signal that was connected in constructor
        if (_tab.HasSignal("edited_script_changed") && _tab.IsConnected("edited_script_changed", Callable.From(OnEditedScriptChanged)))
        {
            _tab.Disconnect("edited_script_changed", Callable.From(OnEditedScriptChanged));
        }

        // Disconnect text edit events
        DisconnectTextEditEvents();

        var popup = _codePopupMenu;

        if (popup != null)
        {
            popup.AboutToPopup -= OnAboutToShow;
            popup.IdPressed -= OnItemPressed;
        }

        // Clean up reference overlay
        if (_referenceOverlay != null)
        {
            _referenceOverlay.ReferenceCountClicked -= OnReferenceCountClicked;
            _referenceOverlay.Detach();
            _referenceOverlay.QueueFree();
            _referenceOverlay = null;
        }

        // Clean up error lens overlay
        if (_errorLensOverlay != null)
        {
            _errorLensOverlay.Detach();
            _errorLensOverlay.QueueFree();
            _errorLensOverlay = null;
        }

        // Clean up quick actions popup
        if (_quickActionsPopup != null)
        {
            _quickActionsPopup.QueueFree();
            _quickActionsPopup = null;
        }

        // Clean up quick fixes popup
        if (_quickFixesPopup != null)
        {
            _quickFixesPopup.FixApplied -= OnQuickFixApplied;
            _quickFixesPopup.QueueFree();
            _quickFixesPopup = null;
        }

        // Clean up completion popup
        if (_completionPopup != null)
        {
            _completionPopup.ItemSelected -= OnCompletionItemSelected;
            _completionPopup.Cancelled -= OnCompletionCancelled;
            _completionPopup.QueueFree();
            _completionPopup = null;
        }

        _completionService = null;
        _completionContextBuilder = null;
    }

    /// <summary>
    /// Forces a refresh of the reference count overlay.
    /// </summary>
    internal void RefreshReferenceOverlay()
    {
        _referenceOverlay?.ForceRefresh();
    }
}
