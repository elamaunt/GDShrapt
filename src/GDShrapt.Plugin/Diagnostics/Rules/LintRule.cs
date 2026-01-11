using System.Text.RegularExpressions;

namespace GDShrapt.Plugin;

/// <summary>
/// Base class for lint rules with common functionality.
/// </summary>
internal abstract class LintRule : ILintRule
{
    public abstract string RuleId { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract DiagnosticCategory Category { get; }
    public virtual GDDiagnosticSeverity DefaultSeverity => GDDiagnosticSeverity.Warning;
    public virtual GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Light;

    public abstract IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig);

    /// <summary>
    /// Creates a diagnostic builder pre-configured with this rule's info.
    /// </summary>
    protected DiagnosticBuilder CreateDiagnostic(string message, ScriptReference? script = null)
    {
        var builder = Diagnostic.Create(RuleId, message)
            .WithSeverity(DefaultSeverity)
            .WithCategory(Category);

        if (script != null)
            builder.AtScript(script);

        return builder;
    }

    /// <summary>
    /// Splits content into lines while preserving line endings info.
    /// </summary>
    protected static string[] SplitLines(string content)
    {
        return content.Split('\n');
    }

    /// <summary>
    /// Gets the line ending used in the content.
    /// </summary>
    protected static string DetectLineEnding(string content)
    {
        if (content.Contains("\r\n"))
            return "\r\n";
        if (content.Contains("\n"))
            return "\n";
        return "\n";
    }

    /// <summary>
    /// Counts leading whitespace characters.
    /// </summary>
    protected static int CountLeadingWhitespace(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t')
                count++;
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// Gets the indentation string (tabs/spaces) from the start of a line.
    /// </summary>
    protected static string GetIndentation(string line)
    {
        int count = CountLeadingWhitespace(line);
        return line.Substring(0, count);
    }

    /// <summary>
    /// Checks if a line contains only whitespace.
    /// </summary>
    protected static bool IsBlankLine(string line)
    {
        return string.IsNullOrWhiteSpace(line);
    }

    /// <summary>
    /// Checks if a line is a comment.
    /// </summary>
    protected static bool IsComment(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("#");
    }

    /// <summary>
    /// Gets trailing whitespace from a line.
    /// </summary>
    protected static string GetTrailingWhitespace(string line)
    {
        var trimmed = line.TrimEnd();
        if (trimmed.Length < line.Length)
        {
            return line.Substring(trimmed.Length);
        }
        return string.Empty;
    }

    /// <summary>
    /// Checks if line contains code (not blank, not comment).
    /// </summary>
    protected static bool IsCodeLine(string line)
    {
        var trimmed = line.TrimStart();
        return !string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#");
    }

    /// <summary>
    /// Creates a fix that removes text at a specific location.
    /// </summary>
    protected static CodeFix CreateRemovalFix(string title, int line, int startCol, int endCol)
    {
        return new CodeFix(title, new TextReplacement
        {
            Line = line,
            StartColumn = startCol,
            EndColumn = endCol,
            NewText = ""
        });
    }

    /// <summary>
    /// Creates a fix that replaces text at a specific location.
    /// </summary>
    protected static CodeFix CreateReplacementFix(string title, int line, int startCol, int endCol, string newText)
    {
        return new CodeFix(title, new TextReplacement
        {
            Line = line,
            StartColumn = startCol,
            EndColumn = endCol,
            NewText = newText
        });
    }

    /// <summary>
    /// Creates a fix that transforms the entire file.
    /// </summary>
    protected static CodeFix CreateFileFix(string title, Func<string, string> transform)
    {
        return new CodeFix(title, transform);
    }
}

/// <summary>
/// Base class for formatting rules.
/// </summary>
internal abstract class FormattingRule : LintRule
{
    public override DiagnosticCategory Category => DiagnosticCategory.Formatting;
    public override GDDiagnosticSeverity DefaultSeverity => GDDiagnosticSeverity.Warning;
}

/// <summary>
/// Base class for style rules.
/// </summary>
internal abstract class StyleRule : LintRule
{
    public override DiagnosticCategory Category => DiagnosticCategory.Style;
    public override GDDiagnosticSeverity DefaultSeverity => GDDiagnosticSeverity.Hint;
    public override GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Off; // Style rules always run
}
