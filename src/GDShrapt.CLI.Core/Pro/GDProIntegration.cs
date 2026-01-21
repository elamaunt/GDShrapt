using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Static integration point for Pro features.
/// Uses reflection to discover Pro provider at startup if Pro module is linked.
/// </summary>
public static class GDProIntegration
{
    private static IGDProFeatureProvider? _provider;
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Name of the Pro provider type to discover via reflection.
    /// </summary>
    private const string ProProviderTypeName = "GDShrapt.Pro.Core.GDProFeatureProviderAdapter";

    /// <summary>
    /// Assembly name containing the Pro provider.
    /// </summary>
    private const string ProAssemblyName = "GDShrapt.Pro.Core";

    /// <summary>
    /// Gets the Pro feature provider if available.
    /// Returns null if Pro module is not linked.
    /// </summary>
    public static IGDProFeatureProvider? Provider
    {
        get
        {
            EnsureInitialized();
            return _provider;
        }
    }

    /// <summary>
    /// Whether Pro module is available (linked at runtime).
    /// </summary>
    public static bool IsProAvailable
    {
        get
        {
            EnsureInitialized();
            return _provider != null;
        }
    }

    /// <summary>
    /// Whether Pro is available AND licensed.
    /// </summary>
    public static bool IsProLicensed
    {
        get
        {
            EnsureInitialized();
            return _provider?.IsLicensed ?? false;
        }
    }

    /// <summary>
    /// Current license state.
    /// </summary>
    public static GDProLicenseState LicenseState
    {
        get
        {
            EnsureInitialized();
            return _provider?.LicenseState ?? GDProLicenseState.ProNotAvailable;
        }
    }

    /// <summary>
    /// Checks if a specific Pro feature is enabled.
    /// Returns false if Pro is not available or not licensed.
    /// </summary>
    public static bool IsFeatureEnabled(GDProFeatureKind feature)
    {
        EnsureInitialized();
        return _provider?.IsFeatureEnabled(feature) ?? false;
    }

    /// <summary>
    /// Attempts to use a Pro feature.
    /// Returns true if the feature is available, false otherwise.
    /// Writes a warning message if the feature is not available.
    /// </summary>
    /// <param name="feature">The feature to use.</param>
    /// <param name="output">Output writer for warnings (uses Console.Error if null).</param>
    /// <returns>True if feature can be used, false otherwise.</returns>
    public static bool TryUseFeature(GDProFeatureKind feature, TextWriter? output = null)
    {
        EnsureInitialized();

        if (_provider == null)
        {
            GDProWarnings.WriteProNotAvailable(feature, output ?? Console.Error);
            return false;
        }

        if (!_provider.IsLicensed)
        {
            GDProWarnings.WriteLicenseRequired(feature, _provider.LicenseState, output ?? Console.Error);
            return false;
        }

        if (!_provider.IsFeatureEnabled(feature))
        {
            GDProWarnings.WriteFeatureNotEnabled(feature, output ?? Console.Error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Registers a Pro feature provider explicitly.
    /// Called by Pro module during startup.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    public static void RegisterProvider(IGDProFeatureProvider provider)
    {
        lock (_lock)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _initialized = true;
        }
    }

    /// <summary>
    /// Clears the provider (for testing).
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _provider = null;
            _initialized = false;
        }
    }

    /// <summary>
    /// Forces initialization using reflection-based discovery.
    /// Called automatically on first access.
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            _provider = DiscoverProvider();
            _initialized = true;
        }
    }

    /// <summary>
    /// Discovers Pro provider via reflection.
    /// Looks for GDProFeatureProviderAdapter in GDShrapt.Pro.Core assembly.
    /// </summary>
    private static IGDProFeatureProvider? DiscoverProvider()
    {
        try
        {
            // Try to find the Pro.Core assembly in already loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var proAssembly = assemblies.FirstOrDefault(a =>
                a.GetName().Name == ProAssemblyName);

            if (proAssembly == null)
            {
                // Try to load it explicitly
                try
                {
                    proAssembly = Assembly.Load(ProAssemblyName);
                }
                catch
                {
                    // Pro assembly not available
                    return null;
                }
            }

            if (proAssembly == null) return null;

            // Find the adapter type
            var adapterType = proAssembly.GetType(ProProviderTypeName);
            if (adapterType == null) return null;

            // Check if it implements IGDProFeatureProvider
            if (!typeof(IGDProFeatureProvider).IsAssignableFrom(adapterType))
                return null;

            // Create instance (parameterless constructor)
            var instance = Activator.CreateInstance(adapterType);
            return instance as IGDProFeatureProvider;
        }
        catch
        {
            // Silently fail - Pro not available
            return null;
        }
    }
}
