namespace GDShrapt.Reader
{
    /// <summary>
    /// Severity level of a validation diagnostic.
    /// </summary>
    public enum GDDiagnosticSeverity
    {
        /// <summary>
        /// An error that prevents valid code execution.
        /// </summary>
        Error,

        /// <summary>
        /// A warning about potential issues.
        /// </summary>
        Warning,

        /// <summary>
        /// A hint or suggestion for improvement.
        /// </summary>
        Hint
    }
}
