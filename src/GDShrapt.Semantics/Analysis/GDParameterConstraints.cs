using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Constraints collected from parameter usage within a method body.
/// Used for duck typing inference - if a parameter is used like a certain type,
/// we can infer it likely IS that type.
/// </summary>
public class GDParameterConstraints
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Methods called on this parameter (e.g., data.get() adds "get" here).
    /// </summary>
    public HashSet<string> RequiredMethods { get; } = new();

    /// <summary>
    /// Properties accessed on this parameter (e.g., player.health adds "health" here).
    /// </summary>
    public HashSet<string> RequiredProperties { get; } = new();

    /// <summary>
    /// Whether the parameter is used as an iterable (e.g., for x in param).
    /// </summary>
    public bool IsIterable { get; private set; }

    /// <summary>
    /// Whether the parameter is used with indexer (e.g., param[0]).
    /// </summary>
    public bool IsIndexable { get; private set; }

    /// <summary>
    /// Tracks where this parameter is passed to other methods (for cross-method inference).
    /// Each entry contains the call expression and the argument index.
    /// </summary>
    public List<(GDCallExpression Call, int ArgIndex)> PassedToCalls { get; } = new();

    /// <summary>
    /// Possible types from 'is' type checks (e.g., if param is Node adds "Node").
    /// </summary>
    public HashSet<string> PossibleTypes { get; } = new();

    /// <summary>
    /// Types excluded by negative 'is' checks (e.g., if not param is Node adds "Node").
    /// </summary>
    public HashSet<string> ExcludedTypes { get; } = new();

    /// <summary>
    /// Creates a new constraints container for a parameter.
    /// </summary>
    public GDParameterConstraints(string name)
    {
        ParameterName = name;
    }

    /// <summary>
    /// Adds a method that must exist on this parameter's type.
    /// </summary>
    public void AddRequiredMethod(string name) => RequiredMethods.Add(name);

    /// <summary>
    /// Adds a property that must exist on this parameter's type.
    /// </summary>
    public void AddRequiredProperty(string name) => RequiredProperties.Add(name);

    /// <summary>
    /// Marks this parameter as being used as an iterable.
    /// </summary>
    public void AddIterableConstraint() => IsIterable = true;

    /// <summary>
    /// Marks this parameter as being used with indexer access.
    /// </summary>
    public void AddIndexableConstraint() => IsIndexable = true;

    /// <summary>
    /// Records that this parameter is passed to another method call.
    /// </summary>
    public void AddPassedToCall(GDCallExpression call, int argIndex)
        => PassedToCalls.Add((call, argIndex));

    /// <summary>
    /// Adds a type that this parameter could be (from 'is' check).
    /// </summary>
    public void AddPossibleType(string type) => PossibleTypes.Add(type);

    /// <summary>
    /// Adds a type that this parameter cannot be (from negative 'is' check).
    /// </summary>
    public void ExcludeType(string type) => ExcludedTypes.Add(type);

    /// <summary>
    /// Returns true if any constraints have been collected.
    /// </summary>
    public bool HasConstraints =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        IsIterable ||
        IsIndexable ||
        PassedToCalls.Count > 0 ||
        PossibleTypes.Count > 0;

    /// <summary>
    /// Returns a string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (RequiredMethods.Count > 0)
            parts.Add($"methods: [{string.Join(", ", RequiredMethods)}]");
        if (RequiredProperties.Count > 0)
            parts.Add($"props: [{string.Join(", ", RequiredProperties)}]");
        if (IsIterable)
            parts.Add("iterable");
        if (IsIndexable)
            parts.Add("indexable");
        if (PossibleTypes.Count > 0)
            parts.Add($"maybe: [{string.Join("|", PossibleTypes)}]");
        if (ExcludedTypes.Count > 0)
            parts.Add($"not: [{string.Join("|", ExcludedTypes)}]");
        if (PassedToCalls.Count > 0)
            parts.Add($"passed:{PassedToCalls.Count}x");

        return parts.Count > 0
            ? $"{ParameterName}({string.Join(", ", parts)})"
            : $"{ParameterName}(no constraints)";
    }
}
