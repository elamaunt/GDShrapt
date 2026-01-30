using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a Callable type with optional return type and parameter information.
/// Used for lambdas and function references.
/// </summary>
public class GDCallableSemanticType : GDSemanticType
{
    /// <summary>
    /// Gets the return type of this callable, if known.
    /// </summary>
    public GDSemanticType? ReturnType { get; }

    /// <summary>
    /// Gets the parameter types of this callable, if known.
    /// </summary>
    public IReadOnlyList<GDSemanticType>? ParameterTypes { get; }

    /// <summary>
    /// Gets whether the parameter count is variable (varargs).
    /// </summary>
    public bool IsVarArgs { get; }

    public override string DisplayName
    {
        get
        {
            if (ReturnType == null && ParameterTypes == null)
                return "Callable";

            var paramsStr = ParameterTypes != null
                ? $"({string.Join(", ", ParameterTypes.Select(p => p.DisplayName))})"
                : "()";

            var returnStr = ReturnType != null
                ? $" -> {ReturnType.DisplayName}"
                : "";

            return $"Callable{paramsStr}{returnStr}";
        }
    }

    public GDCallableSemanticType(
        GDSemanticType? returnType = null,
        IReadOnlyList<GDSemanticType>? parameterTypes = null,
        bool isVarArgs = false)
    {
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
        IsVarArgs = isVarArgs;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        // Anything is assignable to Variant
        if (other.IsVariant)
            return true;

        // Callable is assignable to Callable
        if (other is GDCallableSemanticType otherCallable)
        {
            // If target has no constraints, accept
            if (otherCallable.ReturnType == null && otherCallable.ParameterTypes == null)
                return true;

            // If this callable has no type info, can only assign to untyped callable
            if (ReturnType == null && ParameterTypes == null)
                return otherCallable.ReturnType == null && otherCallable.ParameterTypes == null;

            // Check return type compatibility (covariant)
            if (otherCallable.ReturnType != null && ReturnType != null)
            {
                if (!ReturnType.IsAssignableTo(otherCallable.ReturnType, provider))
                    return false;
            }

            // Check parameter compatibility (contravariant)
            if (otherCallable.ParameterTypes != null && ParameterTypes != null)
            {
                if (ParameterTypes.Count != otherCallable.ParameterTypes.Count && !IsVarArgs && !otherCallable.IsVarArgs)
                    return false;

                for (int i = 0; i < Math.Min(ParameterTypes.Count, otherCallable.ParameterTypes.Count); i++)
                {
                    // Parameters are contravariant: target param must be assignable to source param
                    if (!otherCallable.ParameterTypes[i].IsAssignableTo(ParameterTypes[i], provider))
                        return false;
                }
            }

            return true;
        }

        // Check if other is simple type "Callable"
        if (other is GDSimpleSemanticType simple && simple.TypeName == "Callable")
            return true;

        return false;
    }

    public override GDTypeNode? ToTypeNode()
    {
        // Callable with type info cannot be represented as simple GDTypeNode
        // Only basic "Callable" can be expressed
        if (ReturnType == null && ParameterTypes == null)
            return null; // Would need to construct GDTypeNode for "Callable"

        return null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GDCallableSemanticType other)
            return false;

        if (!Equals(ReturnType, other.ReturnType))
            return false;

        if (ParameterTypes == null && other.ParameterTypes == null)
            return true;

        if (ParameterTypes == null || other.ParameterTypes == null)
            return false;

        if (ParameterTypes.Count != other.ParameterTypes.Count)
            return false;

        return ParameterTypes.Zip(other.ParameterTypes, (a, b) => a.Equals(b)).All(x => x);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ReturnType);
        if (ParameterTypes != null)
        {
            foreach (var param in ParameterTypes)
                hash.Add(param);
        }
        return hash.ToHashCode();
    }
}
