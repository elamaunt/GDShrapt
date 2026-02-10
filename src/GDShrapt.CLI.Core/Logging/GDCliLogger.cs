using System;
using System.IO;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI-optimized logger with colored output and proper stderr/stdout separation.
/// </summary>
public sealed class GDCliLogger : IGDLogger
{
    private readonly TextWriter _output;
    private readonly TextWriter _errorOutput;
    private readonly bool _useColors;
    private readonly bool _showTimestamps;
    private readonly string? _contextPrefix;

    /// <summary>
    /// Current minimum log level. Messages below this level are ignored.
    /// </summary>
    public GDLogLevel MinLevel { get; set; }

    /// <summary>
    /// Creates a new CLI logger.
    /// </summary>
    /// <param name="output">Output stream for Info/Debug/Verbose (default: stdout).</param>
    /// <param name="errorOutput">Output stream for Warning/Error (default: stderr).</param>
    /// <param name="minLevel">Minimum log level (default: Info).</param>
    /// <param name="useColors">Enable colored output (default: true, disabled if redirected).</param>
    /// <param name="showTimestamps">Show timestamps in output (default: false).</param>
    /// <param name="contextPrefix">Optional context prefix for log messages.</param>
    public GDCliLogger(
        TextWriter? output = null,
        TextWriter? errorOutput = null,
        GDLogLevel minLevel = GDLogLevel.Info,
        bool useColors = true,
        bool showTimestamps = false,
        string? contextPrefix = null)
    {
        _output = output ?? Console.Out;
        _errorOutput = errorOutput ?? Console.Error;
        MinLevel = minLevel;
        _useColors = useColors && !Console.IsOutputRedirected && !Console.IsErrorRedirected;
        _showTimestamps = showTimestamps;
        _contextPrefix = contextPrefix;
    }

    /// <summary>
    /// Checks if a log level is enabled.
    /// </summary>
    public bool IsEnabled(GDLogLevel level) => level >= MinLevel;

    public void Verbose(string message) => Log(GDLogLevel.Verbose, message);
    public void Debug(string message) => Log(GDLogLevel.Debug, message);
    public void Info(string message) => Log(GDLogLevel.Info, message);
    public void Warning(string message) => Log(GDLogLevel.Warning, message);
    public void Error(string message) => Log(GDLogLevel.Error, message);
    public void Error(string message, Exception ex) => Log(GDLogLevel.Error, $"{message}: {ex.Message}");

    private void Log(GDLogLevel level, string message)
    {
        if (level < MinLevel)
            return;

        var output = level >= GDLogLevel.Warning ? _errorOutput : _output;
        var prefix = FormatPrefix(level);
        var timestamp = _showTimestamps ? $"{DateTime.Now:HH:mm:ss.fff} " : "";
        var context = string.IsNullOrEmpty(_contextPrefix) ? "" : $"[{_contextPrefix}] ";

        if (_useColors)
        {
            WriteWithColor(output, $"{timestamp}{prefix}{context}{message}", GetColor(level));
        }
        else
        {
            output.WriteLine($"{timestamp}{prefix}{context}{message}");
        }
    }

    private static void WriteWithColor(TextWriter output, string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            output.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private static string FormatPrefix(GDLogLevel level) => level switch
    {
        GDLogLevel.Verbose => "[VRB] ",
        GDLogLevel.Debug => "[DBG] ",
        GDLogLevel.Info => "",
        GDLogLevel.Warning => "warning: ",
        GDLogLevel.Error => "error: ",
        _ => ""
    };

    private static ConsoleColor GetColor(GDLogLevel level) => level switch
    {
        GDLogLevel.Verbose => ConsoleColor.DarkGray,
        GDLogLevel.Debug => ConsoleColor.Gray,
        GDLogLevel.Info => ConsoleColor.White,
        GDLogLevel.Warning => ConsoleColor.Yellow,
        GDLogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    /// <summary>
    /// Creates a child logger with an additional context prefix.
    /// </summary>
    /// <param name="context">Context name to add (e.g., "Parser", "Validator").</param>
    /// <returns>New logger with combined context prefix.</returns>
    public GDCliLogger WithContext(string context)
    {
        var newPrefix = string.IsNullOrEmpty(_contextPrefix)
            ? context
            : $"{_contextPrefix}/{context}";

        return new GDCliLogger(
            _output,
            _errorOutput,
            MinLevel,
            _useColors,
            _showTimestamps,
            newPrefix);
    }

    /// <summary>
    /// Creates a logger from CLI verbosity flags.
    /// </summary>
    /// <param name="quiet">Quiet mode (errors only).</param>
    /// <param name="verbose">Verbose mode (includes debug).</param>
    /// <param name="debug">Debug mode (includes timestamps).</param>
    /// <returns>Configured CLI logger.</returns>
    public static GDCliLogger FromFlags(bool quiet = false, bool verbose = false, bool debug = false, string? logLevel = null)
    {
        // --log-level takes priority if specified
        if (logLevel != null)
        {
            var (lvl, ts) = logLevel.ToLowerInvariant() switch
            {
                "silent" => (GDLogLevel.Silent, false),
                "error" => (GDLogLevel.Error, false),
                "warning" => (GDLogLevel.Warning, false),
                "info" => (GDLogLevel.Info, false),
                "debug" => (GDLogLevel.Debug, true),
                "verbose" => (GDLogLevel.Verbose, false),
                _ => (GDLogLevel.Info, false)
            };
            return new GDCliLogger(minLevel: lvl, showTimestamps: ts);
        }

        // Shorthand flags (priority: quiet > debug > verbose)
        GDLogLevel level;
        bool showTimestamps;

        if (quiet)
        {
            level = GDLogLevel.Error;
            showTimestamps = false;
        }
        else if (debug)
        {
            level = GDLogLevel.Debug;
            showTimestamps = true;
        }
        else if (verbose)
        {
            level = GDLogLevel.Verbose;
            showTimestamps = false;
        }
        else
        {
            level = GDLogLevel.Info;
            showTimestamps = false;
        }

        return new GDCliLogger(minLevel: level, showTimestamps: showTimestamps);
    }
}
