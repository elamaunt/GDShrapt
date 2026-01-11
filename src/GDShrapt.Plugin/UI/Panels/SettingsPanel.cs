using Godot;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Settings panel for configuring GDShrapt plugin options.
/// </summary>
public partial class SettingsPanel : Control
{
    private OptionButton _logLevelOption;
    private CheckButton _logToFileCheck;
    private LineEdit _logFilePathEdit;
    private OptionButton _languageOption;
    private Button _saveButton;
    private Button _resetButton;

    public override void _Ready()
    {
        CreateUI();
        LoadSettings();
    }

    private void CreateUI()
    {
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(mainVBox);

        // Title
        var title = new Label
        {
            Text = "GDShrapt Settings",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        mainVBox.AddChild(title);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Logging section
        var loggingLabel = new Label { Text = "Logging" };
        loggingLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(loggingLabel);

        // Log level
        var logLevelRow = new HBoxContainer();
        logLevelRow.AddChild(new Label { Text = "Log Level:", CustomMinimumSize = new Vector2(120, 0) });
        _logLevelOption = new OptionButton();
        _logLevelOption.AddItem("Debug", (int)LogLevel.Debug);
        _logLevelOption.AddItem("Info", (int)LogLevel.Info);
        _logLevelOption.AddItem("Warning", (int)LogLevel.Warning);
        _logLevelOption.AddItem("Error", (int)LogLevel.Error);
        _logLevelOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        logLevelRow.AddChild(_logLevelOption);
        mainVBox.AddChild(logLevelRow);

        // Log to file
        var logToFileRow = new HBoxContainer();
        logToFileRow.AddChild(new Label { Text = "Log to File:", CustomMinimumSize = new Vector2(120, 0) });
        _logToFileCheck = new CheckButton();
        _logToFileCheck.Toggled += OnLogToFileToggled;
        logToFileRow.AddChild(_logToFileCheck);
        mainVBox.AddChild(logToFileRow);

        // Log file path
        var logPathRow = new HBoxContainer();
        logPathRow.AddChild(new Label { Text = "Log File Path:", CustomMinimumSize = new Vector2(120, 0) });
        _logFilePathEdit = new LineEdit
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "user://gdshrapt.log"
        };
        logPathRow.AddChild(_logFilePathEdit);
        mainVBox.AddChild(logPathRow);

        mainVBox.AddChild(new HSeparator());

        // Localization section
        var locLabel = new Label { Text = "Localization" };
        locLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(locLabel);

        // Language selection
        var langRow = new HBoxContainer();
        langRow.AddChild(new Label { Text = "Language:", CustomMinimumSize = new Vector2(120, 0) });
        _languageOption = new OptionButton();
        _languageOption.AddItem("English", 0);
        _languageOption.AddItem("Russian", 1);
        _languageOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        langRow.AddChild(_languageOption);
        mainVBox.AddChild(langRow);

        mainVBox.AddChild(new HSeparator());

        // Buttons
        var buttonRow = new HBoxContainer();
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buttonRow.AddChild(spacer);

        _resetButton = new Button { Text = "Reset to Defaults" };
        _resetButton.Pressed += OnResetPressed;
        buttonRow.AddChild(_resetButton);

        _saveButton = new Button { Text = "Save Settings" };
        _saveButton.Pressed += OnSavePressed;
        buttonRow.AddChild(_saveButton);

        mainVBox.AddChild(buttonRow);
    }

    private void LoadSettings()
    {
        // Load from Logger
        _logLevelOption.Select(GetLogLevelIndex(Logger.MinLevel));
        _logToFileCheck.ButtonPressed = Logger.LogToFile;
        _logFilePathEdit.Text = Logger.LogFilePath;
        _logFilePathEdit.Editable = Logger.LogToFile;

        // Load language setting (default to English)
        _languageOption.Select(0);
    }

    private static int GetLogLevelIndex(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => 0,
            LogLevel.Info => 1,
            LogLevel.Warning => 2,
            LogLevel.Error => 3,
            _ => 1
        };
    }

    private void OnLogToFileToggled(bool toggled)
    {
        _logFilePathEdit.Editable = toggled;
    }

    private void OnSavePressed()
    {
        // Apply settings
        Logger.MinLevel = (LogLevel)_logLevelOption.GetItemId(_logLevelOption.Selected);
        Logger.LogToFile = _logToFileCheck.ButtonPressed;
        Logger.LogFilePath = _logFilePathEdit.Text;

        // Save to config file
        SaveToConfig();

        Logger.Info("Settings saved");
    }

    private void OnResetPressed()
    {
        _logLevelOption.Select(1); // Info
        _logToFileCheck.ButtonPressed = false;
        _logFilePathEdit.Text = "user://gdshrapt.log";
        _languageOption.Select(0); // English

        OnLogToFileToggled(false);
        Logger.Info("Settings reset to defaults");
    }

    private void SaveToConfig()
    {
        var config = new ConfigFile();
        config.SetValue("logging", "level", (int)Logger.MinLevel);
        config.SetValue("logging", "to_file", Logger.LogToFile);
        config.SetValue("logging", "file_path", Logger.LogFilePath);
        config.SetValue("localization", "language", _languageOption.Selected);

        var error = config.Save("user://gdshrapt_settings.cfg");
        if (error != Error.Ok)
        {
            Logger.Error($"Failed to save settings: {error}");
        }
    }

    /// <summary>
    /// Loads settings from the config file.
    /// </summary>
    public static void LoadFromConfig()
    {
        var config = new ConfigFile();
        var error = config.Load("user://gdshrapt_settings.cfg");

        if (error == Error.Ok)
        {
            Logger.MinLevel = (LogLevel)config.GetValue("logging", "level", (int)LogLevel.Info).AsInt32();
            Logger.LogToFile = config.GetValue("logging", "to_file", false).AsBool();
            Logger.LogFilePath = config.GetValue("logging", "file_path", "user://gdshrapt.log").AsString();
        }
    }
}
