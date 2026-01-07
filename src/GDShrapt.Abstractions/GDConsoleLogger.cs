using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// A logger that writes to the console.
/// </summary>
public class GDConsoleLogger : IGDSemanticLogger
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GDConsoleLogger Instance { get; } = new GDConsoleLogger();

    public void Debug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }

    public void Info(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
}
