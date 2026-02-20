using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Kind of string filter applied to member names in reflection patterns.
/// </summary>
public enum GDReflectionFilterKind
{
    BeginsWith,
    EndsWith,
    Contains,
    Exact
}

/// <summary>
/// A name filter extracted from guard conditions in reflection patterns
/// (e.g. begins_with("test_"), name == "exact", ends_with("_handler")).
/// </summary>
public class GDReflectionNameFilter
{
    public GDReflectionFilterKind Kind { get; set; }
    public string Value { get; set; } = "";

    public bool Matches(string name)
    {
        return Kind switch
        {
            GDReflectionFilterKind.BeginsWith =>
                name.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
            GDReflectionFilterKind.EndsWith =>
                name.EndsWith(Value, StringComparison.OrdinalIgnoreCase),
            GDReflectionFilterKind.Contains =>
                name.IndexOf(Value, StringComparison.OrdinalIgnoreCase) >= 0,
            GDReflectionFilterKind.Exact =>
                string.Equals(name, Value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
