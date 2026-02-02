using System;
using GDShrapt.Abstractions;

namespace GDShrapt.Plugin;

/// <summary>
/// Adapter that bridges the Plugin's static Logger to the IGDLogger interface
/// used by GDShrapt.Semantics components.
/// This allows semantic analysis logging to appear in the Plugin's Output dock.
/// </summary>
internal sealed class SemanticLoggerAdapter : IGDLogger
{
    /// <summary>
    /// Singleton instance for use throughout the plugin.
    /// </summary>
    public static SemanticLoggerAdapter Instance { get; } = new SemanticLoggerAdapter();

    private SemanticLoggerAdapter() { }

    /// <summary>
    /// Maps to Plugin's Logger.MinLevel.
    /// Now supports all 6 levels aligned with GDLogLevel.
    /// </summary>
    public GDLogLevel MinLevel
    {
        get => Logger.MinLevel switch
        {
            LogLevel.Verbose => GDLogLevel.Verbose,
            LogLevel.Debug => GDLogLevel.Debug,
            LogLevel.Info => GDLogLevel.Info,
            LogLevel.Warning => GDLogLevel.Warning,
            LogLevel.Error => GDLogLevel.Error,
            LogLevel.Silent => GDLogLevel.Silent,
            _ => GDLogLevel.Info
        };
        set => Logger.MinLevel = value switch
        {
            GDLogLevel.Verbose => LogLevel.Verbose,
            GDLogLevel.Debug => LogLevel.Debug,
            GDLogLevel.Info => LogLevel.Info,
            GDLogLevel.Warning => LogLevel.Warning,
            GDLogLevel.Error => LogLevel.Error,
            GDLogLevel.Silent => LogLevel.Silent,
            _ => LogLevel.Info
        };
    }

    public bool IsEnabled(GDLogLevel level)
    {
        var pluginLevel = level switch
        {
            GDLogLevel.Verbose => LogLevel.Verbose,
            GDLogLevel.Debug => LogLevel.Debug,
            GDLogLevel.Info => LogLevel.Info,
            GDLogLevel.Warning => LogLevel.Warning,
            GDLogLevel.Error => LogLevel.Error,
            GDLogLevel.Silent => LogLevel.Silent,
            _ => LogLevel.Info
        };
        return Logger.IsEnabled(pluginLevel);
    }

    public void Verbose(string message) => Logger.Verbose($"[Semantics] {message}");

    public void Debug(string message) => Logger.Debug($"[Semantics] {message}");

    public void Info(string message) => Logger.Info($"[Semantics] {message}");

    public void Warning(string message) => Logger.Warning($"[Semantics] {message}");

    public void Error(string message) => Logger.Error($"[Semantics] {message}");

    public void Error(string message, Exception ex) => Logger.Error($"[Semantics] {message}", ex);
}
