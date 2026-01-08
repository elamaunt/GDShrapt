using System;
using System.Diagnostics;
using System.IO;

namespace GDShrapt.Plugin;

/// <summary>
/// Log level for filtering messages.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// Centralized logging system for GDShrapt.Plugin.
/// Supports console output, file logging, and event-based subscriptions.
/// </summary>
internal static class Logger
{
    private static readonly object _lock = new();
    private static StreamWriter? _fileWriter;

    /// <summary>
    /// Minimum log level to output. Messages below this level are ignored.
    /// </summary>
    public static LogLevel MinLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Whether to write logs to a file.
    /// </summary>
    public static bool LogToFile { get; set; }

    /// <summary>
    /// Path to the log file. Default is "gdshrapt.log" in the user directory.
    /// </summary>
    public static string LogFilePath { get; set; } = "user://gdshrapt.log";

    /// <summary>
    /// Event fired when a log message is written.
    /// Parameters: LogLevel, message, timestamp.
    /// </summary>
    public static event Action<LogLevel, string, DateTime>? OnLogMessage;

    /// <summary>
    /// Logs a debug message. Only shown when MinLevel is Debug.
    /// </summary>
    public static void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }

    /// <summary>
    /// Logs an exception with stack trace.
    /// </summary>
    public static void Error(Exception ex)
    {
        Log(LogLevel.Error, $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    /// <summary>
    /// Logs an error message with an associated exception.
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        Log(LogLevel.Error, $"{message}\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    private static void Log(LogLevel level, string message)
    {
        if (level < MinLevel)
            return;

        var timestamp = DateTime.Now;
        var prefix = level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[LOG]"
        };

        var formattedMessage = $"{timestamp:HH:mm:ss} {prefix} {message}";
        var godotMessage = $"GDShrapt: {formattedMessage}";

        // Only write errors to Godot console (plugin has its own log window)
        if (level == LogLevel.Error)
            Godot.GD.PushError(godotMessage);

        // Write to debug output
        System.Diagnostics.Debug.WriteLine(godotMessage);

        // Fire event for UI listeners (like Output dock)
        try
        {
            OnLogMessage?.Invoke(level, message, timestamp);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GDShrapt: Error in log event handler: {ex.Message}");
        }

        // Write to file if enabled
        if (LogToFile)
        {
            WriteToFile(formattedMessage);
        }
    }

    private static void WriteToFile(string message)
    {
        lock (_lock)
        {
            try
            {
                EnsureFileWriter();
                _fileWriter?.WriteLine(message);
                _fileWriter?.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GDShrapt: Failed to write to log file: {ex.Message}");
            }
        }
    }

    private static void EnsureFileWriter()
    {
        if (_fileWriter != null)
            return;

        try
        {
            var path = LogFilePath;

            // Handle Godot res:// and user:// paths
            if (path.StartsWith("user://"))
            {
                // In Godot, user:// maps to the user data directory
                // For now, use a temp directory fallback
                var userDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                path = Path.Combine(userDir, "Godot", "gdshrapt", path.Substring(7));
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _fileWriter = new StreamWriter(path, append: true) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GDShrapt: Failed to create log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes the log file if open.
    /// </summary>
    public static void Close()
    {
        lock (_lock)
        {
            _fileWriter?.Dispose();
            _fileWriter = null;
        }
    }

    /// <summary>
    /// Clears all event handlers.
    /// </summary>
    public static void ClearHandlers()
    {
        OnLogMessage = null;
    }
}
