using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a Callable type with optional return type and parameter information.
/// Used for lambdas, function references, and generic function signatures.
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

    /// <summary>
    /// Gets the type parameters for generic callable signatures.
    /// </summary>
    public IReadOnlyList<GDTypeVariableSemanticType>? TypeParameters { get; }

    public override string DisplayName
    {
        get
        {
            if (ReturnType == null && ParameterTypes == null && TypeParameters == null)
                return "Callable";

            var paramsStr = ParameterTypes != null
                ? string.Join(", ", ParameterTypes.Select(p => p.DisplayName))
                : "";

            if (IsVarArgs && !string.IsNullOrEmpty(paramsStr))
                paramsStr += "...";
            else if (IsVarArgs)
                paramsStr = "Variant...";

            var returnStr = ReturnType != null && !ReturnType.IsVariant
                ? $" -> {ReturnType.DisplayName}"
                : "";

            if (TypeParameters != null && TypeParameters.Count > 0)
            {
                var constraintStr = string.Join(", ",
                    TypeParameters.Select(tp => tp.ConstraintDisplay));
                return $"Callable<{constraintStr}>({paramsStr}){returnStr}";
            }

            return $"Callable({paramsStr}){returnStr}";
        }
    }

    public GDCallableSemanticType(
        GDSemanticType? returnType = null,
        IReadOnlyList<GDSemanticType>? parameterTypes = null,
        bool isVarArgs = false,
        IReadOnlyList<GDTypeVariableSemanticType>? typeParameters = null)
    {
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
        IsVarArgs = isVarArgs;
        TypeParameters = typeParameters;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        if (other.IsVariant)
            return true;

        if (other is GDCallableSemanticType otherCallable)
        {
            if (otherCallable.ReturnType == null && otherCallable.ParameterTypes == null)
                return true;

            if (ReturnType == null && ParameterTypes == null)
                return otherCallable.ReturnType == null && otherCallable.ParameterTypes == null;

            if (otherCallable.ReturnType != null && ReturnType != null)
            {
                if (!ReturnType.IsAssignableTo(otherCallable.ReturnType, provider))
                    return false;
            }

            if (otherCallable.ParameterTypes != null && ParameterTypes != null)
            {
                if (ParameterTypes.Count != otherCallable.ParameterTypes.Count && !IsVarArgs && !otherCallable.IsVarArgs)
                    return false;

                for (int i = 0; i < Math.Min(ParameterTypes.Count, otherCallable.ParameterTypes.Count); i++)
                {
                    if (!otherCallable.ParameterTypes[i].IsAssignableTo(ParameterTypes[i], provider))
                        return false;
                }
            }

            return true;
        }

        if (other is GDSimpleSemanticType simple && simple.TypeName == "Callable")
            return true;

        return false;
    }

    public override GDTypeNode? ToTypeNode()
    {
        if (ReturnType == null && ParameterTypes == null)
            return null;

        return null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GDCallableSemanticType other)
            return false;

        if (!Equals(ReturnType, other.ReturnType))
            return false;

        if (IsVarArgs != other.IsVarArgs)
            return false;

        if (!SequenceEquals(ParameterTypes, other.ParameterTypes))
            return false;

        if (!SequenceEquals(TypeParameters, other.TypeParameters))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (ReturnType?.GetHashCode() ?? 0);
            hash = hash * 31 + IsVarArgs.GetHashCode();
            if (ParameterTypes != null)
            {
                foreach (var param in ParameterTypes)
                    hash = hash * 31 + (param?.GetHashCode() ?? 0);
            }
            if (TypeParameters != null)
            {
                foreach (var tp in TypeParameters)
                    hash = hash * 31 + (tp?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private static bool SequenceEquals<T>(IReadOnlyList<T>? a, IReadOnlyList<T>? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!Equals(a[i], b[i])) return false;
        }
        return true;
    }
}
