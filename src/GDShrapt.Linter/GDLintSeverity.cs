using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Severity level for lint issues.
    /// </summary>
    public enum GDLintSeverity
    {
        /// <summary>
        /// Informational hint, not a problem.
        /// </summary>
        Hint,

        /// <summary>
        /// Style suggestion that could be improved.
        /// </summary>
        Info,

        /// <summary>
        /// Potential issue or deviation from best practices.
        /// </summary>
        Warning,

        /// <summary>
        /// Definite problem that should be fixed.
        /// </summary>
        Error
    }
}
