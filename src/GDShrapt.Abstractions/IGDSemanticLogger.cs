namespace GDShrapt.Abstractions;

/// <summary>
/// Abstraction for logging in semantic analysis.
/// </summary>
public interface IGDSemanticLogger
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void Error(string message);
}
