namespace GDShrapt.Semantics;

/// <summary>
/// Centralized rules for GDScript implicit type conversions.
/// Eliminates duplicated compatibility checks across the codebase.
/// </summary>
internal static class GDTypeCompatibility
{
    /// <summary>
    /// Checks if a source type can be implicitly converted to a target type in GDScript.
    /// Covers: int→float promotion, String↔StringName interoperability.
    /// </summary>
    public static bool IsImplicitlyConvertible(string source, string target)
    {
        if (source == GDWellKnownTypes.Numeric.Int && target == GDWellKnownTypes.Numeric.Float)
            return true;

        if ((source == GDWellKnownTypes.Strings.String && target == GDWellKnownTypes.Strings.StringName) ||
            (source == GDWellKnownTypes.Strings.StringName && target == GDWellKnownTypes.Strings.String))
            return true;

        return false;
    }

    /// <summary>
    /// Resolves the common type when mixing int and float in expressions.
    /// </summary>
    public static string? ResolveNumericPromotion(string type1, string type2)
    {
        if ((type1 == GDWellKnownTypes.Numeric.Int && type2 == GDWellKnownTypes.Numeric.Float) ||
            (type1 == GDWellKnownTypes.Numeric.Float && type2 == GDWellKnownTypes.Numeric.Int))
            return GDWellKnownTypes.Numeric.Float;

        return null;
    }
}
