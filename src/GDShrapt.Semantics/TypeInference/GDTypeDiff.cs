using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents the type information for any AST node, combining:
/// - Expected types (internal constraints from annotations, type guards, match patterns, etc.)
/// - Actual types (external sources from assignments, call sites, flow analysis)
/// - Duck typing constraints (inferred from usage patterns)
///
/// This is the unified type diff API that works for any GDNode, not just parameters.
/// </summary>
public class GDTypeDiff
{
    /// <summary>
    /// The AST node this diff is for.
    /// </summary>
    public GDNode Node { get; }

    /// <summary>
    /// The symbol name (if applicable).
    /// </summary>
    public string? SymbolName { get; }

    /// <summary>
    /// Expected types based on internal constraints:
    /// - Explicit type annotations
    /// - Type guards (is checks)
    /// - typeof() checks
    /// - assert statements
    /// - match patterns
    /// </summary>
    public GDUnionType ExpectedTypes { get; }

    /// <summary>
    /// Actual types from external sources:
    /// - Assignments to this variable
    /// - Call site arguments (for parameters)
    /// - Return values (for method calls)
    /// - Initializers
    /// </summary>
    public GDUnionType ActualTypes { get; }

    /// <summary>
    /// Duck typing constraints inferred from usage patterns:
    /// - Method calls on the value
    /// - Property accesses
    /// - Signal connections
    /// </summary>
    public GDDuckType? DuckConstraints { get; }

    /// <summary>
    /// The narrowed type at this specific location (flow-sensitive).
    /// Null if no narrowing applies.
    /// </summary>
    public GDSemanticType? NarrowedType { get; }

    /// <summary>
    /// Types that are expected but not found in actual sources.
    /// </summary>
    public IReadOnlyList<GDSemanticType> MissingTypes { get; }

    /// <summary>
    /// Types that are in actual sources but not expected.
    /// </summary>
    public IReadOnlyList<GDSemanticType> UnexpectedTypes { get; }

    /// <summary>
    /// Types that match between expected and actual.
    /// </summary>
    public IReadOnlyList<GDSemanticType> MatchingTypes { get; }

    /// <summary>
    /// Confidence level for the type inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// The best inferred type (single type or union representation).
    /// </summary>
    public GDSemanticType TypeName { get; }

    /// <summary>
    /// Whether there are mismatches between expected and actual types.
    /// </summary>
    public bool HasMismatch => MissingTypes.Count > 0 || UnexpectedTypes.Count > 0;

    /// <summary>
    /// Whether there are any expected types defined.
    /// </summary>
    public bool HasExpectedTypes => !ExpectedTypes.IsEmpty;

    /// <summary>
    /// Whether there are any actual types defined.
    /// </summary>
    public bool HasActualTypes => !ActualTypes.IsEmpty;

    /// <summary>
    /// Whether there are duck typing constraints.
    /// </summary>
    public bool HasDuckConstraints => DuckConstraints?.HasRequirements == true;

    /// <summary>
    /// Whether the type is narrowed at this location.
    /// </summary>
    public bool IsNarrowed => NarrowedType != null;

    /// <summary>
    /// Creates a new type diff.
    /// </summary>
    public GDTypeDiff(
        GDNode node,
        string? symbolName,
        GDUnionType expectedTypes,
        GDUnionType actualTypes,
        GDDuckType? duckConstraints,
        GDSemanticType? narrowedType,
        IReadOnlyList<GDSemanticType> missingTypes,
        IReadOnlyList<GDSemanticType> unexpectedTypes,
        IReadOnlyList<GDSemanticType> matchingTypes,
        GDTypeConfidence confidence,
        GDSemanticType typeName)
    {
        Node = node;
        SymbolName = symbolName;
        ExpectedTypes = expectedTypes;
        ActualTypes = actualTypes;
        DuckConstraints = duckConstraints;
        NarrowedType = narrowedType;
        MissingTypes = missingTypes;
        UnexpectedTypes = unexpectedTypes;
        MatchingTypes = matchingTypes;
        Confidence = confidence;
        TypeName = typeName;
    }

    /// <summary>
    /// Creates a type diff by analyzing expected and actual types.
    /// </summary>
    public static GDTypeDiff Create(
        GDNode node,
        string? symbolName,
        GDUnionType expected,
        GDUnionType actual,
        GDDuckType? duckConstraints,
        string? narrowedType,
        IGDRuntimeProvider? runtimeProvider)
    {
        var expectedSet = new HashSet<GDSemanticType>(expected.Types);
        var actualSet = new HashSet<GDSemanticType>(actual.Types);

        // Calculate matching types
        var matching = expectedSet.Intersect(actualSet).ToList();

        // Calculate missing types (expected but not in actual)
        var missing = new List<GDSemanticType>();
        foreach (var expectedType in expectedSet)
        {
            if (!actualSet.Contains(expectedType))
            {
                bool isCompatible = actualSet.Any(actualType =>
                    AreTypesCompatible(actualType.DisplayName, expectedType.DisplayName, runtimeProvider));

                if (!isCompatible)
                {
                    missing.Add(expectedType);
                }
            }
        }

        // Calculate unexpected types (actual but not in expected)
        var unexpected = new List<GDSemanticType>();
        foreach (var actualType in actualSet)
        {
            if (!expectedSet.Contains(actualType))
            {
                bool isCompatible = expectedSet.Any(expectedType =>
                    AreTypesCompatible(actualType.DisplayName, expectedType.DisplayName, runtimeProvider));

                if (!isCompatible)
                {
                    unexpected.Add(actualType);
                }
            }
        }

        // Convert narrowedType string to GDSemanticType
        var narrowedSemType = narrowedType != null ? GDSemanticType.FromRuntimeTypeName(narrowedType) : (GDSemanticType?)null;

        // Determine the best type name
        GDSemanticType typeName;
        GDTypeConfidence confidence;

        if (narrowedSemType != null)
        {
            // Narrowed type takes priority
            typeName = narrowedSemType;
            confidence = GDTypeConfidence.High;
        }
        else if (expected.IsEmpty && actual.IsEmpty)
        {
            // Check duck constraints for possible types
            if (duckConstraints?.PossibleTypes.Count > 0)
            {
                var possibleList = duckConstraints.PossibleTypes.ToList();
                typeName = possibleList.Count == 1 ? possibleList[0] : (GDSemanticType)new GDUnionSemanticType(possibleList);
                confidence = GDTypeConfidence.Medium;
            }
            else
            {
                typeName = GDVariantSemanticType.Instance;
                confidence = GDTypeConfidence.Unknown;
            }
        }
        else if (!expected.IsEmpty)
        {
            var expTypes = expected.Types.ToList();
            typeName = expTypes.Count == 1 ? expTypes[0] : (GDSemanticType)new GDUnionSemanticType(expTypes);
            confidence = expected.AllHighConfidence ? GDTypeConfidence.High : GDTypeConfidence.Medium;
        }
        else
        {
            var actTypes = actual.Types.ToList();
            typeName = actTypes.Count == 1 ? actTypes[0] : (GDSemanticType)new GDUnionSemanticType(actTypes);
            confidence = actual.AllHighConfidence ? GDTypeConfidence.High : GDTypeConfidence.Medium;
        }

        return new GDTypeDiff(
            node,
            symbolName,
            expected,
            actual,
            duckConstraints,
            narrowedSemType,
            missing,
            unexpected,
            matching,
            confidence,
            typeName);
    }

    /// <summary>
    /// Creates an empty type diff (no type information available).
    /// </summary>
    public static GDTypeDiff Empty(GDNode node, string? symbolName = null)
    {
        return new GDTypeDiff(
            node,
            symbolName,
            new GDUnionType(),
            new GDUnionType(),
            null,
            null,
            new List<GDSemanticType>(),
            new List<GDSemanticType>(),
            new List<GDSemanticType>(),
            GDTypeConfidence.Unknown,
            GDVariantSemanticType.Instance);
    }

    /// <summary>
    /// Checks if two types are compatible.
    /// </summary>
    private static bool AreTypesCompatible(string sourceType, string targetType, IGDRuntimeProvider? runtimeProvider)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        if (sourceType == targetType)
            return true;

        if (targetType == "Variant")
            return true;

        if (sourceType == "null")
            return true;

        if (runtimeProvider != null)
        {
            return runtimeProvider.IsAssignableTo(sourceType, targetType);
        }

        if (GDTypeCompatibility.IsImplicitlyConvertible(sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Gets a human-readable summary of the type diff.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        // Type name
        parts.Add($"Type: {TypeName.DisplayName}");

        // Confidence
        parts.Add($"Confidence: {Confidence}");

        // Narrowing info
        if (IsNarrowed)
            parts.Add($"Narrowed to: {NarrowedType!.DisplayName}");

        // Expected vs actual
        if (HasExpectedTypes)
            parts.Add($"Expected: {ExpectedTypes}");
        if (HasActualTypes)
            parts.Add($"Actual: {ActualTypes}");

        // Mismatches
        if (MissingTypes.Count > 0)
            parts.Add($"Missing: {string.Join(", ", MissingTypes.Select(t => t.DisplayName))}");
        if (UnexpectedTypes.Count > 0)
            parts.Add($"Unexpected: {string.Join(", ", UnexpectedTypes.Select(t => t.DisplayName))}");

        // Duck constraints
        if (HasDuckConstraints)
            parts.Add($"Duck: {DuckConstraints}");

        return string.Join("; ", parts);
    }

    public override string ToString() => GetSummary();
}
