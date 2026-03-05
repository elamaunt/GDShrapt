using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for call hierarchy navigation.
/// </summary>
public interface IGDCallHierarchyHandler
{
    /// <summary>
    /// Prepares a call hierarchy item for the symbol at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Call hierarchy item or null if symbol is not a method.</returns>
    GDCallHierarchyItem? Prepare(string filePath, int line, int column);

    /// <summary>
    /// Gets incoming calls (callers) of the specified method.
    /// </summary>
    IReadOnlyList<GDIncomingCall> GetIncomingCalls(GDCallHierarchyItem item);

    /// <summary>
    /// Gets outgoing calls (callees) from the specified method.
    /// </summary>
    IReadOnlyList<GDOutgoingCall> GetOutgoingCalls(GDCallHierarchyItem item);
}

/// <summary>
/// Represents a method in the call hierarchy.
/// </summary>
public class GDCallHierarchyItem
{
    /// <summary>Method name.</summary>
    public required string Name { get; init; }

    /// <summary>Class containing the method.</summary>
    public string? ClassName { get; init; }

    /// <summary>Full file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Line number (1-based).</summary>
    public int Line { get; init; }

    /// <summary>Column number (0-based).</summary>
    public int Column { get; init; }
}

/// <summary>
/// An incoming call (caller) in the call hierarchy.
/// </summary>
public class GDIncomingCall
{
    /// <summary>The calling method.</summary>
    public required GDCallHierarchyItem From { get; init; }

    /// <summary>Ranges where the call occurs within the calling method (1-based line, 0-based column).</summary>
    public required IReadOnlyList<GDCallRange> FromRanges { get; init; }
}

/// <summary>
/// An outgoing call (callee) in the call hierarchy.
/// </summary>
public class GDOutgoingCall
{
    /// <summary>The called method.</summary>
    public required GDCallHierarchyItem To { get; init; }

    /// <summary>Ranges where the call occurs within the current method (1-based line, 0-based column).</summary>
    public required IReadOnlyList<GDCallRange> FromRanges { get; init; }
}

/// <summary>
/// A position range of a call expression.
/// </summary>
public class GDCallRange
{
    /// <summary>Line number (1-based).</summary>
    public int Line { get; init; }

    /// <summary>Column number (0-based).</summary>
    public int Column { get; init; }
}
