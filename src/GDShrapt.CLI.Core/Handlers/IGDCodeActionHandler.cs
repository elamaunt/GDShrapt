using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code actions (quick fixes, refactorings).
/// </summary>
public interface IGDCodeActionHandler
{
    /// <summary>
    /// Gets available code actions for a range in a file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="startLine">Start line of the range (1-based).</param>
    /// <param name="endLine">End line of the range (1-based).</param>
    /// <returns>List of available code actions.</returns>
    IReadOnlyList<GDCodeAction> GetCodeActions(string filePath, int startLine, int endLine);
}

/// <summary>
/// Represents a code action (quick fix, refactoring).
/// </summary>
public class GDCodeAction
{
    /// <summary>
    /// Display title for the action.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Kind of the action (QuickFix, Refactor, Source).
    /// </summary>
    public GDCodeActionKind Kind { get; init; }

    /// <summary>
    /// Whether this is the preferred fix for the diagnostic.
    /// </summary>
    public bool IsPreferred { get; init; }

    /// <summary>
    /// Text edits to apply.
    /// </summary>
    public IReadOnlyList<GDCodeActionEdit> Edits { get; init; } = [];

    /// <summary>
    /// Code of the diagnostic this action fixes.
    /// </summary>
    public string? DiagnosticCode { get; init; }
}

/// <summary>
/// Kind of code action.
/// </summary>
public enum GDCodeActionKind
{
    /// <summary>
    /// A quick fix for a diagnostic.
    /// </summary>
    QuickFix,

    /// <summary>
    /// A refactoring operation.
    /// </summary>
    Refactor,

    /// <summary>
    /// A source-level action (organize imports, etc.).
    /// </summary>
    Source
}

/// <summary>
/// Represents a text edit within a code action.
/// </summary>
public class GDCodeActionEdit
{
    /// <summary>
    /// Start line of the edit (1-based).
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Start column of the edit (1-based).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// End line of the edit (1-based).
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// End column of the edit (1-based).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// New text to insert.
    /// </summary>
    public required string NewText { get; init; }
}
