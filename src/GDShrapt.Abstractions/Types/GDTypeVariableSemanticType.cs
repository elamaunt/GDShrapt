using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a type variable with constraints, used in generic Callable signatures.
/// For example, in Callable&lt;T: int | float&gt;(T) -> T, T is a type variable
/// with constraint "int | float".
/// </summary>
public class GDTypeVariableSemanticType : GDSemanticType
{
    /// <summary>
    /// Name of the type variable (e.g., "T", "T1", "T2").
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Constraint on this type variable — union of allowed types.
    /// Null means no constraint (any type).
    /// </summary>
    public GDSemanticType? Constraint { get; }

    /// <summary>
    /// Gets the display name for use as a parameter type (just the variable name).
    /// </summary>
    public override string DisplayName => VariableName;

    /// <summary>
    /// Gets the full constraint display for use in generic parameter lists.
    /// </summary>
    public string ConstraintDisplay =>
        Constraint != null ? $"{VariableName}: {Constraint.DisplayName}" : VariableName;

    public GDTypeVariableSemanticType(string variableName, GDSemanticType? constraint = null)
    {
        VariableName = variableName;
        Constraint = constraint;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        if (other.IsVariant)
            return true;

        if (other is GDTypeVariableSemanticType otherVar && otherVar.VariableName == VariableName)
            return true;

        if (Constraint != null)
            return Constraint.IsAssignableTo(other, provider);

        return false;
    }

    public override GDTypeNode? ToTypeNode() => null;

    public override bool Equals(object? obj)
    {
        if (obj is not GDTypeVariableSemanticType other)
            return false;
        return VariableName == other.VariableName && Equals(Constraint, other.Constraint);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (VariableName?.GetHashCode() ?? 0);
            hash = hash * 31 + (Constraint?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
