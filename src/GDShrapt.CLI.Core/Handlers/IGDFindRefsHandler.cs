using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding symbol references.
/// </summary>
public interface IGDFindRefsHandler
{
    /// <summary>
    /// Finds all references to a symbol across the project, grouped by declaration.
    /// </summary>
    IReadOnlyList<GDReferenceGroup> FindReferences(string symbolName, string? filePath = null);

    /// <summary>
    /// Finds all references including unrelated same-name symbols, returning a structured result.
    /// </summary>
    GDFindRefsResult FindAllReferences(string symbolName, string? filePath = null);
}

/// <summary>
/// Complete result of a find-refs operation, including primary and unrelated groups.
/// </summary>
public class GDFindRefsResult
{
    /// <summary>
    /// The symbol name being searched.
    /// </summary>
    public required string SymbolName { get; init; }

    /// <summary>
    /// Kind of the primary symbol (method, variable, signal, etc.).
    /// </summary>
    public string SymbolKind { get; init; } = "unknown";

    /// <summary>
    /// Class name where the primary symbol is declared.
    /// </summary>
    public string? DeclaredInClassName { get; init; }

    /// <summary>
    /// File path where the primary symbol is declared.
    /// </summary>
    public string? DeclaredInFilePath { get; init; }

    /// <summary>
    /// Line of the primary symbol declaration (1-based).
    /// </summary>
    public int DeclaredAtLine { get; init; }

    /// <summary>
    /// Primary reference groups (related by inheritance).
    /// </summary>
    public IReadOnlyList<GDReferenceGroup> PrimaryGroups { get; init; } = [];

    /// <summary>
    /// Unrelated same-name symbol groups (different inheritance hierarchies).
    /// </summary>
    public IReadOnlyList<GDReferenceGroup> UnrelatedGroups { get; init; } = [];

    /// <summary>
    /// Total reference count across all groups.
    /// </summary>
    public int TotalCount => CountRefs(PrimaryGroups) + CountRefs(UnrelatedGroups);

    private static int CountRefs(IReadOnlyList<GDReferenceGroup> groups)
    {
        int count = 0;
        foreach (var g in groups)
        {
            count += g.Locations.Count;
            count += CountRefs(g.Overrides);
        }
        return count;
    }
}

/// <summary>
/// A group of references tied to a single declaration.
/// </summary>
public class GDReferenceGroup
{
    /// <summary>
    /// Class name where the original declaration lives (e.g. "TowerBase").
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// File where the symbol is declared.
    /// </summary>
    public required string DeclarationFilePath { get; init; }

    /// <summary>
    /// Line of the declaration (1-based).
    /// </summary>
    public int DeclarationLine { get; init; }

    /// <summary>
    /// Column of the declaration (0-based).
    /// </summary>
    public int DeclarationColumn { get; init; }

    /// <summary>
    /// Whether this declaration overrides a base class member.
    /// </summary>
    public bool IsOverride { get; init; }

    /// <summary>
    /// Whether this group uses an inherited symbol (no local declaration).
    /// </summary>
    public bool IsInherited { get; init; }

    /// <summary>
    /// Whether this group contains cross-file references (duck-typed, potential).
    /// </summary>
    public bool IsCrossFile { get; init; }

    /// <summary>
    /// Whether this group contains signal connection references.
    /// </summary>
    public bool IsSignalConnection { get; init; }

    /// <summary>
    /// The symbol name being searched for (used for highlighting in output).
    /// </summary>
    public string? SymbolName { get; init; }

    /// <summary>
    /// Reference locations belonging to this declaration's own file.
    /// </summary>
    public List<GDReferenceLocation> Locations { get; init; } = new();

    /// <summary>
    /// Child classes that override this symbol, each with their own locations.
    /// </summary>
    public List<GDReferenceGroup> Overrides { get; init; } = new();
}

/// <summary>
/// Represents a reference location in the codebase.
/// </summary>
public class GDReferenceLocation
{
    /// <summary>
    /// Full path to the file containing the reference.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Whether this is the declaration of the symbol.
    /// </summary>
    public bool IsDeclaration { get; init; }

    /// <summary>
    /// Whether this declaration is an override of a base class member.
    /// </summary>
    public bool IsOverride { get; init; }

    /// <summary>
    /// Whether this is a super.method() call to the base class.
    /// </summary>
    public bool IsSuperCall { get; init; }

    /// <summary>
    /// Whether this is a write reference (assignment).
    /// </summary>
    public bool IsWrite { get; init; }

    /// <summary>
    /// Optional context text around the reference.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Confidence level of this reference (null for declaration-based refs).
    /// </summary>
    public GDReferenceConfidence? Confidence { get; init; }

    /// <summary>
    /// Human-readable reason for the confidence determination.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Whether this reference is a contract string (has_method, emit_signal, etc.).
    /// </summary>
    public bool IsContractString { get; init; }

    /// <summary>
    /// Whether this reference is a signal connection (connect() or scene [connection]).
    /// </summary>
    public bool IsSignalConnection { get; init; }

    /// <summary>
    /// Signal name for signal connection references.
    /// </summary>
    public string? SignalName { get; init; }

    /// <summary>
    /// Whether this signal connection comes from a scene file.
    /// </summary>
    public bool IsSceneSignal { get; init; }

    /// <summary>
    /// End column of the identifier span (0-based, exclusive). Null if unavailable.
    /// </summary>
    public int? EndColumn { get; init; }

    /// <summary>
    /// Receiver type name for signal connections (e.g. "EnemyTank" for timeout.connect()).
    /// </summary>
    public string? ReceiverTypeName { get; init; }
}
