using System;
using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Validator;

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

    /// <summary>
    /// Filters out suppressed diagnostics using the provided suppression context.
    /// </summary>
    public void FilterSuppressed(GDValidatorSuppressionContext suppressionContext)
    {
        if (suppressionContext == null)
            return;

        var filteredDiagnostics = new List<GDUnifiedDiagnostic>();
        int newErrorCount = 0;
        int newWarningCount = 0;
        int newHintCount = 0;

        foreach (var diagnostic in Diagnostics)
        {
            if (!suppressionContext.IsSuppressed(diagnostic.Code, diagnostic.StartLine))
            {
                filteredDiagnostics.Add(diagnostic);
                switch (diagnostic.Severity)
                {
                    case GDUnifiedDiagnosticSeverity.Error:
                        newErrorCount++;
                        break;
                    case GDUnifiedDiagnosticSeverity.Warning:
                        newWarningCount++;
                        break;
                    case GDUnifiedDiagnosticSeverity.Info:
                    case GDUnifiedDiagnosticSeverity.Hint:
                        newHintCount++;
                        break;
                }
            }
        }

        Diagnostics.Clear();
        Diagnostics.AddRange(filteredDiagnostics);
        ErrorCount = newErrorCount;
        WarningCount = newWarningCount;
        HintCount = newHintCount;
    }

    private void UpdateCount(GDUnifiedDiagnosticSeverity severity)
    {
        switch (severity)
        {
            case GDUnifiedDiagnosticSeverity.Error:
                ErrorCount++;
                break;
            case GDUnifiedDiagnosticSeverity.Warning:
                WarningCount++;
                break;
            case GDUnifiedDiagnosticSeverity.Info:
            case GDUnifiedDiagnosticSeverity.Hint:
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
    /// Human-readable rule name (e.g., "no-unused-variable").
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level.
    /// </summary>
    public GDUnifiedDiagnosticSeverity Severity { get; set; }

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

    /// <summary>
    /// Available code fix descriptors for this diagnostic.
    /// </summary>
    public IReadOnlyList<GDFixDescriptor> FixDescriptors { get; set; } = Array.Empty<GDFixDescriptor>();
}

/// <summary>
/// Unified diagnostic severity level.
/// </summary>
public enum GDUnifiedDiagnosticSeverity
{
    /// <summary>
    /// Compilation error that must be fixed.
    /// </summary>
    Error,

    /// <summary>
    /// Warning about potential issues.
    /// </summary>
    Warning,

    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Hint for improvement.
    /// </summary>
    Hint
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
    /// Semantic validation error (type checks, member access, argument types, indexers, signals, generics).
    /// </summary>
    SemanticValidator,

    /// <summary>
    /// Lint issue (naming, style, best practices).
    /// </summary>
    Linter
}
