namespace GDShrapt.Validator;

/// <summary>
/// Type of suppression directive in a comment.
/// </summary>
public enum GDValidatorSuppressionType
{
    /// <summary>
    /// Ignore rules for the next line only.
    /// Syntax: # gd:ignore = GD1001, GD2001
    /// </summary>
    Ignore,

    /// <summary>
    /// Disable rules from this point until Enable or end of file.
    /// Syntax: # gd:disable = GD1001, GD2001
    /// </summary>
    Disable,

    /// <summary>
    /// Re-enable previously disabled rules.
    /// Syntax: # gd:enable = GD1001, GD2001
    /// </summary>
    Enable
}
