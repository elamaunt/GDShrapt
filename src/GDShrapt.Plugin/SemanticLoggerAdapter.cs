using GDShrapt.Abstractions;

namespace GDShrapt.Plugin;

/// <summary>
/// Adapter that bridges the Plugin's static Logger to the IGDSemanticLogger interface
/// used by GDShrapt.Semantics components.
/// This allows semantic analysis logging to appear in the Plugin's Output dock.
/// </summary>
internal sealed class SemanticLoggerAdapter : IGDSemanticLogger
{
    /// <summary>
    /// Singleton instance for use throughout the plugin.
    /// </summary>
    public static SemanticLoggerAdapter Instance { get; } = new SemanticLoggerAdapter();

    private SemanticLoggerAdapter() { }

    public void Debug(string message) => Logger.Debug($"[Semantics] {message}");

    public void Info(string message) => Logger.Info($"[Semantics] {message}");

    public void Warning(string message) => Logger.Warning($"[Semantics] {message}");

    public void Error(string message) => Logger.Error($"[Semantics] {message}");
}
