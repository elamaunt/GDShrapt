using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code formatting operations.
/// </summary>
public interface IGDFormatHandler
{
    /// <summary>
    /// Formats a GDScript file.
    /// </summary>
    /// <param name="filePath">Path to the file to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>Formatted code, or null if formatting failed.</returns>
    string? Format(string filePath, GDFormatterConfig? options = null);

    /// <summary>
    /// Formats GDScript code.
    /// </summary>
    /// <param name="code">Code to format.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>Formatted code, or null if formatting failed.</returns>
    string? FormatCode(string code, GDFormatterConfig? options = null);

    /// <summary>
    /// Checks if a file needs formatting.
    /// </summary>
    /// <param name="filePath">Path to the file to check.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>True if the file would be changed by formatting.</returns>
    bool NeedsFormatting(string filePath, GDFormatterConfig? options = null);
}
