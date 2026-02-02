using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Log level for filtering messages.
/// </summary>
public enum GDLogLevel
{
    /// <summary>Most detailed, for deep troubleshooting.</summary>
    Verbose = 0,

    /// <summary>Development diagnostics.</summary>
    Debug = 1,

    /// <summary>Progress milestones and summaries (default).</summary>
    Info = 2,

    /// <summary>Recoverable issues.</summary>
    Warning = 3,

    /// <summary>Operation failures.</summary>
    Error = 4,

    /// <summary>No output except fatal errors.</summary>
    Silent = 5
}

/// <summary>
/// Abstraction for logging in GDShrapt tooling (CLI, Plugin, LSP).
/// </summary>
public interface IGDLogger
{
    /// <summary>
    /// Current minimum log level. Messages below this level are ignored.
    /// </summary>
    GDLogLevel MinLevel { get; set; }

    /// <summary>
    /// Checks if a log level is enabled.
    /// </summary>
    bool IsEnabled(GDLogLevel level);

    /// <summary>
    /// Logs a verbose message (most detailed).
    /// </summary>
    void Verbose(string message);

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

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    void Error(string message, Exception ex);
}
