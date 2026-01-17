using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Maps between different severity types used across GDShrapt components.
/// Provides unified severity for CLI, LSP, and Plugin.
/// </summary>
public static class GDSeverityMapper
{
    /// <summary>
    /// Maps validator severity (from GDShrapt.Reader) to unified severity.
    /// </summary>
    /// <param name="severity">Validator diagnostic severity.</param>
    /// <returns>Unified severity.</returns>
    public static GDUnifiedDiagnosticSeverity FromValidator(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => GDUnifiedDiagnosticSeverity.Error,
            GDDiagnosticSeverity.Warning => GDUnifiedDiagnosticSeverity.Warning,
            GDDiagnosticSeverity.Hint => GDUnifiedDiagnosticSeverity.Hint,
            _ => GDUnifiedDiagnosticSeverity.Info
        };
    }

    /// <summary>
    /// Maps linter severity (from GDShrapt.Linter) to unified severity.
    /// </summary>
    /// <param name="severity">Linter issue severity.</param>
    /// <returns>Unified severity.</returns>
    public static GDUnifiedDiagnosticSeverity FromLinter(GDLintSeverity severity)
    {
        return severity switch
        {
            GDLintSeverity.Error => GDUnifiedDiagnosticSeverity.Error,
            GDLintSeverity.Warning => GDUnifiedDiagnosticSeverity.Warning,
            GDLintSeverity.Info => GDUnifiedDiagnosticSeverity.Info,
            GDLintSeverity.Hint => GDUnifiedDiagnosticSeverity.Hint,
            _ => GDUnifiedDiagnosticSeverity.Info
        };
    }

    /// <summary>
    /// Applies configured severity override if present.
    /// </summary>
    /// <param name="configuredSeverity">Configured severity override (nullable).</param>
    /// <param name="defaultSeverity">Default severity to use if no override.</param>
    /// <returns>Final severity to use.</returns>
    public static GDUnifiedDiagnosticSeverity ApplyOverride(
        GDUnifiedDiagnosticSeverity? configuredSeverity,
        GDUnifiedDiagnosticSeverity defaultSeverity)
    {
        return configuredSeverity ?? defaultSeverity;
    }

    /// <summary>
    /// Converts unified severity to validator severity.
    /// </summary>
    public static GDDiagnosticSeverity ToValidator(GDUnifiedDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDUnifiedDiagnosticSeverity.Error => GDDiagnosticSeverity.Error,
            GDUnifiedDiagnosticSeverity.Warning => GDDiagnosticSeverity.Warning,
            GDUnifiedDiagnosticSeverity.Hint => GDDiagnosticSeverity.Hint,
            GDUnifiedDiagnosticSeverity.Info => GDDiagnosticSeverity.Hint, // No Info in validator
            _ => GDDiagnosticSeverity.Hint
        };
    }

    /// <summary>
    /// Converts unified severity to linter severity.
    /// </summary>
    public static GDLintSeverity ToLinter(GDUnifiedDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDUnifiedDiagnosticSeverity.Error => GDLintSeverity.Error,
            GDUnifiedDiagnosticSeverity.Warning => GDLintSeverity.Warning,
            GDUnifiedDiagnosticSeverity.Info => GDLintSeverity.Info,
            GDUnifiedDiagnosticSeverity.Hint => GDLintSeverity.Hint,
            _ => GDLintSeverity.Info
        };
    }

    /// <summary>
    /// Converts linter severity directly to CLI output severity index.
    /// </summary>
    /// <remarks>
    /// Returns int to avoid circular dependency with CLI.Core.
    /// Maps to: 0=Error, 1=Warning, 2=Information, 3=Hint
    /// </remarks>
    public static int ToCliSeverityIndex(GDLintSeverity severity)
    {
        return severity switch
        {
            GDLintSeverity.Error => 0,
            GDLintSeverity.Warning => 1,
            GDLintSeverity.Info => 2,
            GDLintSeverity.Hint => 3,
            _ => 2
        };
    }

    /// <summary>
    /// Converts validator severity directly to CLI output severity index.
    /// </summary>
    /// <remarks>
    /// Returns int to avoid circular dependency with CLI.Core.
    /// Maps to: 0=Error, 1=Warning, 2=Information, 3=Hint
    /// </remarks>
    public static int ToCliSeverityIndex(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => 0,
            GDDiagnosticSeverity.Warning => 1,
            GDDiagnosticSeverity.Hint => 3,
            _ => 2
        };
    }

    /// <summary>
    /// Converts unified severity to CLI output severity index.
    /// </summary>
    /// <remarks>
    /// Returns int to avoid circular dependency with CLI.Core.
    /// Maps to: 0=Error, 1=Warning, 2=Information, 3=Hint
    /// </remarks>
    public static int ToCliSeverityIndex(GDUnifiedDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDUnifiedDiagnosticSeverity.Error => 0,
            GDUnifiedDiagnosticSeverity.Warning => 1,
            GDUnifiedDiagnosticSeverity.Info => 2,
            GDUnifiedDiagnosticSeverity.Hint => 3,
            _ => 2
        };
    }
}
