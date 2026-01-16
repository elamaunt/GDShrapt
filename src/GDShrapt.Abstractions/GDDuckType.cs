using System.Collections.Generic;

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
    public Dictionary<string, string?> RequiredProperties { get; } = new Dictionary<string, string?>();

    /// <summary>
    /// Signals that this duck type must have.
    /// </summary>
    public HashSet<string> RequiredSignals { get; } = new HashSet<string>();

    /// <summary>
    /// Known base types this could be (from 'is' checks).
    /// </summary>
    public HashSet<string> PossibleTypes { get; } = new HashSet<string>();

    /// <summary>
    /// Types that this is definitely NOT (from failed 'is' checks in else branches).
    /// </summary>
    public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();

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
    public void RequireProperty(string name, string? type = null)
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
    /// Adds a possible type (from 'is' check).
    /// </summary>
    public void AddPossibleType(string typeName)
    {
        PossibleTypes.Add(typeName);
    }

    /// <summary>
    /// Excludes a type (from else branch of 'is' check).
    /// </summary>
    public void ExcludeType(string typeName)
    {
        ExcludedTypes.Add(typeName);
    }

    /// <summary>
    /// Checks if this duck type has any requirements defined.
    /// </summary>
    public bool HasRequirements =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        RequiredSignals.Count > 0;

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
            parts.Add($"is:{string.Join("|", PossibleTypes)}");
        if (RequiredMethods.Count > 0)
            parts.Add($"methods:{string.Join(",", RequiredMethods.Keys)}");
        if (RequiredProperties.Count > 0)
            parts.Add($"props:{string.Join(",", RequiredProperties.Keys)}");
        if (RequiredSignals.Count > 0)
            parts.Add($"signals:{string.Join(",", RequiredSignals)}");

        return parts.Count > 0 ? $"DuckType({string.Join(" ", parts)})" : "DuckType(any)";
    }
}
