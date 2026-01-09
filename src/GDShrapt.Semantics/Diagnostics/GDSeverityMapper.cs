namespace GDShrapt.Semantics;

/// <summary>
/// Maps between different severity types used across GDShrapt components.
/// Provides unified severity for CLI, LSP, and Plugin.
/// </summary>
public static class GDSeverityMapper
{
    /// <summary>
    /// Maps validator severity (from GDShrapt.Validator) to unified severity.
    /// </summary>
    /// <param name="severity">Validator diagnostic severity.</param>
    /// <returns>Unified severity.</returns>
    public static GDDiagnosticSeverity FromValidator(Reader.GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            Reader.GDDiagnosticSeverity.Error => GDDiagnosticSeverity.Error,
            Reader.GDDiagnosticSeverity.Warning => GDDiagnosticSeverity.Warning,
            Reader.GDDiagnosticSeverity.Hint => GDDiagnosticSeverity.Hint,
            _ => GDDiagnosticSeverity.Info
        };
    }

    /// <summary>
    /// Maps linter severity (from GDShrapt.Linter) to unified severity.
    /// </summary>
    /// <param name="severity">Linter issue severity.</param>
    /// <returns>Unified severity.</returns>
    public static GDDiagnosticSeverity FromLinter(Reader.GDLintSeverity severity)
    {
        return severity switch
        {
            Reader.GDLintSeverity.Error => GDDiagnosticSeverity.Error,
            Reader.GDLintSeverity.Warning => GDDiagnosticSeverity.Warning,
            Reader.GDLintSeverity.Info => GDDiagnosticSeverity.Info,
            Reader.GDLintSeverity.Hint => GDDiagnosticSeverity.Hint,
            _ => GDDiagnosticSeverity.Info
        };
    }

    /// <summary>
    /// Applies configured severity override if present.
    /// </summary>
    /// <param name="configuredSeverity">Configured severity override (nullable).</param>
    /// <param name="defaultSeverity">Default severity to use if no override.</param>
    /// <returns>Final severity to use.</returns>
    public static GDDiagnosticSeverity ApplyOverride(
        GDDiagnosticSeverity? configuredSeverity,
        GDDiagnosticSeverity defaultSeverity)
    {
        return configuredSeverity ?? defaultSeverity;
    }

    /// <summary>
    /// Converts unified severity to validator severity.
    /// </summary>
    public static Reader.GDDiagnosticSeverity ToValidator(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => Reader.GDDiagnosticSeverity.Error,
            GDDiagnosticSeverity.Warning => Reader.GDDiagnosticSeverity.Warning,
            GDDiagnosticSeverity.Hint => Reader.GDDiagnosticSeverity.Hint,
            GDDiagnosticSeverity.Info => Reader.GDDiagnosticSeverity.Hint, // No Info in validator
            _ => Reader.GDDiagnosticSeverity.Hint
        };
    }

    /// <summary>
    /// Converts unified severity to linter severity.
    /// </summary>
    public static Reader.GDLintSeverity ToLinter(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => Reader.GDLintSeverity.Error,
            GDDiagnosticSeverity.Warning => Reader.GDLintSeverity.Warning,
            GDDiagnosticSeverity.Info => Reader.GDLintSeverity.Info,
            GDDiagnosticSeverity.Hint => Reader.GDLintSeverity.Hint,
            _ => Reader.GDLintSeverity.Info
        };
    }

    /// <summary>
    /// Converts linter severity directly to CLI output severity index.
    /// </summary>
    /// <remarks>
    /// Returns int to avoid circular dependency with CLI.Core.
    /// Maps to: 0=Error, 1=Warning, 2=Information, 3=Hint
    /// </remarks>
    public static int ToCliSeverityIndex(Reader.GDLintSeverity severity)
    {
        return severity switch
        {
            Reader.GDLintSeverity.Error => 0,
            Reader.GDLintSeverity.Warning => 1,
            Reader.GDLintSeverity.Info => 2,
            Reader.GDLintSeverity.Hint => 3,
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
    public static int ToCliSeverityIndex(Reader.GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            Reader.GDDiagnosticSeverity.Error => 0,
            Reader.GDDiagnosticSeverity.Warning => 1,
            Reader.GDDiagnosticSeverity.Hint => 3,
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
    public static int ToCliSeverityIndex(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => 0,
            GDDiagnosticSeverity.Warning => 1,
            GDDiagnosticSeverity.Info => 2,
            GDDiagnosticSeverity.Hint => 3,
            _ => 2
        };
    }
}
