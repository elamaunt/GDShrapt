namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI exit codes for GDShrapt commands.
/// Provides detailed exit status for CI/CD pipelines.
/// </summary>
public static class GDExitCode
{
    /// <summary>
    /// Success - no issues found.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Warnings or hints found (when fail-on is configured).
    /// </summary>
    public const int WarningsOrHints = 1;

    /// <summary>
    /// Errors found in the codebase.
    /// </summary>
    public const int Errors = 2;

    /// <summary>
    /// Fatal error - project not found, configuration error, or exception.
    /// </summary>
    public const int Fatal = 3;

    /// <summary>
    /// Determines the exit code based on analysis results and configuration.
    /// </summary>
    /// <param name="errorCount">Number of errors found.</param>
    /// <param name="warningCount">Number of warnings found.</param>
    /// <param name="hintCount">Number of hints/info found.</param>
    /// <param name="failOnWarning">Whether to fail on warnings.</param>
    /// <param name="failOnHint">Whether to fail on hints.</param>
    /// <returns>Appropriate exit code.</returns>
    public static int FromResults(
        int errorCount,
        int warningCount,
        int hintCount,
        bool failOnWarning = false,
        bool failOnHint = false)
    {
        if (errorCount > 0)
            return Errors;

        if (failOnWarning && warningCount > 0)
            return WarningsOrHints;

        if (failOnHint && hintCount > 0)
            return WarningsOrHints;

        return Success;
    }

    /// <summary>
    /// Gets a human-readable description of the exit code.
    /// </summary>
    public static string GetDescription(int exitCode)
    {
        return exitCode switch
        {
            Success => "Success - no issues found",
            WarningsOrHints => "Warnings or hints found (fail threshold reached)",
            Errors => "Errors found in codebase",
            Fatal => "Fatal error (project not found or exception)",
            _ => $"Unknown exit code: {exitCode}"
        };
    }
}

/// <summary>
/// Specifies when the CLI should fail (return non-zero exit code).
/// </summary>
public enum GDFailOnLevel
{
    /// <summary>
    /// Only fail on errors.
    /// </summary>
    Error,

    /// <summary>
    /// Fail on errors or warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// Fail on errors, warnings, or hints.
    /// </summary>
    Hint
}
