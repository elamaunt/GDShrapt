using System;
using System.Threading.Tasks;
using GDShrapt.Abstractions;

namespace GDShrapt.LSP;

/// <summary>
/// Sends structured log messages to the LSP client via window/logMessage
/// and implements IGDLogger for universal logging across all layers.
/// </summary>
public class GDLspLogger : IGDLogger
{
    private readonly IGDJsonRpcTransport _transport;
    private readonly string? _contextPrefix;

    public GDLspLogger(IGDJsonRpcTransport transport, string? contextPrefix = null)
    {
        _transport = transport;
        _contextPrefix = contextPrefix;
    }

    public GDLogLevel MinLevel { get; set; } = GDLogLevel.Info;

    public bool IsEnabled(GDLogLevel level) => level >= MinLevel;

    public void Verbose(string message) => LogSync(GDLogLevel.Verbose, message);
    public void Debug(string message) => LogSync(GDLogLevel.Debug, message);
    public void Info(string message) => LogSync(GDLogLevel.Info, message);
    public void Warning(string message) => LogSync(GDLogLevel.Warning, message);
    public void Error(string message) => LogSync(GDLogLevel.Error, message);

    public void Error(string message, Exception ex)
    {
        LogSync(GDLogLevel.Error, $"{message}: {ex.Message}");
        GDLspPerformanceTrace.Log("error", ex.ToString());
    }

    public IGDLogger WithContext(string context)
    {
        var newPrefix = string.IsNullOrEmpty(_contextPrefix)
            ? context
            : $"{_contextPrefix}/{context}";

        return new GDLspLogger(_transport, newPrefix) { MinLevel = MinLevel };
    }

    /// <summary>
    /// Sends a log message notification to the client (async).
    /// </summary>
    public Task LogAsync(GDLspMessageType type, string message)
    {
        return _transport.SendNotificationAsync("window/logMessage", new GDLogMessageParams
        {
            Type = type,
            Message = message
        });
    }

    public Task ErrorAsync(string message) => LogAsync(GDLspMessageType.Error, message);
    public Task WarningAsync(string message) => LogAsync(GDLspMessageType.Warning, message);
    public Task InfoAsync(string message) => LogAsync(GDLspMessageType.Info, message);
    public Task DebugAsync(string message) => LogAsync(GDLspMessageType.Log, message);

    private void LogSync(GDLogLevel level, string message)
    {
        if (!IsEnabled(level))
            return;

        var formatted = string.IsNullOrEmpty(_contextPrefix)
            ? message
            : $"[{_contextPrefix}] {message}";

        _ = LogAsync(MapToLspType(level), formatted);
        GDLspPerformanceTrace.Log(MapToTag(level), formatted);
    }

    private static GDLspMessageType MapToLspType(GDLogLevel level) => level switch
    {
        GDLogLevel.Error => GDLspMessageType.Error,
        GDLogLevel.Warning => GDLspMessageType.Warning,
        GDLogLevel.Info => GDLspMessageType.Info,
        _ => GDLspMessageType.Log
    };

    private static string MapToTag(GDLogLevel level) => level switch
    {
        GDLogLevel.Error => "error",
        GDLogLevel.Warning => "warning",
        GDLogLevel.Info => "info",
        GDLogLevel.Debug => "debug",
        GDLogLevel.Verbose => "verbose",
        _ => "log"
    };
}
