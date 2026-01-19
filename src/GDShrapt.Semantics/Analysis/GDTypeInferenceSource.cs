using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents the source of a type inference - the AST node and reason that led to this inference.
/// Used for navigation (clicking on type parts) and for tracking derivability.
/// </summary>
public class GDTypeInferenceSource
{
    /// <summary>
    /// The AST node that is the source of this inference.
    /// For type checks: the GDDualOperatorExpression (is check)
    /// For duck typing: the GDCallExpression or GDMemberOperatorExpression
    /// For iteration: the GDForStatement
    /// </summary>
    public GDNode? SourceNode { get; init; }

    /// <summary>
    /// Human-readable description of why this type was inferred.
    /// </summary>
    public string Reason { get; init; } = "";

    /// <summary>
    /// The kind of inference that produced this type.
    /// </summary>
    public GDInferenceKind Kind { get; init; }

    /// <summary>
    /// Creates a source from a type check (is operator).
    /// </summary>
    public static GDTypeInferenceSource FromTypeCheck(GDNode node, string typeName)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.TypeCheck,
            Reason = $"type check: is {typeName}"
        };

    /// <summary>
    /// Creates a source from duck typing (method call).
    /// </summary>
    public static GDTypeInferenceSource FromMethodCall(GDNode node, string methodName)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.DuckTyping,
            Reason = $"duck typing: .{methodName}() call"
        };

    /// <summary>
    /// Creates a source from duck typing (property access).
    /// </summary>
    public static GDTypeInferenceSource FromPropertyAccess(GDNode node, string propertyName)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.DuckTyping,
            Reason = $"duck typing: .{propertyName} access"
        };

    /// <summary>
    /// Creates a source from iteration (for loop).
    /// </summary>
    public static GDTypeInferenceSource FromIteration(GDNode node)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.Iteration,
            Reason = "used in for loop"
        };

    /// <summary>
    /// Creates a source from indexer access.
    /// </summary>
    public static GDTypeInferenceSource FromIndexer(GDNode node)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.Indexer,
            Reason = "used with indexer []"
        };

    /// <summary>
    /// Creates a source for alias tracking.
    /// </summary>
    public static GDTypeInferenceSource FromAlias(GDNode node, string aliasName, string originalName)
        => new()
        {
            SourceNode = node,
            Kind = GDInferenceKind.Alias,
            Reason = $"via alias: {aliasName} = {originalName}"
        };
}

/// <summary>
/// The kind of inference that produced a type.
/// </summary>
public enum GDInferenceKind
{
    /// <summary>
    /// Type was inferred from an explicit 'is' type check.
    /// </summary>
    TypeCheck,

    /// <summary>
    /// Type was inferred from duck typing (method calls, property access).
    /// </summary>
    DuckTyping,

    /// <summary>
    /// Type was inferred from being used in a for loop.
    /// </summary>
    Iteration,

    /// <summary>
    /// Type was inferred from being used with indexer [].
    /// </summary>
    Indexer,

    /// <summary>
    /// Type was inferred through an alias variable.
    /// </summary>
    Alias,

    /// <summary>
    /// Type was inferred from being passed to a method.
    /// </summary>
    CallSite,

    /// <summary>
    /// Type was explicitly declared.
    /// </summary>
    Declared
}

/// <summary>
/// A type member in a union with its inference source and derivability information.
/// </summary>
public class GDUnionTypeMember
{
    /// <summary>
    /// The base type name (e.g., "Dictionary", "Array").
    /// </summary>
    public string BaseType { get; init; } = "";

    /// <summary>
    /// The formatted type with generics (e.g., "Dictionary[int, Variant]").
    /// </summary>
    public string FormattedType { get; init; } = "";

    /// <summary>
    /// The source of this type inference.
    /// </summary>
    public GDTypeInferenceSource? Source { get; init; }

    /// <summary>
    /// Key type info for containers (with source and derivability).
    /// </summary>
    public GDGenericTypeSlot? KeyType { get; init; }

    /// <summary>
    /// Value/element type info for containers (with source and derivability).
    /// </summary>
    public GDGenericTypeSlot? ValueType { get; init; }

    /// <summary>
    /// Confidence level of this type inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; init; }
}

/// <summary>
/// Represents a generic type slot (key or value type) with its inference source
/// and derivability marker.
/// </summary>
public class GDGenericTypeSlot
{
    /// <summary>
    /// The type name. Can be a union like "int | String".
    /// If derivable, this shows current best guess.
    /// </summary>
    public string TypeName { get; init; } = "Variant";

    /// <summary>
    /// Whether this type can be inferred further with more analysis.
    /// When true, displays as "&lt;Derivable&gt;" or "TypeName?" in UI.
    /// </summary>
    public bool IsDerivable { get; init; }

    /// <summary>
    /// The AST node that can provide more type information.
    /// Clicking on &lt;Derivable&gt; navigates to this node.
    /// </summary>
    public GDNode? DerivableSourceNode { get; init; }

    /// <summary>
    /// Human-readable reason why this is derivable.
    /// </summary>
    public string? DerivableReason { get; init; }

    /// <summary>
    /// The sources that contributed to this type inference.
    /// </summary>
    public List<GDTypeInferenceSource> Sources { get; init; } = new();

    /// <summary>
    /// Confidence level of this type inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; init; }

    /// <summary>
    /// Creates a slot with a known type.
    /// </summary>
    public static GDGenericTypeSlot Known(string typeName, GDTypeConfidence confidence = GDTypeConfidence.High)
        => new() { TypeName = typeName, Confidence = confidence };

    /// <summary>
    /// Creates a derivable slot (type can be inferred further).
    /// </summary>
    public static GDGenericTypeSlot Derivable(GDNode? sourceNode, string? reason = null, string currentGuess = "Variant")
        => new()
        {
            TypeName = currentGuess,
            IsDerivable = true,
            DerivableSourceNode = sourceNode,
            DerivableReason = reason ?? "can be inferred from further analysis",
            Confidence = GDTypeConfidence.Low
        };

    /// <summary>
    /// Creates a Variant slot (completely unknown).
    /// </summary>
    public static GDGenericTypeSlot Variant()
        => new() { TypeName = "Variant", Confidence = GDTypeConfidence.Unknown };

    /// <summary>
    /// Formats the type for display.
    /// If derivable, shows "&lt;Derivable&gt;" marker.
    /// </summary>
    public string ToDisplayString()
    {
        if (IsDerivable)
            return $"<Derivable>";
        return TypeName;
    }

    /// <summary>
    /// Formats the type with derivable marker inline.
    /// E.g., "int | &lt;Derivable&gt;" or "Node?"
    /// </summary>
    public string ToDisplayStringWithMarker()
    {
        if (IsDerivable && TypeName != "Variant")
            return $"{TypeName}?";
        if (IsDerivable)
            return "<Derivable>";
        return TypeName;
    }
}
