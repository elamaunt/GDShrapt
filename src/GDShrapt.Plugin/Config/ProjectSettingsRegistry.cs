using Godot;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Config;

/// <summary>
/// Registers GDShrapt settings in Godot's Project Settings window.
/// Provides bidirectional sync between JSON config and ProjectSettings.
/// </summary>
internal class ProjectSettingsRegistry
{
    private const string Prefix = "gdshrapt/";

    private readonly ConfigManager _configManager;
    private bool _isSyncing = false;
    private DateTime _lastSyncTime = DateTime.MinValue;

    // Track registered settings to avoid duplicates
    private readonly HashSet<string> _registeredSettings = new();

    public ProjectSettingsRegistry(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    /// <summary>
    /// Registers all GDShrapt settings in Project Settings.
    /// </summary>
    public void RegisterAllSettings()
    {
        Logger.Info("Registering GDShrapt settings in Project Settings...");

        RegisterGeneralSettings();
        RegisterUISettings();
        RegisterCodeStyleSettings();
        RegisterNamingConventionsSettings();
        RegisterNotificationSettings();
        RegisterTodoTagsSettings();

        Logger.Info($"Registered {_registeredSettings.Count} settings in Project Settings");
    }

    /// <summary>
    /// Syncs current JSON config values to ProjectSettings.
    /// </summary>
    public void SyncToProjectSettings()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            var config = _configManager.Config;

            // General
            SetSetting("general/cache_enabled", config.Cache.Enabled);

            // UI
            SetSetting("ui/ast_viewer_enabled", config.UI.AstViewerEnabled);
            SetSetting("ui/code_lens_enabled", config.UI.CodeLensEnabled);
            SetSetting("ui/references_counter_enabled", config.UI.ReferencesCounterEnabled);
            SetSetting("ui/problems_dock_enabled", config.UI.ProblemsDockEnabled);
            SetSetting("ui/todo_tags_dock_enabled", config.UI.TodoTagsDockEnabled);
            SetSetting("ui/api_documentation_dock_enabled", config.UI.ApiDocumentationDockEnabled);
            SetSetting("ui/find_references_dock_enabled", config.UI.FindReferencesDockEnabled);

            // Code Style (merged linting + formatting)
            SetSetting("code_style/linting_enabled", config.Linting.Enabled);
            SetSetting("code_style/formatting_level", (int)config.Linting.FormattingLevel);
            SetSetting("code_style/indentation_style", (int)config.Linting.IndentationStyle);
            SetSetting("code_style/tab_width", config.Linting.TabWidth);
            SetSetting("code_style/max_line_length", config.Linting.MaxLineLength);
            SetSetting("code_style/indent_size", config.Formatter.IndentSize);
            SetSetting("code_style/line_ending", (int)config.Formatter.LineEnding);
            SetSetting("code_style/blank_lines_between_functions", config.Formatter.BlankLinesBetweenFunctions);
            SetSetting("code_style/space_around_operators", config.Formatter.SpaceAroundOperators);
            SetSetting("code_style/space_after_comma", config.Formatter.SpaceAfterComma);
            SetSetting("code_style/space_after_colon", config.Formatter.SpaceAfterColon);
            SetSetting("code_style/remove_trailing_whitespace", config.Formatter.RemoveTrailingWhitespace);
            SetSetting("code_style/ensure_trailing_newline", config.Formatter.EnsureTrailingNewline);
            SetSetting("code_style/wrap_long_lines", config.Formatter.WrapLongLines);

            // Naming Conventions
            SetSetting("naming/class_name_case", (int)config.AdvancedLinting.ClassNameCase);
            SetSetting("naming/function_name_case", (int)config.AdvancedLinting.FunctionNameCase);
            SetSetting("naming/variable_name_case", (int)config.AdvancedLinting.VariableNameCase);
            SetSetting("naming/constant_name_case", (int)config.AdvancedLinting.ConstantNameCase);
            SetSetting("naming/require_underscore_for_private", config.AdvancedLinting.RequireUnderscoreForPrivate);
            SetSetting("naming/warn_unused_variables", config.AdvancedLinting.WarnUnusedVariables);
            SetSetting("naming/warn_unused_parameters", config.AdvancedLinting.WarnUnusedParameters);
            SetSetting("naming/max_parameters", config.AdvancedLinting.MaxParameters);
            SetSetting("naming/max_function_length", config.AdvancedLinting.MaxFunctionLength);
            SetSetting("naming/max_cyclomatic_complexity", config.AdvancedLinting.MaxCyclomaticComplexity);

            // Notifications
            SetSetting("notifications/enabled", config.Notifications.Enabled);
            SetSetting("notifications/show_expanded_on_first_open", config.Notifications.ShowExpandedOnFirstOpen);
            SetSetting("notifications/auto_hide_seconds", config.Notifications.AutoHideSeconds);
            SetSetting("notifications/min_severity", (int)config.Notifications.MinSeverity);

            // TODO Tags
            SetSetting("todo_tags/enabled", config.TodoTags.Enabled);
            SetSetting("todo_tags/scan_on_startup", config.TodoTags.ScanOnStartup);
            SetSetting("todo_tags/auto_refresh", config.TodoTags.AutoRefresh);
            SetSetting("todo_tags/case_sensitive", config.TodoTags.CaseSensitive);
            SetSetting("todo_tags/default_grouping", (int)config.TodoTags.DefaultGrouping);

            _lastSyncTime = DateTime.UtcNow;
            Logger.Debug("Synced config to ProjectSettings");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Syncs ProjectSettings values back to JSON config.
    /// </summary>
    public void SyncFromProjectSettings()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            var config = _configManager.Config;

            // General
            config.Cache.Enabled = GetBool("general/cache_enabled", config.Cache.Enabled);

            // UI
            config.UI.AstViewerEnabled = GetBool("ui/ast_viewer_enabled", config.UI.AstViewerEnabled);
            config.UI.CodeLensEnabled = GetBool("ui/code_lens_enabled", config.UI.CodeLensEnabled);
            config.UI.ReferencesCounterEnabled = GetBool("ui/references_counter_enabled", config.UI.ReferencesCounterEnabled);
            config.UI.ProblemsDockEnabled = GetBool("ui/problems_dock_enabled", config.UI.ProblemsDockEnabled);
            config.UI.TodoTagsDockEnabled = GetBool("ui/todo_tags_dock_enabled", config.UI.TodoTagsDockEnabled);
            config.UI.ApiDocumentationDockEnabled = GetBool("ui/api_documentation_dock_enabled", config.UI.ApiDocumentationDockEnabled);
            config.UI.FindReferencesDockEnabled = GetBool("ui/find_references_dock_enabled", config.UI.FindReferencesDockEnabled);

            // Code Style (merged linting + formatting)
            config.Linting.Enabled = GetBool("code_style/linting_enabled", config.Linting.Enabled);
            config.Linting.FormattingLevel = (GDFormattingLevel)GetInt("code_style/formatting_level", (int)config.Linting.FormattingLevel);
            config.Linting.IndentationStyle = (GDIndentationStyle)GetInt("code_style/indentation_style", (int)config.Linting.IndentationStyle);
            config.Linting.TabWidth = GetInt("code_style/tab_width", config.Linting.TabWidth);
            config.Linting.MaxLineLength = GetInt("code_style/max_line_length", config.Linting.MaxLineLength);
            config.Formatter.IndentSize = GetInt("code_style/indent_size", config.Formatter.IndentSize);
            config.Formatter.LineEnding = (GDLineEndingStyle)GetInt("code_style/line_ending", (int)config.Formatter.LineEnding);
            config.Formatter.BlankLinesBetweenFunctions = GetInt("code_style/blank_lines_between_functions", config.Formatter.BlankLinesBetweenFunctions);
            config.Formatter.SpaceAroundOperators = GetBool("code_style/space_around_operators", config.Formatter.SpaceAroundOperators);
            config.Formatter.SpaceAfterComma = GetBool("code_style/space_after_comma", config.Formatter.SpaceAfterComma);
            config.Formatter.SpaceAfterColon = GetBool("code_style/space_after_colon", config.Formatter.SpaceAfterColon);
            config.Formatter.RemoveTrailingWhitespace = GetBool("code_style/remove_trailing_whitespace", config.Formatter.RemoveTrailingWhitespace);
            config.Formatter.EnsureTrailingNewline = GetBool("code_style/ensure_trailing_newline", config.Formatter.EnsureTrailingNewline);
            config.Formatter.WrapLongLines = GetBool("code_style/wrap_long_lines", config.Formatter.WrapLongLines);

            // Naming Conventions
            config.AdvancedLinting.ClassNameCase = (GDNamingCase)GetInt("naming/class_name_case", (int)config.AdvancedLinting.ClassNameCase);
            config.AdvancedLinting.FunctionNameCase = (GDNamingCase)GetInt("naming/function_name_case", (int)config.AdvancedLinting.FunctionNameCase);
            config.AdvancedLinting.VariableNameCase = (GDNamingCase)GetInt("naming/variable_name_case", (int)config.AdvancedLinting.VariableNameCase);
            config.AdvancedLinting.ConstantNameCase = (GDNamingCase)GetInt("naming/constant_name_case", (int)config.AdvancedLinting.ConstantNameCase);
            config.AdvancedLinting.RequireUnderscoreForPrivate = GetBool("naming/require_underscore_for_private", config.AdvancedLinting.RequireUnderscoreForPrivate);
            config.AdvancedLinting.WarnUnusedVariables = GetBool("naming/warn_unused_variables", config.AdvancedLinting.WarnUnusedVariables);
            config.AdvancedLinting.WarnUnusedParameters = GetBool("naming/warn_unused_parameters", config.AdvancedLinting.WarnUnusedParameters);
            config.AdvancedLinting.MaxParameters = GetInt("naming/max_parameters", config.AdvancedLinting.MaxParameters);
            config.AdvancedLinting.MaxFunctionLength = GetInt("naming/max_function_length", config.AdvancedLinting.MaxFunctionLength);
            config.AdvancedLinting.MaxCyclomaticComplexity = GetInt("naming/max_cyclomatic_complexity", config.AdvancedLinting.MaxCyclomaticComplexity);

            // Notifications
            config.Notifications.Enabled = GetBool("notifications/enabled", config.Notifications.Enabled);
            config.Notifications.ShowExpandedOnFirstOpen = GetBool("notifications/show_expanded_on_first_open", config.Notifications.ShowExpandedOnFirstOpen);
            config.Notifications.AutoHideSeconds = GetInt("notifications/auto_hide_seconds", config.Notifications.AutoHideSeconds);
            config.Notifications.MinSeverity = (GDDiagnosticSeverity)GetInt("notifications/min_severity", (int)config.Notifications.MinSeverity);

            // TODO Tags
            config.TodoTags.Enabled = GetBool("todo_tags/enabled", config.TodoTags.Enabled);
            config.TodoTags.ScanOnStartup = GetBool("todo_tags/scan_on_startup", config.TodoTags.ScanOnStartup);
            config.TodoTags.AutoRefresh = GetBool("todo_tags/auto_refresh", config.TodoTags.AutoRefresh);
            config.TodoTags.CaseSensitive = GetBool("todo_tags/case_sensitive", config.TodoTags.CaseSensitive);
            config.TodoTags.DefaultGrouping = (TodoGroupingMode)GetInt("todo_tags/default_grouping", (int)config.TodoTags.DefaultGrouping);

            // Save to JSON
            _configManager.SaveConfig();
            _lastSyncTime = DateTime.UtcNow;
            Logger.Debug("Synced ProjectSettings to config");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Checks if ProjectSettings have changed since last sync.
    /// Call this periodically to detect user changes in Project Settings.
    /// </summary>
    public bool HasProjectSettingsChanged()
    {
        // Compare a few key settings to detect changes
        var config = _configManager.Config;

        if (GetBool("code_style/linting_enabled", config.Linting.Enabled) != config.Linting.Enabled)
            return true;

        if (GetBool("notifications/enabled", config.Notifications.Enabled) != config.Notifications.Enabled)
            return true;

        if (GetBool("todo_tags/enabled", config.TodoTags.Enabled) != config.TodoTags.Enabled)
            return true;

        if (GetBool("ui/ast_viewer_enabled", config.UI.AstViewerEnabled) != config.UI.AstViewerEnabled)
            return true;

        if (GetBool("ui/code_lens_enabled", config.UI.CodeLensEnabled) != config.UI.CodeLensEnabled)
            return true;

        return false;
    }

    /// <summary>
    /// Removes all GDShrapt settings from ProjectSettings.
    /// Call this on plugin disable.
    /// </summary>
    public void UnregisterAllSettings()
    {
        foreach (var setting in _registeredSettings)
        {
            var fullPath = Prefix + setting;
            if (ProjectSettings.HasSetting(fullPath))
            {
                ProjectSettings.SetSetting(fullPath, default);
            }
        }

        _registeredSettings.Clear();
        Logger.Info("Unregistered all GDShrapt settings from Project Settings");
    }

    #region Registration Helpers

    private void RegisterGeneralSettings()
    {
        RegisterBool("general/cache_enabled", true, isBasic: true);
    }

    private void RegisterUISettings()
    {
        RegisterBool("ui/ast_viewer_enabled", true, isBasic: true);
        RegisterBool("ui/code_lens_enabled", true, isBasic: true);
        RegisterBool("ui/references_counter_enabled", true, isBasic: true);
        RegisterBool("ui/problems_dock_enabled", true, isBasic: true);
        RegisterBool("ui/todo_tags_dock_enabled", true, isBasic: true);
        RegisterBool("ui/api_documentation_dock_enabled", true, isBasic: true);
        RegisterBool("ui/find_references_dock_enabled", true, isBasic: true);
    }

    private void RegisterCodeStyleSettings()
    {
        RegisterBool("code_style/linting_enabled", true, isBasic: true);
        RegisterEnum("code_style/formatting_level", 1, "Off,Light,Full", isBasic: true);
        RegisterEnum("code_style/indentation_style", 0, "Tabs,Spaces", isBasic: true);
        RegisterIntRange("code_style/tab_width", 4, 1, 8, isBasic: true);
        RegisterIntRange("code_style/max_line_length", 120, 0, 200);
        RegisterIntRange("code_style/indent_size", 4, 1, 8);
        RegisterEnum("code_style/line_ending", 0, "LF,CRLF,Platform");
        RegisterIntRange("code_style/blank_lines_between_functions", 2, 0, 5);
        RegisterBool("code_style/space_around_operators", true);
        RegisterBool("code_style/space_after_comma", true);
        RegisterBool("code_style/space_after_colon", true);
        RegisterBool("code_style/remove_trailing_whitespace", true);
        RegisterBool("code_style/ensure_trailing_newline", true);
        RegisterBool("code_style/wrap_long_lines", true);
    }

    private void RegisterNamingConventionsSettings()
    {
        var namingCaseHint = "snake_case,PascalCase,camelCase,SCREAMING_SNAKE_CASE,Any";

        RegisterEnum("naming/class_name_case", 1, namingCaseHint);
        RegisterEnum("naming/function_name_case", 0, namingCaseHint);
        RegisterEnum("naming/variable_name_case", 0, namingCaseHint);
        RegisterEnum("naming/constant_name_case", 3, namingCaseHint);
        RegisterBool("naming/require_underscore_for_private", true);
        RegisterBool("naming/warn_unused_variables", true);
        RegisterBool("naming/warn_unused_parameters", true);
        RegisterIntRange("naming/max_parameters", 5, 0, 20);
        RegisterIntRange("naming/max_function_length", 50, 0, 500);
        RegisterIntRange("naming/max_cyclomatic_complexity", 10, 0, 50);
    }

    private void RegisterNotificationSettings()
    {
        RegisterBool("notifications/enabled", true, isBasic: true);
        RegisterBool("notifications/show_expanded_on_first_open", true);
        RegisterIntRange("notifications/auto_hide_seconds", 0, 0, 60);
        RegisterEnum("notifications/min_severity", 2, "Hint,Info,Warning,Error");
    }

    private void RegisterTodoTagsSettings()
    {
        RegisterBool("todo_tags/enabled", true, isBasic: true);
        RegisterBool("todo_tags/scan_on_startup", true);
        RegisterBool("todo_tags/auto_refresh", true);
        RegisterBool("todo_tags/case_sensitive", false);
        RegisterEnum("todo_tags/default_grouping", 0, "ByFile,ByTag");
    }

    #endregion

    #region Low-Level Helpers

    private void SetSetting(string path, Variant value)
    {
        var fullPath = Prefix + path;
        ProjectSettings.SetSetting(fullPath, value);
    }

    private bool GetBool(string path, bool defaultValue)
    {
        var fullPath = Prefix + path;
        if (!ProjectSettings.HasSetting(fullPath))
            return defaultValue;

        var value = ProjectSettings.GetSetting(fullPath);
        return value.VariantType == Variant.Type.Bool ? value.AsBool() : defaultValue;
    }

    private int GetInt(string path, int defaultValue)
    {
        var fullPath = Prefix + path;
        if (!ProjectSettings.HasSetting(fullPath))
            return defaultValue;

        var value = ProjectSettings.GetSetting(fullPath);
        return value.VariantType == Variant.Type.Int ? value.AsInt32() : defaultValue;
    }

    private void RegisterBool(string path, bool defaultValue, bool isBasic = false)
    {
        var fullPath = Prefix + path;

        if (!ProjectSettings.HasSetting(fullPath))
        {
            ProjectSettings.SetSetting(fullPath, defaultValue);
        }

        var propertyInfo = new Godot.Collections.Dictionary
        {
            { "name", fullPath },
            { "type", (int)Variant.Type.Bool }
        };

        ProjectSettings.AddPropertyInfo(propertyInfo);
        ProjectSettings.SetAsBasic(fullPath, isBasic);
        ProjectSettings.SetInitialValue(fullPath, defaultValue);

        _registeredSettings.Add(path);
    }

    private void RegisterIntRange(string path, int defaultValue, int min, int max, bool isBasic = false)
    {
        var fullPath = Prefix + path;

        if (!ProjectSettings.HasSetting(fullPath))
        {
            ProjectSettings.SetSetting(fullPath, defaultValue);
        }

        var propertyInfo = new Godot.Collections.Dictionary
        {
            { "name", fullPath },
            { "type", (int)Variant.Type.Int },
            { "hint", (int)PropertyHint.Range },
            { "hint_string", $"{min},{max},1" }
        };

        ProjectSettings.AddPropertyInfo(propertyInfo);
        ProjectSettings.SetAsBasic(fullPath, isBasic);
        ProjectSettings.SetInitialValue(fullPath, defaultValue);

        _registeredSettings.Add(path);
    }

    private void RegisterEnum(string path, int defaultValue, string options, bool isBasic = false)
    {
        var fullPath = Prefix + path;

        if (!ProjectSettings.HasSetting(fullPath))
        {
            ProjectSettings.SetSetting(fullPath, defaultValue);
        }

        var propertyInfo = new Godot.Collections.Dictionary
        {
            { "name", fullPath },
            { "type", (int)Variant.Type.Int },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", options }
        };

        ProjectSettings.AddPropertyInfo(propertyInfo);
        ProjectSettings.SetAsBasic(fullPath, isBasic);
        ProjectSettings.SetInitialValue(fullPath, defaultValue);

        _registeredSettings.Add(path);
    }

    #endregion
}
