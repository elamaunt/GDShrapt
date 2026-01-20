namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI overrides for validation checks. Null values mean "use default".
/// </summary>
public class GDValidationCheckOverrides
{
    /// <summary>
    /// Enable/disable syntax checking.
    /// </summary>
    public bool? CheckSyntax { get; set; }

    /// <summary>
    /// Enable/disable scope checking.
    /// </summary>
    public bool? CheckScope { get; set; }

    /// <summary>
    /// Enable/disable type checking.
    /// </summary>
    public bool? CheckTypes { get; set; }

    /// <summary>
    /// Enable/disable call checking.
    /// </summary>
    public bool? CheckCalls { get; set; }

    /// <summary>
    /// Enable/disable control flow checking.
    /// </summary>
    public bool? CheckControlFlow { get; set; }

    /// <summary>
    /// Enable/disable indentation checking.
    /// </summary>
    public bool? CheckIndentation { get; set; }

    /// <summary>
    /// Enable/disable member access checking on typed/untyped expressions (GD7xxx).
    /// </summary>
    public bool? CheckMemberAccess { get; set; }

    /// <summary>
    /// Enable/disable @abstract annotation checking (GD8xxx).
    /// </summary>
    public bool? CheckAbstract { get; set; }

    /// <summary>
    /// Enable/disable signal operation validation.
    /// </summary>
    public bool? CheckSignals { get; set; }

    /// <summary>
    /// Enable/disable resource path validation in load/preload calls.
    /// </summary>
    public bool? CheckResourcePaths { get; set; }

    /// <summary>
    /// Applies overrides to the given validation checks flags.
    /// </summary>
    public GDValidationChecks ApplyTo(GDValidationChecks checks)
    {
        var result = checks;

        if (CheckSyntax.HasValue)
        {
            if (CheckSyntax.Value)
                result |= GDValidationChecks.Syntax;
            else
                result &= ~GDValidationChecks.Syntax;
        }

        if (CheckScope.HasValue)
        {
            if (CheckScope.Value)
                result |= GDValidationChecks.Scope;
            else
                result &= ~GDValidationChecks.Scope;
        }

        if (CheckTypes.HasValue)
        {
            if (CheckTypes.Value)
                result |= GDValidationChecks.Types;
            else
                result &= ~GDValidationChecks.Types;
        }

        if (CheckCalls.HasValue)
        {
            if (CheckCalls.Value)
                result |= GDValidationChecks.Calls;
            else
                result &= ~GDValidationChecks.Calls;
        }

        if (CheckControlFlow.HasValue)
        {
            if (CheckControlFlow.Value)
                result |= GDValidationChecks.ControlFlow;
            else
                result &= ~GDValidationChecks.ControlFlow;
        }

        if (CheckIndentation.HasValue)
        {
            if (CheckIndentation.Value)
                result |= GDValidationChecks.Indentation;
            else
                result &= ~GDValidationChecks.Indentation;
        }

        if (CheckMemberAccess.HasValue)
        {
            if (CheckMemberAccess.Value)
                result |= GDValidationChecks.MemberAccess;
            else
                result &= ~GDValidationChecks.MemberAccess;
        }

        if (CheckAbstract.HasValue)
        {
            if (CheckAbstract.Value)
                result |= GDValidationChecks.Abstract;
            else
                result &= ~GDValidationChecks.Abstract;
        }

        if (CheckSignals.HasValue)
        {
            if (CheckSignals.Value)
                result |= GDValidationChecks.Signals;
            else
                result &= ~GDValidationChecks.Signals;
        }

        if (CheckResourcePaths.HasValue)
        {
            if (CheckResourcePaths.Value)
                result |= GDValidationChecks.ResourcePaths;
            else
                result &= ~GDValidationChecks.ResourcePaths;
        }

        return result;
    }
}
