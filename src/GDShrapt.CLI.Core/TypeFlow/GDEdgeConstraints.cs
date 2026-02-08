using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Represents duck typing constraints attached to an edge.
/// Used to show what methods/properties are required.
/// </summary>
public class GDEdgeConstraints
{
    /// <summary>
    /// Methods required by this constraint.
    /// Key = method name, Value = parameter count.
    /// </summary>
    public Dictionary<string, int> RequiredMethods { get; } = new();

    /// <summary>
    /// Properties required by this constraint.
    /// Key = property name, Value = expected type (or null).
    /// </summary>
    public Dictionary<string, string> RequiredProperties { get; } = new();

    /// <summary>
    /// Signals required by this constraint.
    /// </summary>
    public HashSet<string> RequiredSignals { get; } = new();

    /// <summary>
    /// Creates constraints from a GDDuckType.
    /// </summary>
    public static GDEdgeConstraints? FromDuckType(GDDuckType? duckType)
    {
        if (duckType == null)
            return null;

        var constraints = new GDEdgeConstraints();

        foreach (var kv in duckType.RequiredMethods)
            constraints.RequiredMethods[kv.Key] = kv.Value;

        foreach (var kv in duckType.RequiredProperties)
        {
            if (kv.Value != null)
                constraints.RequiredProperties[kv.Key] = kv.Value.DisplayName;
        }

        foreach (var signal in duckType.RequiredSignals)
            constraints.RequiredSignals.Add(signal);

        return constraints;
    }

    /// <summary>
    /// Whether this constraint has any requirements.
    /// </summary>
    public bool HasRequirements =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        RequiredSignals.Count > 0;

    /// <summary>
    /// Gets a summary string for tooltip display.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (RequiredMethods.Count > 0)
        {
            var methods = string.Join(", ", RequiredMethods.Keys.Select(m => m + "()"));
            parts.Add($"Methods: {methods}");
        }

        if (RequiredProperties.Count > 0)
        {
            var props = string.Join(", ", RequiredProperties.Keys);
            parts.Add($"Properties: {props}");
        }

        if (RequiredSignals.Count > 0)
        {
            var signals = string.Join(", ", RequiredSignals);
            parts.Add($"Signals: {signals}");
        }

        return string.Join("\n", parts);
    }
}
