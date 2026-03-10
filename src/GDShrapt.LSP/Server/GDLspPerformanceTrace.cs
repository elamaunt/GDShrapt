using System;
using System.Diagnostics;
using System.IO;

namespace GDShrapt.LSP;

/// <summary>
/// File-based performance trace for diagnosing LSP hangs.
/// Writes timestamped entries to %TEMP%/gdshrapt-lsp-trace.log.
/// </summary>
internal static class GDLspPerformanceTrace
{
    private static readonly string _logPath;
    private static readonly object _lock = new();
    private static readonly Stopwatch _uptime = Stopwatch.StartNew();

    static GDLspPerformanceTrace()
    {
        _logPath = Path.Combine(Path.GetTempPath(), "gdshrapt-lsp-trace.log");
        // Clear old log on startup
        try { File.WriteAllText(_logPath, $"=== GDShrapt LSP Trace started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"); }
        catch { /* best effort */ }
    }

    public static string LogPath => _logPath;

    public static void Log(string tag, string message)
    {
        var line = $"[{_uptime.Elapsed:mm\\:ss\\.fff}] [{tag}] {message}";
        lock (_lock)
        {
            try { File.AppendAllText(_logPath, line + "\n"); }
            catch { /* best effort */ }
        }
    }

    public static void LogSlow(string tag, long elapsedMs, string details)
    {
        Log(tag, $"{elapsedMs}ms — {details}");
    }
}
