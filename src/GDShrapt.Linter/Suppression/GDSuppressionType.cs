namespace GDShrapt.Linter
{
    /// <summary>
    /// Type of suppression directive in a comment.
    /// </summary>
    public enum GDSuppressionType
    {
        /// <summary>
        /// Ignore rules for the next line only.
        /// Syntax: # gdlint:ignore = rule1, rule2
        /// </summary>
        Ignore,

        /// <summary>
        /// Disable rules from this point until Enable or end of file.
        /// Syntax: # gdlint: disable=rule1, rule2
        /// </summary>
        Disable,

        /// <summary>
        /// Re-enable previously disabled rules.
        /// Syntax: # gdlint: enable=rule1, rule2
        /// </summary>
        Enable,

        /// <summary>
        /// Ignore rules for the entire file.
        /// Syntax: # gdlint:ignore-file or # gdlint:ignore-file = rule1, rule2
        /// Must appear at the top of the file (first 10 lines).
        /// </summary>
        IgnoreFile,

        /// <summary>
        /// Ignore rules for the entire function that follows.
        /// Syntax: # gdlint:ignore-function or # gdlint:ignore-function = rule1, rule2
        /// Must appear immediately before a func declaration.
        /// </summary>
        IgnoreFunction,

        /// <summary>
        /// Ignore rules from this point to end of file.
        /// Syntax: # gdlint:ignore-below or # gdlint:ignore-below = rule1, rule2
        /// </summary>
        IgnoreBelow
    }
}
