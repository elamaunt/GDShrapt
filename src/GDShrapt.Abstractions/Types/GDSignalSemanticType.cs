using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a Signal type with optional parameter information.
/// Used for signal declarations with known parameter signatures.
/// </summary>
public class GDSignalSemanticType : GDSemanticType
{
    /// <summary>
    /// Gets the parameter types of this signal, if known.
    /// </summary>
    public IReadOnlyList<GDSemanticType>? ParameterTypes { get; }

    /// <summary>
    /// Gets the parameter names of this signal, if known.
    /// </summary>
    public IReadOnlyList<string?>? ParameterNames { get; }

    public override bool IsSignal => true;
    public override bool IsValueType => true;

    public override string DisplayName
    {
        get
        {
            if (ParameterTypes == null || ParameterTypes.Count == 0)
                return "Signal";

            var parts = new List<string>();
            for (int i = 0; i < ParameterTypes.Count; i++)
            {
                var name = ParameterNames != null && i < ParameterNames.Count
                    ? ParameterNames[i]
                    : null;
                var typeName = ParameterTypes[i].DisplayName;

                if (!string.IsNullOrEmpty(name))
                    parts.Add($"{name}: {typeName}");
                else
                    parts.Add(typeName);
            }

            return $"Signal({string.Join(", ", parts)})";
        }
    }

    public GDSignalSemanticType(
        IReadOnlyList<GDSemanticType>? parameterTypes = null,
        IReadOnlyList<string?>? parameterNames = null)
    {
        ParameterTypes = parameterTypes;
        ParameterNames = parameterNames;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        if (other.IsVariant)
            return true;

        if (other is GDSignalSemanticType otherSignal)
        {
            if (otherSignal.ParameterTypes == null || otherSignal.ParameterTypes.Count == 0)
                return true;

            if (ParameterTypes == null || ParameterTypes.Count == 0)
                return otherSignal.ParameterTypes == null || otherSignal.ParameterTypes.Count == 0;

            if (ParameterTypes.Count != otherSignal.ParameterTypes.Count)
                return false;

            for (int i = 0; i < ParameterTypes.Count; i++)
            {
                if (!ParameterTypes[i].IsAssignableTo(otherSignal.ParameterTypes[i], provider))
                    return false;
            }

            return true;
        }

        if (other is GDSimpleSemanticType simple && simple.TypeName == "Signal")
            return true;

        return false;
    }

    public override GDTypeNode? ToTypeNode() => null;

    public override bool Equals(object? obj)
    {
        if (obj is not GDSignalSemanticType other)
            return false;

        if (!SequenceEquals(ParameterTypes, other.ParameterTypes))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 19;
            if (ParameterTypes != null)
            {
                foreach (var param in ParameterTypes)
                    hash = hash * 31 + (param?.GetHashCode() ?? 0);
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
