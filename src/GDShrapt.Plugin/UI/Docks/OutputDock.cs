using Godot;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

/// <summary>
/// Bottom panel dock that displays log output from the plugin.
/// Provides filtering, clearing, and saving functionality.
/// </summary>
[Tool]
public partial class OutputDock : Control
{
    private RichTextLabel _outputLabel;
    private OptionButton _levelFilter;
    private Button _clearButton;
    private Button _saveButton;
    private CheckButton _autoScrollCheck;

    private readonly List<LogEntry> _logEntries = new();
    private LogLevel _minDisplayLevel = LogLevel.Info;
    private bool _autoScroll = true;
    private const int MaxLogEntries = 1000;

    public override void _Ready()
    {
        CreateUI();
        SubscribeToLogger();
    }

    public override void _ExitTree()
    {
        UnsubscribeFromLogger();
    }

    private void CreateUI()
    {
        // Main container
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(mainVBox);

        // Toolbar
        var toolbar = new HBoxContainer();
        mainVBox.AddChild(toolbar);

        // Level filter
        var filterLabel = new Label { Text = "Level:" };
        toolbar.AddChild(filterLabel);

        _levelFilter = new OptionButton();
        _levelFilter.AddItem("Debug", (int)LogLevel.Debug);
        _levelFilter.AddItem("Info", (int)LogLevel.Info);
        _levelFilter.AddItem("Warning", (int)LogLevel.Warning);
        _levelFilter.AddItem("Error", (int)LogLevel.Error);
        _levelFilter.Select(1); // Default to Info
        _levelFilter.ItemSelected += OnLevelFilterChanged;
        toolbar.AddChild(_levelFilter);

        // Auto-scroll toggle
        _autoScrollCheck = new CheckButton { Text = "Auto-scroll", ButtonPressed = true };
        _autoScrollCheck.Toggled += OnAutoScrollToggled;
        toolbar.AddChild(_autoScrollCheck);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        toolbar.AddChild(spacer);

        // Clear button
        _clearButton = new Button { Text = "Clear" };
        _clearButton.Pressed += OnClearPressed;
        toolbar.AddChild(_clearButton);

        // Save button
        _saveButton = new Button { Text = "Save" };
        _saveButton.Pressed += OnSavePressed;
        toolbar.AddChild(_saveButton);

        // Output area
        var scrollContainer = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        mainVBox.AddChild(scrollContainer);

        _outputLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SelectionEnabled = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FitContent = true
        };
        scrollContainer.AddChild(_outputLabel);

        // Set minimum size
        CustomMinimumSize = new Vector2(200, 100);
    }

    private void SubscribeToLogger()
    {
        Logger.OnLogMessage += OnLogMessage;
    }

    private void UnsubscribeFromLogger()
    {
        Logger.OnLogMessage -= OnLogMessage;
    }

    private void OnLogMessage(LogLevel level, string message, DateTime timestamp)
    {
        // Store the entry
        var entry = new LogEntry(level, message, timestamp);
        _logEntries.Add(entry);

        // Limit entries to prevent memory issues
        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.RemoveAt(0);
        }

        // Only display if meets minimum level
        if (level >= _minDisplayLevel)
        {
            AppendLogEntry(entry);
        }
    }

    private void AppendLogEntry(LogEntry entry)
    {
        var color = GetColorForLevel(entry.Level);
        var levelPrefix = GetLevelPrefix(entry.Level);
        var formattedTime = entry.Timestamp.ToString("HH:mm:ss");

        _outputLabel.AppendText($"[color=#{color}][{formattedTime}] {levelPrefix} {entry.Message}[/color]\n");

        if (_autoScroll)
        {
            _outputLabel.ScrollToLine(_outputLabel.GetLineCount() - 1);
        }
    }

    private static string GetColorForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "808080", // Gray
            LogLevel.Info => "FFFFFF", // White
            LogLevel.Warning => "FFD700", // Gold
            LogLevel.Error => "FF4444", // Red
            _ => "FFFFFF"
        };
    }

    private static string GetLevelPrefix(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[LOG]"
        };
    }

    private void OnLevelFilterChanged(long index)
    {
        _minDisplayLevel = (LogLevel)_levelFilter.GetItemId((int)index);
        RefreshDisplay();
    }

    private void OnAutoScrollToggled(bool toggled)
    {
        _autoScroll = toggled;
    }

    private void OnClearPressed()
    {
        _logEntries.Clear();
        _outputLabel.Clear();
    }

    private void OnSavePressed()
    {
        // Create file dialog for saving
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.log ; Log files", "*.txt ; Text files" },
            CurrentFile = $"gdshrapt_log_{DateTime.Now:yyyyMMdd_HHmmss}.log"
        };
        dialog.FileSelected += OnLogFileSaveSelected;
        GetTree().Root.AddChild(dialog);
        dialog.PopupCentered(new Vector2I(600, 400));
    }

    private void OnLogFileSaveSelected(string path)
    {
        try
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            if (file != null)
            {
                foreach (var entry in _logEntries)
                {
                    var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {GetLevelPrefix(entry.Level)} {entry.Message}";
                    file.StoreLine(line);
                }
                Logger.Info($"Log saved to: {path}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save log file: {ex.Message}");
        }
    }

    private void RefreshDisplay()
    {
        _outputLabel.Clear();
        foreach (var entry in _logEntries)
        {
            if (entry.Level >= _minDisplayLevel)
            {
                AppendLogEntry(entry);
            }
        }
    }

    /// <summary>
    /// Represents a single log entry.
    /// </summary>
    private record LogEntry(LogLevel Level, string Message, DateTime Timestamp);
}
