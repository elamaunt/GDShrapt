using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a duck type - a type defined by its capabilities rather than its name.
/// Used when we know what methods/properties an object has without knowing the exact type.
/// Data-only class - resolution logic is in GDDuckTypeResolver (Semantics).
/// </summary>
public class GDDuckType
{
    /// <summary>
    /// Methods that this duck type must have.
    /// Key = method name, Value = observed parameter count (or -1 if unknown).
    /// </summary>
    public Dictionary<string, int> RequiredMethods { get; } = new Dictionary<string, int>();

    /// <summary>
    /// Properties that this duck type must have.
    /// Key = property name, Value = observed type (or null if unknown).
    /// </summary>
    public Dictionary<string, GDSemanticType?> RequiredProperties { get; } = new Dictionary<string, GDSemanticType?>();

    /// <summary>
    /// Signals that this duck type must have.
    /// </summary>
    public HashSet<string> RequiredSignals { get; } = new HashSet<string>();

    /// <summary>
    /// Operators that this duck type must support.
    /// Key = operator type, Value = observed operand types from the other side of the operator.
    /// </summary>
    public Dictionary<GDDualOperatorType, List<GDSemanticType>> RequiredOperators { get; }
        = new Dictionary<GDDualOperatorType, List<GDSemanticType>>();

    /// <summary>
    /// Known base types this could be (from 'is' checks).
    /// </summary>
    public HashSet<GDSemanticType> PossibleTypes { get; } = new HashSet<GDSemanticType>();

    /// <summary>
    /// Types that this is definitely NOT (from failed 'is' checks in else branches).
    /// </summary>
    public HashSet<GDSemanticType> ExcludedTypes { get; } = new HashSet<GDSemanticType>();

    /// <summary>
    /// Whether this type has been validated (e.g., via is_valid() for Callable).
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Whether this type may be null (for nullable type tracking).
    /// </summary>
    public bool MayBeNull { get; set; } = true;

    /// <summary>
    /// The concrete type, if known exactly.
    /// </summary>
    public GDSemanticType? ConcreteType { get; set; }

    /// <summary>
    /// Adds a method requirement.
    /// </summary>
    public void RequireMethod(string name, int paramCount = -1)
    {
        RequiredMethods[name] = paramCount;
    }

    /// <summary>
    /// Adds a property requirement.
    /// </summary>
    public void RequireProperty(string name, GDSemanticType? type = null)
    {
        RequiredProperties[name] = type;
    }

    /// <summary>
    /// Adds a signal requirement.
    /// </summary>
    public void RequireSignal(string name)
    {
        RequiredSignals.Add(name);
    }

    /// <summary>
    /// Adds an operator requirement with optional operand type.
    /// </summary>
    public void RequireOperator(GDDualOperatorType op, GDSemanticType? operandType = null)
    {
        if (!RequiredOperators.TryGetValue(op, out var operands))
        {
            operands = new List<GDSemanticType>();
            RequiredOperators[op] = operands;
        }
        if (operandType != null && !operands.Contains(operandType))
            operands.Add(operandType);
    }

    /// <summary>
    /// Adds a possible type (from 'is' check).
    /// </summary>
    public void AddPossibleType(GDSemanticType type)
    {
        PossibleTypes.Add(type);
    }

    /// <summary>
    /// Adds a possible type by name. Convenience method for AST boundary.
    /// </summary>
    public void AddPossibleTypeName(string typeName)
    {
        PossibleTypes.Add(GDSemanticType.FromRuntimeTypeName(typeName));
    }

    /// <summary>
    /// Excludes a type (from else branch of 'is' check).
    /// </summary>
    public void ExcludeType(GDSemanticType type)
    {
        ExcludedTypes.Add(type);
    }

    /// <summary>
    /// Excludes a type by name. Convenience method for AST boundary.
    /// </summary>
    public void ExcludeTypeName(string typeName)
    {
        ExcludedTypes.Add(GDSemanticType.FromRuntimeTypeName(typeName));
    }

    /// <summary>
    /// Checks if this duck type has any requirements defined.
    /// </summary>
    public bool HasRequirements =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        RequiredSignals.Count > 0 ||
        RequiredOperators.Count > 0;

    /// <summary>
    /// Merges another duck type into this one.
    /// </summary>
    public void MergeWith(GDDuckType? other)
    {
        if (other == null)
            return;

        foreach (var kv in other.RequiredMethods)
        {
            if (!RequiredMethods.ContainsKey(kv.Key))
                RequiredMethods[kv.Key] = kv.Value;
        }
        foreach (var kv in other.RequiredProperties)
        {
            if (!RequiredProperties.ContainsKey(kv.Key))
                RequiredProperties[kv.Key] = kv.Value;
        }
        foreach (var s in other.RequiredSignals)
            RequiredSignals.Add(s);
        foreach (var kv in other.RequiredOperators)
        {
            if (!RequiredOperators.TryGetValue(kv.Key, out var operands))
            {
                operands = new List<GDSemanticType>();
                RequiredOperators[kv.Key] = operands;
            }
            foreach (var op in kv.Value)
            {
                if (!operands.Contains(op))
                    operands.Add(op);
            }
        }
        foreach (var t in other.PossibleTypes)
            PossibleTypes.Add(t);
        foreach (var t in other.ExcludedTypes)
            ExcludedTypes.Add(t);
    }

    /// <summary>
    /// Creates intersection of possible types (for type narrowing).
    /// </summary>
    public GDDuckType IntersectWith(GDDuckType? other)
    {
        if (other == null)
            return this;

        var result = new GDDuckType();

        // Merge all requirements
        foreach (var kv in RequiredMethods)
            result.RequiredMethods[kv.Key] = kv.Value;
        foreach (var kv in other.RequiredMethods)
        {
            if (!result.RequiredMethods.ContainsKey(kv.Key))
                result.RequiredMethods[kv.Key] = kv.Value;
        }

        foreach (var kv in RequiredProperties)
            result.RequiredProperties[kv.Key] = kv.Value;
        foreach (var kv in other.RequiredProperties)
        {
            if (!result.RequiredProperties.ContainsKey(kv.Key))
                result.RequiredProperties[kv.Key] = kv.Value;
        }

        foreach (var s in RequiredSignals)
            result.RequiredSignals.Add(s);
        foreach (var s in other.RequiredSignals)
            result.RequiredSignals.Add(s);

        // Merge operator requirements
        foreach (var kv in RequiredOperators)
        {
            var operands = new List<GDSemanticType>(kv.Value);
            result.RequiredOperators[kv.Key] = operands;
        }
        foreach (var kv in other.RequiredOperators)
        {
            if (!result.RequiredOperators.TryGetValue(kv.Key, out var operands))
            {
                operands = new List<GDSemanticType>();
                result.RequiredOperators[kv.Key] = operands;
            }
            foreach (var op in kv.Value)
            {
                if (!operands.Contains(op))
                    operands.Add(op);
            }
        }

        // Intersect possible types
        if (PossibleTypes.Count > 0 && other.PossibleTypes.Count > 0)
        {
            foreach (var t in PossibleTypes)
            {
                if (other.PossibleTypes.Contains(t))
                    result.PossibleTypes.Add(t);
            }
        }
        else if (PossibleTypes.Count > 0)
        {
            foreach (var t in PossibleTypes)
                result.PossibleTypes.Add(t);
        }
        else if (other.PossibleTypes.Count > 0)
        {
            foreach (var t in other.PossibleTypes)
                result.PossibleTypes.Add(t);
        }

        // Union excluded types
        foreach (var t in ExcludedTypes)
            result.ExcludedTypes.Add(t);
        foreach (var t in other.ExcludedTypes)
            result.ExcludedTypes.Add(t);

        return result;
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (PossibleTypes.Count > 0)
            parts.Add($"is:{string.Join("|", PossibleTypes.Select(t => t.DisplayName))}");
        if (RequiredMethods.Count > 0)
            parts.Add($"methods:{string.Join(",", RequiredMethods.Keys)}");
        if (RequiredOperators.Count > 0)
            parts.Add($"operators:{string.Join(",", RequiredOperators.Keys)}");
        if (RequiredProperties.Count > 0)
            parts.Add($"props:{string.Join(",", RequiredProperties.Keys)}");
        if (RequiredSignals.Count > 0)
            parts.Add($"signals:{string.Join(",", RequiredSignals)}");

        return parts.Count > 0 ? $"DuckType({string.Join(" ", parts)})" : "DuckType(any)";
    }
}
