using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GDShrapt.Plugin;

/// <summary>
/// Manages localization for the GDShrapt plugin.
/// Supports loading translations from JSON files and provides translation lookup.
/// </summary>
internal static class LocalizationManager
{
    private static Dictionary<string, string> _translations = new();
    private static string _currentLanguage = "en";
    private static readonly string[] SupportedLanguages = { "en", "ru" };

    /// <summary>
    /// Gets the current language code.
    /// </summary>
    public static string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Gets the list of supported language codes.
    /// </summary>
    public static IReadOnlyList<string> Languages => SupportedLanguages;

    /// <summary>
    /// Event fired when the language changes.
    /// </summary>
    public static event Action<string>? OnLanguageChanged;

    /// <summary>
    /// Initializes the localization system with the specified language.
    /// </summary>
    public static void Initialize(string language = "en")
    {
        SetLanguage(language);
    }

    /// <summary>
    /// Sets the current language and loads translations.
    /// </summary>
    public static void SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            language = "en";

        // Validate language
        var normalizedLang = language.ToLowerInvariant();
        if (!Array.Exists(SupportedLanguages, l => l == normalizedLang))
        {
            Logger.Warning($"Unsupported language '{language}', falling back to English");
            normalizedLang = "en";
        }

        if (_currentLanguage == normalizedLang && _translations.Count > 0)
            return;

        _currentLanguage = normalizedLang;
        LoadTranslations();
        OnLanguageChanged?.Invoke(_currentLanguage);
    }

    /// <summary>
    /// Translates a key to the current language.
    /// Returns the key itself if no translation is found.
    /// </summary>
    public static string Tr(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        if (_translations.TryGetValue(key, out var translation))
            return translation;

        Logger.Debug($"Missing translation for key: {key}");
        return key;
    }

    /// <summary>
    /// Translates a key with format arguments.
    /// </summary>
    public static string Tr(string key, params object[] args)
    {
        var translation = Tr(key);
        try
        {
            return string.Format(translation, args);
        }
        catch (FormatException)
        {
            Logger.Warning($"Format error for translation key: {key}");
            return translation;
        }
    }

    /// <summary>
    /// Gets a display name for a language code.
    /// </summary>
    public static string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "en" => "English",
            "ru" => "Русский",
            _ => languageCode
        };
    }

    private static void LoadTranslations()
    {
        _translations.Clear();

        // Try to load from embedded resource first
        var jsonContent = LoadEmbeddedTranslations(_currentLanguage);

        if (string.IsNullOrEmpty(jsonContent))
        {
            // Fall back to file-based loading
            jsonContent = LoadTranslationsFromFile(_currentLanguage);
        }

        if (string.IsNullOrEmpty(jsonContent))
        {
            Logger.Warning($"Could not load translations for language: {_currentLanguage}");
            // Load fallback English if not already trying English
            if (_currentLanguage != "en")
            {
                jsonContent = LoadEmbeddedTranslations("en") ?? LoadTranslationsFromFile("en");
            }
        }

        if (!string.IsNullOrEmpty(jsonContent))
        {
            ParseTranslations(jsonContent);
        }
        else
        {
            Logger.Error("Failed to load any translations, using keys as values");
            LoadDefaultTranslations();
        }

        Logger.Info($"Loaded {_translations.Count} translations for language: {_currentLanguage}");
    }

    private static string? LoadEmbeddedTranslations(string language)
    {
        // Load from embedded JSON strings
        return language switch
        {
            "en" => GetEnglishTranslations(),
            "ru" => GetRussianTranslations(),
            _ => null
        };
    }

    private static string? LoadTranslationsFromFile(string language)
    {
        try
        {
            // Try plugin addon directory first
            var addonPath = $"res://addons/gdshrapt/localization/{language}.json";
            if (Godot.FileAccess.FileExists(addonPath))
            {
                using var file = Godot.FileAccess.Open(addonPath, Godot.FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    return file.GetAsText();
                }
            }

            // Try user directory
            var userPath = $"user://gdshrapt/localization/{language}.json";
            if (Godot.FileAccess.FileExists(userPath))
            {
                using var file = Godot.FileAccess.Open(userPath, Godot.FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    return file.GetAsText();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading translation file: {ex.Message}");
        }

        return null;
    }

    private static void ParseTranslations(string jsonContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, options);
            if (dict != null)
            {
                _translations = dict;
            }
        }
        catch (JsonException ex)
        {
            Logger.Error($"Error parsing translations JSON: {ex.Message}");
            LoadDefaultTranslations();
        }
    }

    private static void LoadDefaultTranslations()
    {
        // Load minimal English defaults
        _translations = new Dictionary<string, string>
        {
            [Strings.PluginName] = "GDShrapt",
            [Strings.PluginDescription] = "Professional GDScript IDE features for Godot",
            [Strings.MenuFormat] = "Format Code",
            [Strings.MenuRename] = "Rename",
            [Strings.MenuGoToDefinition] = "Go to Definition",
            [Strings.MenuFindReferences] = "Find References",
            [Strings.MenuExtractMethod] = "Extract Method",
            [Strings.MenuRemoveComments] = "Remove Comments",
            [Strings.MenuSettings] = "Settings",
            [Strings.MenuAbout] = "About",
            [Strings.DialogOk] = "OK",
            [Strings.DialogCancel] = "Cancel"
        };
    }

    private static string GetEnglishTranslations()
    {
        return """
{
    "plugin.name": "GDShrapt",
    "plugin.description": "Professional GDScript IDE features for Godot",

    "menu.format": "Format Code",
    "menu.rename": "Rename",
    "menu.go_to_definition": "Go to Definition",
    "menu.find_references": "Find References",
    "menu.extract_method": "Extract Method",
    "menu.remove_comments": "Remove Comments",
    "menu.settings": "Settings",
    "menu.about": "About",

    "settings.title": "GDShrapt Settings",
    "settings.logging": "Logging",
    "settings.log_level": "Log Level",
    "settings.log_to_file": "Log to File",
    "settings.log_file_path": "Log File Path",
    "settings.language": "Language",
    "settings.save": "Save",
    "settings.reset": "Reset to Defaults",

    "log_level.debug": "Debug",
    "log_level.info": "Info",
    "log_level.warning": "Warning",
    "log_level.error": "Error",

    "output.title": "GDShrapt Output",
    "output.clear": "Clear",
    "output.save": "Save Log",
    "output.auto_scroll": "Auto-scroll",
    "output.level": "Level",

    "dialog.rename": "Rename Symbol",
    "dialog.rename_prompt": "Enter new name:",
    "dialog.extract_method": "Extract Method",
    "dialog.extract_method_prompt": "Enter method name:",
    "dialog.ok": "OK",
    "dialog.cancel": "Cancel",

    "message.settings_saved": "Settings saved successfully",
    "message.settings_reset": "Settings reset to defaults",
    "message.log_saved": "Log saved to: {0}",
    "message.no_selection": "No text selected",
    "message.operation_complete": "Operation completed successfully",
    "message.operation_failed": "Operation failed: {0}",

    "error.file_not_found": "File not found: {0}",
    "error.invalid_syntax": "Invalid syntax at line {0}",
    "error.unknown_type": "Unknown type: {0}",

    "about.version": "Version: {0}",
    "about.author": "Author: elamaunt",
    "about.github": "GitHub",
    "about.donate": "Support Development",

    "support.button": "☕ Support GDShrapt",

    "references.copy_list": "Copy List",
    "references.no_results": "No references found",

    "todo_tags.title": "TODO Tags",
    "todo_tags.refresh": "Refresh",
    "todo_tags.no_results": "No TODO tags found",
    "todo_tags.settings": "Settings"
}
""";
    }

    private static string GetRussianTranslations()
    {
        return """
{
    "plugin.name": "GDShrapt",
    "plugin.description": "Профессиональные инструменты для GDScript в Godot",

    "menu.format": "Форматировать код",
    "menu.rename": "Переименовать",
    "menu.go_to_definition": "Перейти к определению",
    "menu.find_references": "Найти ссылки",
    "menu.extract_method": "Извлечь метод",
    "menu.remove_comments": "Удалить комментарии",
    "menu.settings": "Настройки",
    "menu.about": "О плагине",

    "settings.title": "Настройки GDShrapt",
    "settings.logging": "Логирование",
    "settings.log_level": "Уровень логов",
    "settings.log_to_file": "Записывать в файл",
    "settings.log_file_path": "Путь к файлу логов",
    "settings.language": "Язык",
    "settings.save": "Сохранить",
    "settings.reset": "Сбросить настройки",

    "log_level.debug": "Отладка",
    "log_level.info": "Информация",
    "log_level.warning": "Предупреждение",
    "log_level.error": "Ошибка",

    "output.title": "Вывод GDShrapt",
    "output.clear": "Очистить",
    "output.save": "Сохранить лог",
    "output.auto_scroll": "Автопрокрутка",
    "output.level": "Уровень",

    "dialog.rename": "Переименование символа",
    "dialog.rename_prompt": "Введите новое имя:",
    "dialog.extract_method": "Извлечение метода",
    "dialog.extract_method_prompt": "Введите имя метода:",
    "dialog.ok": "ОК",
    "dialog.cancel": "Отмена",

    "message.settings_saved": "Настройки сохранены",
    "message.settings_reset": "Настройки сброшены",
    "message.log_saved": "Лог сохранён в: {0}",
    "message.no_selection": "Текст не выделен",
    "message.operation_complete": "Операция выполнена успешно",
    "message.operation_failed": "Ошибка операции: {0}",

    "error.file_not_found": "Файл не найден: {0}",
    "error.invalid_syntax": "Ошибка синтаксиса в строке {0}",
    "error.unknown_type": "Неизвестный тип: {0}",

    "about.version": "Версия: {0}",
    "about.author": "Автор: elamaunt",
    "about.github": "GitHub",
    "about.donate": "Поддержать разработку",

    "support.button": "☕ Поддержать GDShrapt",

    "references.copy_list": "Копировать список",
    "references.no_results": "Ссылки не найдены",

    "todo_tags.title": "TODO теги",
    "todo_tags.refresh": "Обновить",
    "todo_tags.no_results": "TODO теги не найдены",
    "todo_tags.settings": "Настройки"
}
""";
    }

    /// <summary>
    /// Exports translations to a JSON file for customization.
    /// </summary>
    public static bool ExportTranslations(string path, string language)
    {
        try
        {
            var content = language switch
            {
                "en" => GetEnglishTranslations(),
                "ru" => GetRussianTranslations(),
                _ => GetEnglishTranslations()
            };

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(content);
                Logger.Info($"Exported {language} translations to: {path}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to export translations: {ex.Message}");
        }

        return false;
    }
}
