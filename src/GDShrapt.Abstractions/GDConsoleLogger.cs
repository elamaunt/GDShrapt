using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// A logger that writes to the console with level filtering.
/// </summary>
public class GDConsoleLogger : IGDLogger
{
    /// <summary>
    /// Singleton instance with default Info level.
    /// </summary>
    public static GDConsoleLogger Instance { get; } = new GDConsoleLogger();

    /// <summary>
    /// Current minimum log level. Messages below this level are ignored.
    /// </summary>
    public GDLogLevel MinLevel { get; set; } = GDLogLevel.Info;

    /// <summary>
    /// Checks if a log level is enabled.
    /// </summary>
    public bool IsEnabled(GDLogLevel level) => level >= MinLevel;

    public void Verbose(string message)
    {
        if (IsEnabled(GDLogLevel.Verbose))
            Console.WriteLine($"[VERBOSE] {message}");
    }

    public void Debug(string message)
    {
        if (IsEnabled(GDLogLevel.Debug))
            Console.WriteLine($"[DEBUG] {message}");
    }

    public void Info(string message)
    {
        if (IsEnabled(GDLogLevel.Info))
            Console.WriteLine($"[INFO] {message}");
    }

    public void Warning(string message)
    {
        if (IsEnabled(GDLogLevel.Warning))
            Console.WriteLine($"[WARNING] {message}");
    }

    public void Error(string message)
    {
        if (IsEnabled(GDLogLevel.Error))
            Console.WriteLine($"[ERROR] {message}");
    }

    public void Error(string message, Exception ex)
    {
        if (IsEnabled(GDLogLevel.Error))
            Console.WriteLine($"[ERROR] {message}: {ex.Message}");
    }
}
