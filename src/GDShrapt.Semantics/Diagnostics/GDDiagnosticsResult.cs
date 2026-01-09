using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of running diagnostics on a script file.
/// Contains combined results from validator and linter.
/// </summary>
public class GDDiagnosticsResult
{
    /// <summary>
    /// All diagnostics found in the file.
    /// </summary>
    public List<GDUnifiedDiagnostic> Diagnostics { get; } = new();

    /// <summary>
    /// Number of errors.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Number of warnings.
    /// </summary>
    public int WarningCount { get; private set; }

    /// <summary>
    /// Number of hints and info messages.
    /// </summary>
    public int HintCount { get; private set; }

    /// <summary>
    /// Whether the file has any errors.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>
    /// Whether the file has any diagnostics at all.
    /// </summary>
    public bool HasDiagnostics => Diagnostics.Count > 0;

    /// <summary>
    /// Adds a diagnostic to the result.
    /// </summary>
    public void Add(GDUnifiedDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        UpdateCount(diagnostic.Severity);
    }

    /// <summary>
    /// Adds multiple diagnostics to the result.
    /// </summary>
    public void AddRange(IEnumerable<GDUnifiedDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Add(diagnostic);
        }
    }

    private void UpdateCount(GDDiagnosticSeverity severity)
    {
        switch (severity)
        {
            case GDDiagnosticSeverity.Error:
                ErrorCount++;
                break;
            case GDDiagnosticSeverity.Warning:
                WarningCount++;
                break;
            case GDDiagnosticSeverity.Info:
            case GDDiagnosticSeverity.Hint:
                HintCount++;
                break;
        }
    }
}

/// <summary>
/// A unified diagnostic that can come from validator, linter, or syntax errors.
/// </summary>
public class GDUnifiedDiagnostic
{
    /// <summary>
    /// Rule/diagnostic code (e.g., "GD0001", "GD5001", "GDL001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level.
    /// </summary>
    public GDDiagnosticSeverity Severity { get; set; }

    /// <summary>
    /// Source of the diagnostic.
    /// </summary>
    public GDDiagnosticSource Source { get; set; }

    /// <summary>
    /// Start line (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Start column (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End line (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// End column (1-based).
    /// </summary>
    public int EndColumn { get; set; }
}

/// <summary>
/// Source of a diagnostic.
/// </summary>
public enum GDDiagnosticSource
{
    /// <summary>
    /// Syntax error (invalid token, parse failure).
    /// </summary>
    Syntax,

    /// <summary>
    /// Validation error (scope, types, calls, control flow).
    /// </summary>
    Validator,

    /// <summary>
    /// Lint issue (naming, style, best practices).
    /// </summary>
    Linter
}
