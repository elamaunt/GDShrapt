namespace GDShrapt.Plugin.Localization;

/// <summary>
/// String key constants for localization.
/// </summary>
public static class Strings
{
    // Plugin general
    public const string PluginName = "plugin.name";
    public const string PluginDescription = "plugin.description";

    // Menu items
    public const string MenuFormat = "menu.format";
    public const string MenuRename = "menu.rename";
    public const string MenuGoToDefinition = "menu.go_to_definition";
    public const string MenuFindReferences = "menu.find_references";
    public const string MenuExtractMethod = "menu.extract_method";
    public const string MenuRemoveComments = "menu.remove_comments";
    public const string MenuSettings = "menu.settings";
    public const string MenuAbout = "menu.about";

    // Settings panel
    public const string SettingsTitle = "settings.title";
    public const string SettingsLogging = "settings.logging";
    public const string SettingsLogLevel = "settings.log_level";
    public const string SettingsLogToFile = "settings.log_to_file";
    public const string SettingsLogFilePath = "settings.log_file_path";
    public const string SettingsLanguage = "settings.language";
    public const string SettingsSave = "settings.save";
    public const string SettingsReset = "settings.reset";

    // Log levels
    public const string LogLevelDebug = "log_level.debug";
    public const string LogLevelInfo = "log_level.info";
    public const string LogLevelWarning = "log_level.warning";
    public const string LogLevelError = "log_level.error";

    // Output dock
    public const string OutputTitle = "output.title";
    public const string OutputClear = "output.clear";
    public const string OutputSave = "output.save";
    public const string OutputAutoScroll = "output.auto_scroll";
    public const string OutputLevel = "output.level";

    // Dialogs
    public const string DialogRename = "dialog.rename";
    public const string DialogRenamePrompt = "dialog.rename_prompt";
    public const string DialogExtractMethod = "dialog.extract_method";
    public const string DialogExtractMethodPrompt = "dialog.extract_method_prompt";
    public const string DialogOk = "dialog.ok";
    public const string DialogCancel = "dialog.cancel";

    // Messages
    public const string MessageSettingsSaved = "message.settings_saved";
    public const string MessageSettingsReset = "message.settings_reset";
    public const string MessageLogSaved = "message.log_saved";
    public const string MessageNoSelection = "message.no_selection";
    public const string MessageOperationComplete = "message.operation_complete";
    public const string MessageOperationFailed = "message.operation_failed";

    // Errors
    public const string ErrorFileNotFound = "error.file_not_found";
    public const string ErrorInvalidSyntax = "error.invalid_syntax";
    public const string ErrorUnknownType = "error.unknown_type";

    // About panel
    public const string AboutVersion = "about.version";
    public const string AboutAuthor = "about.author";
    public const string AboutGithub = "about.github";
    public const string AboutDonate = "about.donate";

    // Support button
    public const string SupportButton = "support.button";

    // References dock
    public const string ReferencesCopyList = "references.copy_list";
    public const string ReferencesNoResults = "references.no_results";

    // TODO Tags dock
    public const string TodoTagsTitle = "todo_tags.title";
    public const string TodoTagsRefresh = "todo_tags.refresh";
    public const string TodoTagsNoResults = "todo_tags.no_results";
    public const string TodoTagsSettings = "todo_tags.settings";
}
