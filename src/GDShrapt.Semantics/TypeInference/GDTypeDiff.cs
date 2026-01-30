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
    public string? NarrowedType { get; }

    /// <summary>
    /// Types that are expected but not found in actual sources.
    /// </summary>
    public IReadOnlyList<string> MissingTypes { get; }

    /// <summary>
    /// Types that are in actual sources but not expected.
    /// </summary>
    public IReadOnlyList<string> UnexpectedTypes { get; }

    /// <summary>
    /// Types that match between expected and actual.
    /// </summary>
    public IReadOnlyList<string> MatchingTypes { get; }

    /// <summary>
    /// Confidence level for the type inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// The best inferred type name (single type or union representation).
    /// </summary>
    public string TypeName { get; }

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
    public bool IsNarrowed => !string.IsNullOrEmpty(NarrowedType);

    /// <summary>
    /// Creates a new type diff.
    /// </summary>
    public GDTypeDiff(
        GDNode node,
        string? symbolName,
        GDUnionType expectedTypes,
        GDUnionType actualTypes,
        GDDuckType? duckConstraints,
        string? narrowedType,
        IReadOnlyList<string> missingTypes,
        IReadOnlyList<string> unexpectedTypes,
        IReadOnlyList<string> matchingTypes,
        GDTypeConfidence confidence,
        string typeName)
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
        var expectedSet = new HashSet<string>(expected.Types);
        var actualSet = new HashSet<string>(actual.Types);

        // Calculate matching types
        var matching = expectedSet.Intersect(actualSet).ToList();

        // Calculate missing types (expected but not in actual)
        var missing = new List<string>();
        foreach (var expectedType in expectedSet)
        {
            if (!actualSet.Contains(expectedType))
            {
                bool isCompatible = actualSet.Any(actualType =>
                    AreTypesCompatible(actualType, expectedType, runtimeProvider));

                if (!isCompatible)
                {
                    missing.Add(expectedType);
                }
            }
        }

        // Calculate unexpected types (actual but not in expected)
        var unexpected = new List<string>();
        foreach (var actualType in actualSet)
        {
            if (!expectedSet.Contains(actualType))
            {
                bool isCompatible = expectedSet.Any(expectedType =>
                    AreTypesCompatible(actualType, expectedType, runtimeProvider));

                if (!isCompatible)
                {
                    unexpected.Add(actualType);
                }
            }
        }

        // Determine the best type name
        string typeName;
        GDTypeConfidence confidence;

        if (!string.IsNullOrEmpty(narrowedType))
        {
            // Narrowed type takes priority
            typeName = narrowedType;
            confidence = GDTypeConfidence.High;
        }
        else if (expected.IsEmpty && actual.IsEmpty)
        {
            // Check duck constraints for possible types
            if (duckConstraints?.PossibleTypes.Count > 0)
            {
                typeName = string.Join("|", duckConstraints.PossibleTypes);
                confidence = GDTypeConfidence.Medium;
            }
            else
            {
                typeName = "Variant";
                confidence = GDTypeConfidence.Unknown;
            }
        }
        else if (!expected.IsEmpty)
        {
            typeName = expected.ToString();
            confidence = expected.AllHighConfidence ? GDTypeConfidence.High : GDTypeConfidence.Medium;
        }
        else
        {
            typeName = actual.ToString();
            confidence = actual.AllHighConfidence ? GDTypeConfidence.High : GDTypeConfidence.Medium;
        }

        return new GDTypeDiff(
            node,
            symbolName,
            expected,
            actual,
            duckConstraints,
            narrowedType,
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
            new List<string>(),
            new List<string>(),
            new List<string>(),
            GDTypeConfidence.Unknown,
            "Variant");
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

        // Basic numeric compatibility
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            if (sourceType == "int" && targetType == "float")
                return true;
        }

        return false;
    }

    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "float" or "bool";
    }

    /// <summary>
    /// Gets a human-readable summary of the type diff.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        // Type name
        parts.Add($"Type: {TypeName}");

        // Confidence
        parts.Add($"Confidence: {Confidence}");

        // Narrowing info
        if (IsNarrowed)
            parts.Add($"Narrowed to: {NarrowedType}");

        // Expected vs actual
        if (HasExpectedTypes)
            parts.Add($"Expected: {ExpectedTypes}");
        if (HasActualTypes)
            parts.Add($"Actual: {ActualTypes}");

        // Mismatches
        if (MissingTypes.Count > 0)
            parts.Add($"Missing: {string.Join(", ", MissingTypes)}");
        if (UnexpectedTypes.Count > 0)
            parts.Add($"Unexpected: {string.Join(", ", UnexpectedTypes)}");

        // Duck constraints
        if (HasDuckConstraints)
            parts.Add($"Duck: {DuckConstraints}");

        return string.Join("; ", parts);
    }

    public override string ToString() => GetSummary();
}
