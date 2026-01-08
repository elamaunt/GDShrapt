using System;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Static entry point for accessing GDShrapt plugin functionality.
/// Other plugins can reference this through NuGet.
/// </summary>
public static class GDShraptApi
{
    private static IGDShraptServices? _services;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the current instance of GDShrapt services.
    /// Returns null if the plugin is not initialized.
    /// </summary>
    public static IGDShraptServices? Services => _services;

    /// <summary>
    /// Checks if the plugin is initialized and ready to use.
    /// </summary>
    public static bool IsInitialized => _services != null;

    /// <summary>
    /// Event fired when the plugin is initialized.
    /// </summary>
    public static event Action? Initialized;

    /// <summary>
    /// Event fired when the plugin is being disposed.
    /// </summary>
    public static event Action? Disposing;

    /// <summary>
    /// Called by GDShraptPlugin on initialization.
    /// </summary>
    internal static void Initialize(IGDShraptServices services)
    {
        lock (_lock)
        {
            _services = services;
        }
        Initialized?.Invoke();
    }

    /// <summary>
    /// Called by GDShraptPlugin on exit.
    /// </summary>
    internal static void Shutdown()
    {
        Disposing?.Invoke();
        lock (_lock)
        {
            _services = null;
        }
    }
}
