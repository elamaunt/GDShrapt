using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Precise source location for editor navigation.
/// Used by Type Flow panel to navigate to specific tokens in the code.
/// </summary>
public class GDSourceLocation
{
    /// <summary>
    /// Absolute path to the script file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Start line (0-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End line (0-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// End column (0-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Creates a source location from an AST node.
    /// </summary>
    /// <param name="node">The AST node.</param>
    /// <param name="filePath">The file path containing the node.</param>
    /// <returns>A new GDSourceLocation instance, or null if node is null.</returns>
    public static GDSourceLocation? FromNode(GDNode? node, string? filePath)
    {
        if (node == null)
            return null;

        return new GDSourceLocation
        {
            FilePath = filePath,
            StartLine = node.StartLine,
            StartColumn = node.StartColumn,
            EndLine = node.EndLine,
            EndColumn = node.EndColumn
        };
    }

    /// <summary>
    /// Creates a source location for a specific position.
    /// </summary>
    public static GDSourceLocation FromPosition(string filePath, int line, int column)
    {
        return new GDSourceLocation
        {
            FilePath = filePath,
            StartLine = line,
            StartColumn = column,
            EndLine = line,
            EndColumn = column
        };
    }

    /// <summary>
    /// Checks if this location is valid (has a file path).
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(FilePath);

    public override string ToString()
    {
        return $"{System.IO.Path.GetFileName(FilePath ?? "")}:{StartLine + 1}:{StartColumn + 1}";
    }
}
