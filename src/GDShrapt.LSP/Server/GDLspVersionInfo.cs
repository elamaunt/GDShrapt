using System.Reflection;

namespace GDShrapt.LSP;

/// <summary>
/// Provides the LSP server version from assembly metadata.
/// </summary>
internal static class GDLspVersionInfo
{
    /// <summary>
    /// Gets the server version string from assembly informational version.
    /// </summary>
    public static string GetVersion()
    {
        var assembly = typeof(GDLspVersionInfo).Assembly;

        var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attr != null && !string.IsNullOrEmpty(attr.InformationalVersion))
            return attr.InformationalVersion;

        var version = assembly.GetName().Version;
        if (version != null)
            return version.ToString();

        return "unknown";
    }
}
