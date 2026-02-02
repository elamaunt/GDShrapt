using Godot;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

/// <summary>
/// Registers GDShrapt settings in Godot's Project Settings window.
/// Provides bidirectional sync between JSON config and ProjectSettings.
/// </summary>
internal class ProjectSettingsRegistry
{
    private const string Prefix = "gdshrapt/";

    private readonly GDConfigManager _configManager;
    private bool _isSyncing = false;
    private DateTime _lastSyncTime = DateTime.MinValue;

    // Track registered settings to avoid duplicates
    private readonly HashSet<string> _registeredSettings = new();

    public ProjectSettingsRegistry(GDConfigManager configManager)
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
        RegisterAnalysisSettings();
        RegisterValidationSettings();
        RegisterUISettings();
        RegisterCodeStyleSettings();
        RegisterLintingSettings();
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
            var plugin = config.Plugin ?? new GDPluginConfig();

            // General (plugin-specific)
            SetSetting("general/cache_enabled", plugin.Cache?.Enabled ?? true);

            // Analysis (plugin-specific)
            var analysis = plugin.Analysis ?? new GDPluginAnalysisConfig();
            SetSetting("analysis/log_level", (int)analysis.LogLevel);
            SetSetting("analysis/max_parallelism", analysis.MaxParallelism);
            SetSetting("analysis/timeout_seconds", analysis.TimeoutSeconds);
            SetSetting("analysis/incremental_enabled", analysis.IncrementalEnabled);
            SetSetting("analysis/max_issues", analysis.MaxIssues);
            SetSetting("analysis/file_change_debounce_ms", analysis.FileChangeDebounceMs);
            SetSetting("analysis/enable_call_site_registry", analysis.EnableCallSiteRegistry);
            SetSetting("analysis/incremental_reparse_threshold", analysis.IncrementalFullReparseThreshold);
            SetSetting("analysis/incremental_max_members", analysis.IncrementalMaxAffectedMembers);
            SetSetting("analysis/parallel_batch_size", analysis.ParallelBatchSize);

            // Validation (plugin-specific)
            SetSetting("validation/check_syntax", analysis.CheckSyntax);
            SetSetting("validation/check_scope", analysis.CheckScope);
            SetSetting("validation/check_types", analysis.CheckTypes);
            SetSetting("validation/check_calls", analysis.CheckCalls);
            SetSetting("validation/check_control_flow", analysis.CheckControlFlow);
            SetSetting("validation/check_indentation", analysis.CheckIndentation);
            SetSetting("validation/check_member_access", analysis.CheckMemberAccess);
            SetSetting("validation/check_abstract", analysis.CheckAbstract);
            SetSetting("validation/check_signals", analysis.CheckSignals);
            SetSetting("validation/check_resource_paths", analysis.CheckResourcePaths);

            // UI (plugin-specific)
            var ui = plugin.UI ?? new GDUIConfig();
            SetSetting("ui/ast_viewer_enabled", ui.AstViewerEnabled);
            SetSetting("ui/code_lens_enabled", ui.CodeLensEnabled);
            SetSetting("ui/references_counter_enabled", ui.ReferencesCounterEnabled);
            SetSetting("ui/problems_dock_enabled", ui.ProblemsDockEnabled);
            SetSetting("ui/todo_tags_dock_enabled", ui.TodoTagsDockEnabled);
            SetSetting("ui/find_references_dock_enabled", ui.FindReferencesDockEnabled);

            // Code Style (core config - linting + formatting)
            SetSetting("code_style/linting_enabled", config.Linting.Enabled);
            SetSetting("code_style/formatting_level", (int)config.Linting.FormattingLevel);
            SetSetting("code_style/indentation_style", (int)config.Linting.IndentationStyle);
            SetSetting("code_style/tab_width", config.Linting.TabWidth);
            SetSetting("code_style/max_line_length", config.Linting.MaxLineLength);
            SetSetting("code_style/indent_size", config.Formatter.IndentSize);
            SetSetting("code_style/line_ending", (int)config.Formatter.LineEnding);
            SetSetting("code_style/blank_lines_between_functions", config.Formatter.BlankLinesBetweenFunctions);
            SetSetting("code_style/blank_lines_after_class", config.Formatter.BlankLinesAfterClassDeclaration);
            SetSetting("code_style/blank_lines_between_members", config.Formatter.BlankLinesBetweenMemberTypes);
            SetSetting("code_style/space_around_operators", config.Formatter.SpaceAroundOperators);
            SetSetting("code_style/space_after_comma", config.Formatter.SpaceAfterComma);
            SetSetting("code_style/space_after_colon", config.Formatter.SpaceAfterColon);
            SetSetting("code_style/space_before_colon", config.Formatter.SpaceBeforeColon);
            SetSetting("code_style/space_inside_parens", config.Formatter.SpaceInsideParentheses);
            SetSetting("code_style/space_inside_brackets", config.Formatter.SpaceInsideBrackets);
            SetSetting("code_style/space_inside_braces", config.Formatter.SpaceInsideBraces);
            SetSetting("code_style/remove_trailing_whitespace", config.Formatter.RemoveTrailingWhitespace);
            SetSetting("code_style/ensure_trailing_newline", config.Formatter.EnsureTrailingNewline);
            SetSetting("code_style/remove_multiple_newlines", config.Formatter.RemoveMultipleTrailingNewlines);
            SetSetting("code_style/wrap_long_lines", config.Formatter.WrapLongLines);
            SetSetting("code_style/line_wrap_style", (int)config.Formatter.LineWrapStyle);
            SetSetting("code_style/continuation_indent", config.Formatter.ContinuationIndentSize);
            SetSetting("code_style/use_backslash", config.Formatter.UseBackslashContinuation);

            // Naming Conventions (core config)
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

            // Notifications (plugin-specific)
            var notifications = plugin.Notifications ?? new GDNotificationConfig();
            SetSetting("notifications/enabled", notifications.Enabled);
            SetSetting("notifications/show_expanded_on_first_open", notifications.ShowExpandedOnFirstOpen);
            SetSetting("notifications/auto_hide_seconds", notifications.AutoHideSeconds);
            SetSetting("notifications/min_severity", (int)notifications.MinSeverity);

            // TODO Tags (plugin-specific)
            var todoTags = plugin.TodoTags ?? new GDTodoTagsConfig();
            SetSetting("todo_tags/enabled", todoTags.Enabled);
            SetSetting("todo_tags/scan_on_startup", todoTags.ScanOnStartup);
            SetSetting("todo_tags/auto_refresh", todoTags.AutoRefresh);
            SetSetting("todo_tags/case_sensitive", todoTags.CaseSensitive);
            SetSetting("todo_tags/default_grouping", (int)todoTags.DefaultGrouping);

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

            // Ensure Plugin section exists
            config.Plugin ??= new GDPluginConfig();
            config.Plugin.UI ??= new GDUIConfig();
            config.Plugin.Cache ??= new GDCacheConfig();
            config.Plugin.Notifications ??= new GDNotificationConfig();
            config.Plugin.TodoTags ??= new GDTodoTagsConfig();
            config.Plugin.Analysis ??= new GDPluginAnalysisConfig();

            // General (plugin-specific)
            config.Plugin.Cache.Enabled = GetBool("general/cache_enabled", config.Plugin.Cache.Enabled);

            // Analysis (plugin-specific)
            config.Plugin.Analysis.LogLevel = (Abstractions.GDLogLevel)GetInt("analysis/log_level", (int)config.Plugin.Analysis.LogLevel);
            config.Plugin.Analysis.MaxParallelism = GetInt("analysis/max_parallelism", config.Plugin.Analysis.MaxParallelism);
            config.Plugin.Analysis.TimeoutSeconds = GetInt("analysis/timeout_seconds", config.Plugin.Analysis.TimeoutSeconds);
            config.Plugin.Analysis.IncrementalEnabled = GetBool("analysis/incremental_enabled", config.Plugin.Analysis.IncrementalEnabled);
            config.Plugin.Analysis.MaxIssues = GetInt("analysis/max_issues", config.Plugin.Analysis.MaxIssues);
            config.Plugin.Analysis.FileChangeDebounceMs = GetInt("analysis/file_change_debounce_ms", config.Plugin.Analysis.FileChangeDebounceMs);
            config.Plugin.Analysis.EnableCallSiteRegistry = GetBool("analysis/enable_call_site_registry", config.Plugin.Analysis.EnableCallSiteRegistry);
            config.Plugin.Analysis.IncrementalFullReparseThreshold = GetDouble("analysis/incremental_reparse_threshold", config.Plugin.Analysis.IncrementalFullReparseThreshold);
            config.Plugin.Analysis.IncrementalMaxAffectedMembers = GetInt("analysis/incremental_max_members", config.Plugin.Analysis.IncrementalMaxAffectedMembers);
            config.Plugin.Analysis.ParallelBatchSize = GetInt("analysis/parallel_batch_size", config.Plugin.Analysis.ParallelBatchSize);

            // Validation (plugin-specific)
            config.Plugin.Analysis.CheckSyntax = GetBool("validation/check_syntax", config.Plugin.Analysis.CheckSyntax);
            config.Plugin.Analysis.CheckScope = GetBool("validation/check_scope", config.Plugin.Analysis.CheckScope);
            config.Plugin.Analysis.CheckTypes = GetBool("validation/check_types", config.Plugin.Analysis.CheckTypes);
            config.Plugin.Analysis.CheckCalls = GetBool("validation/check_calls", config.Plugin.Analysis.CheckCalls);
            config.Plugin.Analysis.CheckControlFlow = GetBool("validation/check_control_flow", config.Plugin.Analysis.CheckControlFlow);
            config.Plugin.Analysis.CheckIndentation = GetBool("validation/check_indentation", config.Plugin.Analysis.CheckIndentation);
            config.Plugin.Analysis.CheckMemberAccess = GetBool("validation/check_member_access", config.Plugin.Analysis.CheckMemberAccess);
            config.Plugin.Analysis.CheckAbstract = GetBool("validation/check_abstract", config.Plugin.Analysis.CheckAbstract);
            config.Plugin.Analysis.CheckSignals = GetBool("validation/check_signals", config.Plugin.Analysis.CheckSignals);
            config.Plugin.Analysis.CheckResourcePaths = GetBool("validation/check_resource_paths", config.Plugin.Analysis.CheckResourcePaths);

            // UI (plugin-specific)
            config.Plugin.UI.AstViewerEnabled = GetBool("ui/ast_viewer_enabled", config.Plugin.UI.AstViewerEnabled);
            config.Plugin.UI.CodeLensEnabled = GetBool("ui/code_lens_enabled", config.Plugin.UI.CodeLensEnabled);
            config.Plugin.UI.ReferencesCounterEnabled = GetBool("ui/references_counter_enabled", config.Plugin.UI.ReferencesCounterEnabled);
            config.Plugin.UI.ProblemsDockEnabled = GetBool("ui/problems_dock_enabled", config.Plugin.UI.ProblemsDockEnabled);
            config.Plugin.UI.TodoTagsDockEnabled = GetBool("ui/todo_tags_dock_enabled", config.Plugin.UI.TodoTagsDockEnabled);
            config.Plugin.UI.FindReferencesDockEnabled = GetBool("ui/find_references_dock_enabled", config.Plugin.UI.FindReferencesDockEnabled);

            // Code Style (core config - linting + formatting)
            config.Linting.Enabled = GetBool("code_style/linting_enabled", config.Linting.Enabled);
            config.Linting.FormattingLevel = (GDFormattingLevel)GetInt("code_style/formatting_level", (int)config.Linting.FormattingLevel);
            config.Linting.IndentationStyle = (Semantics.GDIndentationStyle)GetInt("code_style/indentation_style", (int)config.Linting.IndentationStyle);
            config.Linting.TabWidth = GetInt("code_style/tab_width", config.Linting.TabWidth);
            config.Linting.MaxLineLength = GetInt("code_style/max_line_length", config.Linting.MaxLineLength);
            config.Formatter.IndentSize = GetInt("code_style/indent_size", config.Formatter.IndentSize);
            config.Formatter.LineEnding = (GDLineEndingStyle)GetInt("code_style/line_ending", (int)config.Formatter.LineEnding);
            config.Formatter.BlankLinesBetweenFunctions = GetInt("code_style/blank_lines_between_functions", config.Formatter.BlankLinesBetweenFunctions);
            config.Formatter.BlankLinesAfterClassDeclaration = GetInt("code_style/blank_lines_after_class", config.Formatter.BlankLinesAfterClassDeclaration);
            config.Formatter.BlankLinesBetweenMemberTypes = GetInt("code_style/blank_lines_between_members", config.Formatter.BlankLinesBetweenMemberTypes);
            config.Formatter.SpaceAroundOperators = GetBool("code_style/space_around_operators", config.Formatter.SpaceAroundOperators);
            config.Formatter.SpaceAfterComma = GetBool("code_style/space_after_comma", config.Formatter.SpaceAfterComma);
            config.Formatter.SpaceAfterColon = GetBool("code_style/space_after_colon", config.Formatter.SpaceAfterColon);
            config.Formatter.SpaceBeforeColon = GetBool("code_style/space_before_colon", config.Formatter.SpaceBeforeColon);
            config.Formatter.SpaceInsideParentheses = GetBool("code_style/space_inside_parens", config.Formatter.SpaceInsideParentheses);
            config.Formatter.SpaceInsideBrackets = GetBool("code_style/space_inside_brackets", config.Formatter.SpaceInsideBrackets);
            config.Formatter.SpaceInsideBraces = GetBool("code_style/space_inside_braces", config.Formatter.SpaceInsideBraces);
            config.Formatter.RemoveTrailingWhitespace = GetBool("code_style/remove_trailing_whitespace", config.Formatter.RemoveTrailingWhitespace);
            config.Formatter.EnsureTrailingNewline = GetBool("code_style/ensure_trailing_newline", config.Formatter.EnsureTrailingNewline);
            config.Formatter.RemoveMultipleTrailingNewlines = GetBool("code_style/remove_multiple_newlines", config.Formatter.RemoveMultipleTrailingNewlines);
            config.Formatter.WrapLongLines = GetBool("code_style/wrap_long_lines", config.Formatter.WrapLongLines);
            config.Formatter.LineWrapStyle = (GDLineWrapStyle)GetInt("code_style/line_wrap_style", (int)config.Formatter.LineWrapStyle);
            config.Formatter.ContinuationIndentSize = GetInt("code_style/continuation_indent", config.Formatter.ContinuationIndentSize);
            config.Formatter.UseBackslashContinuation = GetBool("code_style/use_backslash", config.Formatter.UseBackslashContinuation);

            // Naming Conventions (core config)
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

            // Notifications (plugin-specific)
            config.Plugin.Notifications.Enabled = GetBool("notifications/enabled", config.Plugin.Notifications.Enabled);
            config.Plugin.Notifications.ShowExpandedOnFirstOpen = GetBool("notifications/show_expanded_on_first_open", config.Plugin.Notifications.ShowExpandedOnFirstOpen);
            config.Plugin.Notifications.AutoHideSeconds = GetInt("notifications/auto_hide_seconds", config.Plugin.Notifications.AutoHideSeconds);
            config.Plugin.Notifications.MinSeverity = (GDDiagnosticSeverity)GetInt("notifications/min_severity", (int)config.Plugin.Notifications.MinSeverity);

            // TODO Tags (plugin-specific)
            config.Plugin.TodoTags.Enabled = GetBool("todo_tags/enabled", config.Plugin.TodoTags.Enabled);
            config.Plugin.TodoTags.ScanOnStartup = GetBool("todo_tags/scan_on_startup", config.Plugin.TodoTags.ScanOnStartup);
            config.Plugin.TodoTags.AutoRefresh = GetBool("todo_tags/auto_refresh", config.Plugin.TodoTags.AutoRefresh);
            config.Plugin.TodoTags.CaseSensitive = GetBool("todo_tags/case_sensitive", config.Plugin.TodoTags.CaseSensitive);
            config.Plugin.TodoTags.DefaultGrouping = (GDTodoGroupingMode)GetInt("todo_tags/default_grouping", (int)config.Plugin.TodoTags.DefaultGrouping);

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
        var plugin = config.Plugin ?? new GDPluginConfig();

        if (GetBool("code_style/linting_enabled", config.Linting.Enabled) != config.Linting.Enabled)
            return true;

        var notifications = plugin.Notifications ?? new GDNotificationConfig();
        if (GetBool("notifications/enabled", notifications.Enabled) != notifications.Enabled)
            return true;

        var todoTags = plugin.TodoTags ?? new GDTodoTagsConfig();
        if (GetBool("todo_tags/enabled", todoTags.Enabled) != todoTags.Enabled)
            return true;

        var ui = plugin.UI ?? new GDUIConfig();
        if (GetBool("ui/ast_viewer_enabled", ui.AstViewerEnabled) != ui.AstViewerEnabled)
            return true;

        if (GetBool("ui/code_lens_enabled", ui.CodeLensEnabled) != ui.CodeLensEnabled)
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

    private void RegisterAnalysisSettings()
    {
        RegisterEnum("analysis/log_level", 2, "Verbose,Debug,Info,Warning,Error,Silent", isBasic: true);
        RegisterIntRange("analysis/max_parallelism", -1, -1, 32, isBasic: true);
        RegisterIntRange("analysis/timeout_seconds", 30, 5, 300);
        RegisterBool("analysis/incremental_enabled", true, isBasic: true);
        RegisterIntRange("analysis/max_issues", 0, 0, 10000);
        RegisterIntRange("analysis/file_change_debounce_ms", 300, 50, 2000);
        RegisterBool("analysis/enable_call_site_registry", true);
        RegisterFloatRange("analysis/incremental_reparse_threshold", 0.5, 0.1, 1.0);
        RegisterIntRange("analysis/incremental_max_members", 3, 1, 10);
        RegisterIntRange("analysis/parallel_batch_size", 10, 1, 50);
    }

    private void RegisterValidationSettings()
    {
        RegisterBool("validation/check_syntax", true, isBasic: true);
        RegisterBool("validation/check_scope", true, isBasic: true);
        RegisterBool("validation/check_types", true, isBasic: true);
        RegisterBool("validation/check_calls", true, isBasic: true);
        RegisterBool("validation/check_control_flow", true);
        RegisterBool("validation/check_indentation", true);
        RegisterBool("validation/check_member_access", true, isBasic: true);
        RegisterBool("validation/check_abstract", true);
        RegisterBool("validation/check_signals", true);
        RegisterBool("validation/check_resource_paths", true);
    }

    private void RegisterLintingSettings()
    {
        // Complexity limits
        RegisterIntRange("linting/max_file_lines", 1000, 100, 5000);
        RegisterIntRange("linting/max_public_methods", 20, 5, 100);
        RegisterIntRange("linting/max_returns", 6, 1, 20);
        RegisterIntRange("linting/max_nesting_depth", 4, 2, 10);
        RegisterIntRange("linting/max_local_variables", 15, 5, 50);
        RegisterIntRange("linting/max_class_variables", 30, 10, 100);
        RegisterIntRange("linting/max_branches", 12, 5, 50);
        RegisterIntRange("linting/max_boolean_expressions", 5, 2, 10);
        RegisterIntRange("linting/max_inner_classes", 3, 1, 10);

        // Warnings
        RegisterBool("linting/warn_unused_signals", true);
        RegisterBool("linting/warn_empty_functions", true);
        RegisterBool("linting/warn_magic_numbers", false);
        RegisterBool("linting/warn_variable_shadowing", true);
        RegisterBool("linting/warn_await_in_loop", true);
        RegisterBool("linting/warn_no_elif_return", true);
        RegisterBool("linting/warn_no_else_return", true);
        RegisterBool("linting/warn_private_method_call", false);
        RegisterBool("linting/warn_duplicated_load", true);
        RegisterBool("linting/warn_expression_not_assigned", true);
        RegisterBool("linting/warn_useless_assignment", true);
        RegisterBool("linting/warn_inconsistent_return", true);
        RegisterBool("linting/warn_missing_return", true);
        RegisterBool("linting/warn_no_lonely_if", false);
        RegisterBool("linting/warn_god_class", false);
        RegisterBool("linting/warn_commented_code", false);
        RegisterBool("linting/warn_debug_print", true);

        // Strict typing
        RegisterEnum("linting/strict_typing", 0, "Off,Warning,Error");
        RegisterBool("linting/strict_typing_class_vars", false);
        RegisterBool("linting/strict_typing_local_vars", false);
        RegisterBool("linting/strict_typing_params", false);
        RegisterBool("linting/strict_typing_return", false);

        // God class thresholds
        RegisterIntRange("linting/god_class_max_variables", 20, 10, 100);
        RegisterIntRange("linting/god_class_max_methods", 20, 10, 100);
        RegisterIntRange("linting/god_class_max_lines", 500, 200, 2000);

        // Suppression
        RegisterBool("linting/enable_suppression", true);

        // Member ordering
        RegisterEnum("linting/abstract_method_position", 0, "Default,First,Last");
        RegisterEnum("linting/private_method_position", 0, "Default,First,Last");
        RegisterEnum("linting/static_method_position", 0, "Default,First,Last");
    }

    private void RegisterUISettings()
    {
        RegisterBool("ui/ast_viewer_enabled", true, isBasic: true);
        RegisterBool("ui/code_lens_enabled", true, isBasic: true);
        RegisterBool("ui/references_counter_enabled", true, isBasic: true);
        RegisterBool("ui/problems_dock_enabled", true, isBasic: true);
        RegisterBool("ui/todo_tags_dock_enabled", true, isBasic: true);
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
        RegisterIntRange("code_style/blank_lines_after_class", 1, 0, 5);
        RegisterIntRange("code_style/blank_lines_between_members", 1, 0, 5);
        RegisterBool("code_style/space_around_operators", true);
        RegisterBool("code_style/space_after_comma", true);
        RegisterBool("code_style/space_after_colon", true);
        RegisterBool("code_style/space_before_colon", false);
        RegisterBool("code_style/space_inside_parens", false);
        RegisterBool("code_style/space_inside_brackets", false);
        RegisterBool("code_style/space_inside_braces", true);
        RegisterBool("code_style/remove_trailing_whitespace", true);
        RegisterBool("code_style/ensure_trailing_newline", true);
        RegisterBool("code_style/remove_multiple_newlines", true);
        RegisterBool("code_style/wrap_long_lines", true);
        RegisterEnum("code_style/line_wrap_style", 0, "AfterOpen,Before");
        RegisterIntRange("code_style/continuation_indent", 4, 1, 8);
        RegisterBool("code_style/use_backslash", false);
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

    private void RegisterFloatRange(string path, double defaultValue, double min, double max, bool isBasic = false)
    {
        var fullPath = Prefix + path;

        if (!ProjectSettings.HasSetting(fullPath))
        {
            ProjectSettings.SetSetting(fullPath, defaultValue);
        }

        var propertyInfo = new Godot.Collections.Dictionary
        {
            { "name", fullPath },
            { "type", (int)Variant.Type.Float },
            { "hint", (int)PropertyHint.Range },
            { "hint_string", $"{min},{max},0.1" }
        };

        ProjectSettings.AddPropertyInfo(propertyInfo);
        ProjectSettings.SetAsBasic(fullPath, isBasic);
        ProjectSettings.SetInitialValue(fullPath, defaultValue);

        _registeredSettings.Add(path);
    }

    private double GetDouble(string path, double defaultValue)
    {
        var fullPath = Prefix + path;
        if (!ProjectSettings.HasSetting(fullPath))
            return defaultValue;

        var value = ProjectSettings.GetSetting(fullPath);
        return value.VariantType == Variant.Type.Float ? value.AsDouble() : defaultValue;
    }

    #endregion
}
