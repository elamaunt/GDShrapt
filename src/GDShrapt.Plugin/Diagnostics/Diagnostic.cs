using GDShrapt.Semantics;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a single diagnostic (error, warning, hint) for a script.
/// </summary>
internal class Diagnostic
{
    /// <summary>
    /// Unique rule identifier (e.g., "GDS001").
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable message describing the issue.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity level of the diagnostic.
    /// </summary>
    public GDDiagnosticSeverity Severity { get; init; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Category for grouping and filtering.
    /// </summary>
    public DiagnosticCategory Category { get; init; } = DiagnosticCategory.Style;

    /// <summary>
    /// Reference to the script containing the issue.
    /// </summary>
    public ScriptReference? Script { get; init; }

    /// <summary>
    /// Start line (0-based).
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// End line (0-based). If same as StartLine, issue is on single line.
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// End column (0-based).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// Available code fixes for this diagnostic.
    /// </summary>
    public IReadOnlyList<CodeFix> Fixes { get; init; } = Array.Empty<CodeFix>();

    /// <summary>
    /// Source text that triggered the diagnostic (for display).
    /// </summary>
    public string? SourceText { get; init; }

    /// <summary>
    /// URL to documentation for this rule.
    /// </summary>
    public string? HelpUrl { get; init; }

    /// <summary>
    /// Creates a diagnostic builder for fluent construction.
    /// </summary>
    public static DiagnosticBuilder Create(string ruleId, string message) => new(ruleId, message);

    public override string ToString()
    {
        return $"[{RuleId}] {Severity}: {Message} at line {StartLine + 1}";
    }
}

/// <summary>
/// Builder for creating diagnostics with fluent API.
/// </summary>
internal class DiagnosticBuilder
{
    private readonly string _ruleId;
    private readonly string _message;
    private GDDiagnosticSeverity _severity = GDDiagnosticSeverity.Warning;
    private DiagnosticCategory _category = DiagnosticCategory.Style;
    private ScriptReference? _script;
    private int _startLine;
    private int _startColumn;
    private int _endLine;
    private int _endColumn;
    private List<CodeFix>? _fixes;
    private string? _sourceText;
    private string? _helpUrl;

    public DiagnosticBuilder(string ruleId, string message)
    {
        _ruleId = ruleId;
        _message = message;
    }

    public DiagnosticBuilder WithSeverity(GDDiagnosticSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public DiagnosticBuilder WithCategory(DiagnosticCategory category)
    {
        _category = category;
        return this;
    }

    public DiagnosticBuilder AtScript(ScriptReference script)
    {
        _script = script;
        return this;
    }

    public DiagnosticBuilder AtLocation(int line, int column)
    {
        _startLine = line;
        _startColumn = column;
        _endLine = line;
        _endColumn = column;
        return this;
    }

    public DiagnosticBuilder AtSpan(int startLine, int startColumn, int endLine, int endColumn)
    {
        _startLine = startLine;
        _startColumn = startColumn;
        _endLine = endLine;
        _endColumn = endColumn;
        return this;
    }

    public DiagnosticBuilder WithFix(CodeFix fix)
    {
        _fixes ??= new List<CodeFix>();
        _fixes.Add(fix);
        return this;
    }

    public DiagnosticBuilder WithFix(string title, Func<string, string> apply)
    {
        return WithFix(new CodeFix(title, apply));
    }

    public DiagnosticBuilder WithSourceText(string sourceText)
    {
        _sourceText = sourceText;
        return this;
    }

    public DiagnosticBuilder WithHelpUrl(string url)
    {
        _helpUrl = url;
        return this;
    }

    public Diagnostic Build()
    {
        return new Diagnostic
        {
            RuleId = _ruleId,
            Message = _message,
            Severity = _severity,
            Category = _category,
            Script = _script,
            StartLine = _startLine,
            StartColumn = _startColumn,
            EndLine = _endLine,
            EndColumn = _endColumn,
            Fixes = _fixes?.ToArray() ?? Array.Empty<CodeFix>(),
            SourceText = _sourceText,
            HelpUrl = _helpUrl
        };
    }
}

/// <summary>
/// Represents a code fix that can be applied to resolve a diagnostic.
/// </summary>
internal class CodeFix
{
    /// <summary>
    /// Title displayed to the user (e.g., "Remove trailing whitespace").
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Function that transforms the original code to fixed code.
    /// Input: original source, Output: fixed source.
    /// </summary>
    public Func<string, string> Apply { get; }

    /// <summary>
    /// Optional: specific text replacement instead of full-file transform.
    /// </summary>
    public TextReplacement? Replacement { get; init; }

    public CodeFix(string title, Func<string, string> apply)
    {
        Title = title;
        Apply = apply;
    }

    public CodeFix(string title, TextReplacement replacement)
    {
        Title = title;
        Replacement = replacement;
        Apply = source =>
        {
            // Apply the specific replacement
            var lines = source.Split('\n');
            if (replacement.Line < lines.Length)
            {
                var line = lines[replacement.Line];
                if (replacement.StartColumn <= line.Length)
                {
                    var endCol = Math.Min(replacement.EndColumn, line.Length);
                    lines[replacement.Line] =
                        line.Substring(0, replacement.StartColumn) +
                        replacement.NewText +
                        line.Substring(endCol);
                }
            }
            return string.Join("\n", lines);
        };
    }
}

/// <summary>
/// Represents a specific text replacement in a file.
/// </summary>
internal class TextReplacement
{
    /// <summary>
    /// Line number (0-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// End column (0-based).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// Text to insert in place of the range.
    /// </summary>
    public string NewText { get; init; } = "";
}

/// <summary>
/// Summary of diagnostics for display in notification panel.
/// </summary>
internal class DiagnosticSummary
{
    /// <summary>
    /// Total error count.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Total warning count.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Total hint/info count.
    /// </summary>
    public int HintCount { get; init; }

    /// <summary>
    /// Total diagnostic count.
    /// </summary>
    public int TotalCount => ErrorCount + WarningCount + HintCount;

    /// <summary>
    /// Whether there are any issues.
    /// </summary>
    public bool HasIssues => TotalCount > 0;

    /// <summary>
    /// Number of files with issues.
    /// </summary>
    public int AffectedFileCount { get; init; }

    /// <summary>
    /// Whether there are formatting issues that can be auto-fixed.
    /// </summary>
    public bool HasFormattingIssues { get; init; }

    /// <summary>
    /// Creates an empty summary.
    /// </summary>
    public static DiagnosticSummary Empty => new();

    public override string ToString()
    {
        if (!HasIssues)
            return "No issues";

        var parts = new List<string>();
        if (ErrorCount > 0) parts.Add($"{ErrorCount} error(s)");
        if (WarningCount > 0) parts.Add($"{WarningCount} warning(s)");
        if (HintCount > 0) parts.Add($"{HintCount} hint(s)");

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Event args for diagnostic changes.
/// </summary>
internal class DiagnosticsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Script that was analyzed.
    /// </summary>
    public ScriptReference Script { get; init; } = null!;

    /// <summary>
    /// New diagnostics for the script.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

    /// <summary>
    /// Summary of all diagnostics.
    /// </summary>
    public DiagnosticSummary Summary { get; init; } = DiagnosticSummary.Empty;
}

/// <summary>
/// Event args for project-wide analysis completion.
/// </summary>
internal class ProjectAnalysisCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Summary of all project diagnostics.
    /// </summary>
    public DiagnosticSummary Summary { get; init; } = DiagnosticSummary.Empty;

    /// <summary>
    /// Time taken for analysis.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of files analyzed.
    /// </summary>
    public int FilesAnalyzed { get; init; }
}
