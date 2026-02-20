using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a member that was excluded from the dead code report
/// because it is reachable via a reflection pattern.
/// Used with --show-dropped-by-reflection CLI flag.
/// </summary>
public class GDReflectionDroppedItem
{
    /// <summary>
    /// What kind of member was dropped (Function, Variable, Signal).
    /// </summary>
    public GDDeadCodeKind Kind { get; set; }

    /// <summary>
    /// Name of the dropped member.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// File where the member is declared.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Line of the member declaration (0-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column of the member declaration (0-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// What kind of reflection pattern matched (Method, Property, Signal).
    /// </summary>
    public GDReflectionKind ReflectionKind { get; set; }

    /// <summary>
    /// File where the reflection pattern was found.
    /// </summary>
    public string ReflectionSiteFile { get; set; } = "";

    /// <summary>
    /// Line of the reflection invocation (0-based).
    /// </summary>
    public int ReflectionSiteLine { get; set; }

    /// <summary>
    /// The reflection call method used (e.g. "call", "set", "emit_signal").
    /// </summary>
    public string CallMethod { get; set; } = "";

    /// <summary>
    /// The receiver type name from the reflection site.
    /// </summary>
    public string ReceiverTypeName { get; set; } = "";

    /// <summary>
    /// Name filters from the reflection site (if any).
    /// </summary>
    public List<GDReflectionNameFilter>? NameFilters { get; set; }
}
