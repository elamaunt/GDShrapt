using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// A logger that discards all messages.
/// </summary>
public class GDNullLogger : IGDLogger
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GDNullLogger Instance { get; } = new GDNullLogger();

    /// <summary>
    /// Always returns Silent (no output).
    /// </summary>
    public GDLogLevel MinLevel { get; set; } = GDLogLevel.Silent;

    /// <summary>
    /// Always returns false (nothing is enabled).
    /// </summary>
    public bool IsEnabled(GDLogLevel level) => false;

    public void Verbose(string message) { }
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
    public void Error(string message, Exception ex) { }
}
