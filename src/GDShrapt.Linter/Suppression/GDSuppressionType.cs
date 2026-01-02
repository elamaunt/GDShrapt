namespace GDShrapt.Reader
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
        Enable
    }
}
