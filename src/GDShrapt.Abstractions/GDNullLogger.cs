namespace GDShrapt.Abstractions;

/// <summary>
/// A logger that discards all messages.
/// </summary>
public class GDNullLogger : IGDSemanticLogger
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GDNullLogger Instance { get; } = new GDNullLogger();

    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
}
