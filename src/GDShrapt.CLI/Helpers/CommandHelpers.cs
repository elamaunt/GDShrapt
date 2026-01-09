using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Common helper methods for CLI commands.
/// </summary>
public static class CommandHelpers
{
    /// <summary>
    /// Gets the appropriate output formatter based on the format string.
    /// </summary>
    public static IGDOutputFormatter GetFormatter(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new GDJsonFormatter(),
            "text" => new GDTextFormatter(),
            _ => new GDTextFormatter()
        };
    }

    /// <summary>
    /// Gets the parse output format from a string.
    /// </summary>
    public static GDParseOutputFormat ParseOutputFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "tree" => GDParseOutputFormat.Tree,
            "json" => GDParseOutputFormat.Json,
            "tokens" => GDParseOutputFormat.Tokens,
            _ => GDParseOutputFormat.Tree
        };
    }

    /// <summary>
    /// Gets the extract style output format from a string.
    /// </summary>
    public static GDExtractStyleOutputFormat ParseStyleOutputFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "toml" => GDExtractStyleOutputFormat.Toml,
            "json" => GDExtractStyleOutputFormat.Json,
            "text" => GDExtractStyleOutputFormat.Text,
            _ => GDExtractStyleOutputFormat.Toml
        };
    }
}
