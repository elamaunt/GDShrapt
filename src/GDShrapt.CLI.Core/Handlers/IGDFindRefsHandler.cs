using System.Collections.Generic;
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
}
