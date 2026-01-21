using System.IO;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code formatting operations.
/// Wraps GDFormatter.
/// </summary>
public class GDFormatHandler : IGDFormatHandler
{
    /// <inheritdoc />
    public virtual string? Format(string filePath, GDFormatterConfig? options = null)
    {
        if (!File.Exists(filePath))
            return null;

        var code = File.ReadAllText(filePath);
        return FormatCode(code, options);
    }

    /// <inheritdoc />
    public virtual string? FormatCode(string code, GDFormatterConfig? options = null)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var formatterOptions = options != null
            ? CreateFormatterOptions(options)
            : GDFormatterOptions.Default;

        var formatter = new GDFormatter(formatterOptions);
        return formatter.FormatCode(code);
    }

    /// <inheritdoc />
    public virtual bool NeedsFormatting(string filePath, GDFormatterConfig? options = null)
    {
        if (!File.Exists(filePath))
            return false;

        var original = File.ReadAllText(filePath);
        var formatterOptions = options != null
            ? CreateFormatterOptions(options)
            : GDFormatterOptions.Default;

        var formatter = new GDFormatter(formatterOptions);
        return !formatter.IsFormatted(original);
    }

    private static GDFormatterOptions CreateFormatterOptions(GDFormatterConfig config)
    {
        var options = new GDFormatterOptions
        {
            IndentSize = config.IndentSize,
            IndentStyle = config.IndentStyle == Semantics.GDIndentationStyle.Tabs ? IndentStyle.Tabs : IndentStyle.Spaces,
            BlankLinesBetweenFunctions = config.BlankLinesBetweenFunctions,
            SpaceAroundOperators = config.SpaceAroundOperators,
            SpaceAfterComma = config.SpaceAfterComma,
            SpaceAfterColon = config.SpaceAfterColon
        };

        return options;
    }
}
