using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// The kind of a symbol reference in the codebase.
/// </summary>
public enum GDSymbolReferenceKind
{
    Declaration,
    Read,
    Write,
    Call,
    Override,
    SuperCall,
    ContractString,
    SignalConnection,
    SceneSignalConnection,
    TypeUsage
}

/// <summary>
/// A single reference to a symbol, with full context for any consumer (find-refs, rename, LSP).
/// Line and column are 0-based (AST convention). Consumers convert to 1-based as needed.
/// </summary>
public class GDSymbolReference
{
    public GDScriptFile Script { get; }
    public GDNode? Node { get; }
    public GDSyntaxToken? IdentifierToken { get; }
    public int Line { get; }
    public int Column { get; }
    public GDReferenceConfidence Confidence { get; }
    public string? ConfidenceReason { get; }
    public GDSymbolReferenceKind Kind { get; }
    public bool IsInherited { get; }
    public bool IsOverride { get; }
    public string? CallerTypeName { get; }
    public string? SignalName { get; }
    public bool IsSceneSignal { get; }

    public GDSymbolReference(
        GDScriptFile script,
        GDNode? node,
        GDSyntaxToken? identifierToken,
        int line,
        int column,
        GDReferenceConfidence confidence,
        string? confidenceReason,
        GDSymbolReferenceKind kind,
        bool isInherited = false,
        bool isOverride = false,
        string? callerTypeName = null,
        string? signalName = null,
        bool isSceneSignal = false)
    {
        Script = script;
        Node = node;
        IdentifierToken = identifierToken;
        Line = line;
        Column = column;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
        Kind = kind;
        IsInherited = isInherited;
        IsOverride = isOverride;
        CallerTypeName = callerTypeName;
        SignalName = signalName;
        IsSceneSignal = isSceneSignal;
    }

    /// <summary>
    /// File path of the script containing this reference.
    /// </summary>
    public string? FilePath => Script.FullPath;

    /// <summary>
    /// Whether this is a contract string reference (has_method, emit_signal, call, etc.).
    /// </summary>
    public bool IsContractString => Kind == GDSymbolReferenceKind.ContractString;

    /// <summary>
    /// Whether this is a signal connection reference.
    /// </summary>
    public bool IsSignalConnection =>
        Kind == GDSymbolReferenceKind.SignalConnection ||
        Kind == GDSymbolReferenceKind.SceneSignalConnection;

    public override string ToString() =>
        $"{FilePath ?? "unknown"}:{Line + 1}:{Column} [{Kind}] [{Confidence}]";
}

/// <summary>
/// Complete set of references to a symbol, collected by GDSymbolReferenceCollector.
/// </summary>
public class GDSymbolReferences
{
    public GDSymbolInfo Symbol { get; }
    public GDScriptFile? DeclaringScript { get; }
    public IReadOnlyList<GDSymbolReference> References { get; }
    public IReadOnlyList<GDRenameWarning> StringWarnings { get; }

    public GDSymbolReferences(
        GDSymbolInfo symbol,
        GDScriptFile? declaringScript,
        IReadOnlyList<GDSymbolReference> references,
        IReadOnlyList<GDRenameWarning> stringWarnings)
    {
        Symbol = symbol;
        DeclaringScript = declaringScript;
        References = references;
        StringWarnings = stringWarnings;
    }

    /// <summary>
    /// References in the same file as the declaration.
    /// </summary>
    public IEnumerable<GDSymbolReference> SameFileReferences =>
        DeclaringScript?.FullPath != null
            ? References.Where(r => string.Equals(r.FilePath, DeclaringScript.FullPath, StringComparison.OrdinalIgnoreCase))
            : Enumerable.Empty<GDSymbolReference>();

    /// <summary>
    /// References in files other than the declaration file.
    /// </summary>
    public IEnumerable<GDSymbolReference> CrossFileReferences =>
        DeclaringScript?.FullPath != null
            ? References.Where(r => !string.Equals(r.FilePath, DeclaringScript.FullPath, StringComparison.OrdinalIgnoreCase))
            : References;

    public IEnumerable<GDSymbolReference> StrictReferences =>
        References.Where(r => r.Confidence == GDReferenceConfidence.Strict);

    public IEnumerable<GDSymbolReference> PotentialReferences =>
        References.Where(r => r.Confidence == GDReferenceConfidence.Potential);

    public IEnumerable<GDSymbolReference> SignalConnectionReferences =>
        References.Where(r => r.IsSignalConnection);

    public IEnumerable<GDSymbolReference> ContractStringReferences =>
        References.Where(r => r.IsContractString);
}

/// <summary>
/// Complete result of collecting all references to a symbol, including unrelated same-name symbols.
/// </summary>
public class GDAllSymbolReferences
{
    /// <summary>
    /// Primary references (from the main inheritance hierarchy).
    /// </summary>
    public GDSymbolReferences Primary { get; }

    /// <summary>
    /// Unrelated same-name symbol references (different inheritance hierarchies).
    /// </summary>
    public IReadOnlyList<GDSymbolReferences> UnrelatedSymbols { get; }

    public GDAllSymbolReferences(
        GDSymbolReferences primary,
        IReadOnlyList<GDSymbolReferences> unrelatedSymbols)
    {
        Primary = primary;
        UnrelatedSymbols = unrelatedSymbols;
    }
}
