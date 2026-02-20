using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a detected reflection-based member access pattern
/// (e.g. get_method_list() + call(method.name), get_property_list() + set(prop.name, val)).
/// Members matching this site are considered reachable with Potential confidence.
/// </summary>
public class GDReflectionCallSite
{
    /// <summary>
    /// What kind of reflection this site represents.
    /// </summary>
    public GDReflectionKind Kind { get; set; } = GDReflectionKind.Method;

    /// <summary>
    /// Type whose members become reachable (e.g. "Node", "MyClass").
    /// "*" means any type (receiver was Variant/unresolvable).
    /// </summary>
    public string ReceiverTypeName { get; set; } = "";

    /// <summary>
    /// Optional name filters from guard conditions (begins_with, ends_with, contains, ==).
    /// Null or empty means all members are reachable.
    /// </summary>
    public List<GDReflectionNameFilter>? NameFilters { get; set; }

    /// <summary>
    /// Source file where the reflection pattern was found.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Line of the invocation (0-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column of the invocation (0-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// The reflection method used: "call", "call_deferred", "callv", "set", "get", "emit_signal", "connect".
    /// </summary>
    public string CallMethod { get; set; } = "call";

    /// <summary>
    /// Whether this is a self call (receiver is current class).
    /// </summary>
    public bool IsSelfCall { get; set; }

    /// <summary>
    /// Checks if a member name matches this reflection scope.
    /// </summary>
    public bool Matches(string name)
    {
        if (NameFilters == null || NameFilters.Count == 0)
            return true;

        foreach (var filter in NameFilters)
        {
            if (filter.Matches(name))
                return true;
        }

        return false;
    }
}
