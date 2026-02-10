namespace GDShrapt.CLI.Core;

/// <summary>
/// ANSI escape code helpers for terminal coloring.
/// Can be disabled globally for non-TTY or when --color=never.
/// </summary>
public static class GDAnsiColors
{
    public static bool Enabled { get; set; } = true;

    public static string Red(string text) => Enabled ? $"\x1b[31m{text}\x1b[0m" : text;
    public static string Yellow(string text) => Enabled ? $"\x1b[33m{text}\x1b[0m" : text;
    public static string Green(string text) => Enabled ? $"\x1b[32m{text}\x1b[0m" : text;
    public static string Cyan(string text) => Enabled ? $"\x1b[36m{text}\x1b[0m" : text;
    public static string Blue(string text) => Enabled ? $"\x1b[34m{text}\x1b[0m" : text;
    public static string Bold(string text) => Enabled ? $"\x1b[1m{text}\x1b[0m" : text;
    public static string Dim(string text) => Enabled ? $"\x1b[2m{text}\x1b[0m" : text;

    /// <summary>
    /// Configures color based on --color option value.
    /// auto = detect TTY, always = force on, never = force off.
    /// </summary>
    public static void Configure(string? colorMode)
    {
        switch (colorMode?.ToLowerInvariant())
        {
            case "always":
                Enabled = true;
                break;
            case "never":
                Enabled = false;
                break;
            case "auto":
            case null:
            default:
                Enabled = !System.Console.IsOutputRedirected;
                break;
        }
    }
}
