using System.Text.RegularExpressions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Converts Godot BBCode documentation format to Markdown for display in hover tooltips.
/// </summary>
public static class GDBBCodeToMarkdownConverter
{
    public static string Convert(string bbCode)
    {
        if (string.IsNullOrEmpty(bbCode))
            return string.Empty;

        var result = bbCode;

        // Codeblocks first (before inline conversions)
        result = Regex.Replace(result, @"\[codeblocks?\]", "```gdscript\n");
        result = Regex.Replace(result, @"\[/codeblocks?\]", "\n```");
        result = Regex.Replace(result, @"\[gdscript\]", "");
        result = Regex.Replace(result, @"\[/gdscript\]", "");
        result = Regex.Replace(result, @"\[csharp\].*?\[/csharp\]", "", RegexOptions.Singleline);

        // Inline code
        result = Regex.Replace(result, @"\[code\](.*?)\[/code\]", "`$1`");

        // Bold and italic
        result = Regex.Replace(result, @"\[b\](.*?)\[/b\]", "**$1**");
        result = Regex.Replace(result, @"\[i\](.*?)\[/i\]", "*$1*");

        // Godot-specific references
        result = Regex.Replace(result, @"\[param\s+(\w+)\]", "`$1`");
        result = Regex.Replace(result, @"\[method\s+([\w.]+)\]", "`$1()`");
        result = Regex.Replace(result, @"\[member\s+([\w.]+)\]", "`$1`");
        result = Regex.Replace(result, @"\[signal\s+([\w.]+)\]", "`$1`");
        result = Regex.Replace(result, @"\[enum\s+([\w.]+)\]", "`$1`");
        result = Regex.Replace(result, @"\[constant\s+([\w.]+)\]", "`$1`");
        result = Regex.Replace(result, @"\[annotation\s+([\w.@]+)\]", "`$1`");
        result = Regex.Replace(result, @"\[theme_item\s+([\w.]+)\]", "`$1`");

        // Type references like [Type] or [Type.Member]
        result = Regex.Replace(result, @"\[(\w+)\](?!\()", "`$1`");

        // URLs
        result = Regex.Replace(result, @"\[url=(.*?)\](.*?)\[/url\]", "[$2]($1)");
        result = Regex.Replace(result, @"\[url\](.*?)\[/url\]", "$1");

        // Keyboard shortcuts
        result = Regex.Replace(result, @"\[kbd\](.*?)\[/kbd\]", "`$1`");

        // Line break
        result = result.Replace("[br]", "\n");

        // Color tags — strip
        result = Regex.Replace(result, @"\[color=.*?\](.*?)\[/color\]", "$1");

        // Center/right alignment — strip
        result = Regex.Replace(result, @"\[/?center\]", "");
        result = Regex.Replace(result, @"\[/?right\]", "");

        // Strip any remaining unknown tags
        result = Regex.Replace(result, @"\[/?\w+[^\]]*\]", "");

        return result.Trim();
    }

    /// <summary>
    /// Strips all BBCode tags, returning plain text. Used for XML doc comments.
    /// </summary>
    public static string StripToPlainText(string bbCode)
    {
        if (string.IsNullOrEmpty(bbCode))
            return string.Empty;

        var result = bbCode;

        // Remove codeblock contents markers but keep text
        result = Regex.Replace(result, @"\[codeblocks?\]", "\n");
        result = Regex.Replace(result, @"\[/codeblocks?\]", "\n");
        result = Regex.Replace(result, @"\[gdscript\]", "");
        result = Regex.Replace(result, @"\[/gdscript\]", "");
        result = Regex.Replace(result, @"\[csharp\].*?\[/csharp\]", "", RegexOptions.Singleline);

        // Extract text from tags
        result = Regex.Replace(result, @"\[code\](.*?)\[/code\]", "$1");
        result = Regex.Replace(result, @"\[b\](.*?)\[/b\]", "$1");
        result = Regex.Replace(result, @"\[i\](.*?)\[/i\]", "$1");
        result = Regex.Replace(result, @"\[url=(.*?)\](.*?)\[/url\]", "$2");
        result = Regex.Replace(result, @"\[url\](.*?)\[/url\]", "$1");
        result = Regex.Replace(result, @"\[kbd\](.*?)\[/kbd\]", "$1");
        result = Regex.Replace(result, @"\[color=.*?\](.*?)\[/color\]", "$1");

        // Reference tags — extract name only
        result = Regex.Replace(result, @"\[param\s+(\w+)\]", "$1");
        result = Regex.Replace(result, @"\[method\s+([\w.]+)\]", "$1()");
        result = Regex.Replace(result, @"\[member\s+([\w.]+)\]", "$1");
        result = Regex.Replace(result, @"\[signal\s+([\w.]+)\]", "$1");
        result = Regex.Replace(result, @"\[enum\s+([\w.]+)\]", "$1");
        result = Regex.Replace(result, @"\[constant\s+([\w.]+)\]", "$1");
        result = Regex.Replace(result, @"\[annotation\s+([\w.@]+)\]", "$1");
        result = Regex.Replace(result, @"\[theme_item\s+([\w.]+)\]", "$1");

        result = result.Replace("[br]", "\n");

        // Strip all remaining tags
        result = Regex.Replace(result, @"\[/?\w+[^\]]*\]", "");

        return result.Trim();
    }
}
