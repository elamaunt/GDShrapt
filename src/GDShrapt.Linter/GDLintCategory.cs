namespace GDShrapt.Reader
{
    /// <summary>
    /// Categories for lint rules.
    /// </summary>
    public enum GDLintCategory
    {
        /// <summary>
        /// Naming convention rules (snake_case, PascalCase, etc.)
        /// </summary>
        Naming,

        /// <summary>
        /// Code style rules (formatting, spacing, line length)
        /// </summary>
        Style,

        /// <summary>
        /// Best practices and code quality rules
        /// </summary>
        BestPractices,

        /// <summary>
        /// Code organization and structure rules
        /// </summary>
        Organization,

        /// <summary>
        /// Documentation and comments rules
        /// </summary>
        Documentation,

        /// <summary>
        /// Complexity metrics rules (max methods, max nesting, etc.)
        /// </summary>
        Complexity
    }
}
