namespace GDShrapt.Semantics;

/// <summary>
/// Centralized constants for well-known GDScript built-in function names.
/// </summary>
internal static class GDWellKnownFunctions
{
    public const string Preload = "preload";
    public const string Load = "load";
    public const string Range = "range";
    public const string Constructor = "new";

    public static bool IsResourceLoader(string? name) => name is Preload or Load;
}
