using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents the difference between expected parameter types (from usage/type guards)
/// and actual types (from call site arguments).
/// </summary>
public class GDParameterTypeDiff
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Expected types based on usage within the method (type guards, method calls, etc.).
    /// These are the types the method body expects to receive.
    /// </summary>
    public GDUnionType ExpectedTypes { get; }

    /// <summary>
    /// Actual types passed at call sites.
    /// These are the types callers are actually passing.
    /// </summary>
    public GDUnionType ActualTypes { get; }

    /// <summary>
    /// Types that are expected but never passed (missing from call sites).
    /// </summary>
    public IReadOnlyList<string> MissingTypes { get; }

    /// <summary>
    /// Types that are passed but not expected (unexpected from call sites).
    /// </summary>
    public IReadOnlyList<string> UnexpectedTypes { get; }

    /// <summary>
    /// Types that are both expected and actually passed.
    /// </summary>
    public IReadOnlyList<string> MatchingTypes { get; }

    /// <summary>
    /// Whether there are any type mismatches.
    /// </summary>
    public bool HasMismatch => MissingTypes.Count > 0 || UnexpectedTypes.Count > 0;

    /// <summary>
    /// Whether there are no expected types (parameter usage is unknown).
    /// </summary>
    public bool ExpectedIsEmpty => ExpectedTypes.IsEmpty;

    /// <summary>
    /// Whether there are no actual types (no call sites analyzed).
    /// </summary>
    public bool ActualIsEmpty => ActualTypes.IsEmpty;

    /// <summary>
    /// Creates a parameter type diff.
    /// </summary>
    public GDParameterTypeDiff(
        string parameterName,
        GDUnionType expectedTypes,
        GDUnionType actualTypes,
        IReadOnlyList<string> missingTypes,
        IReadOnlyList<string> unexpectedTypes,
        IReadOnlyList<string> matchingTypes)
    {
        ParameterName = parameterName;
        ExpectedTypes = expectedTypes;
        ActualTypes = actualTypes;
        MissingTypes = missingTypes;
        UnexpectedTypes = unexpectedTypes;
        MatchingTypes = matchingTypes;
    }

    /// <summary>
    /// Creates a type diff by comparing expected and actual type unions.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="expected">Expected types from usage.</param>
    /// <param name="actual">Actual types from call sites.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type compatibility checks.</param>
    public static GDParameterTypeDiff Create(
        string parameterName,
        GDUnionType expected,
        GDUnionType actual,
        IGDRuntimeProvider? runtimeProvider = null)
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
                // Check if any actual type is compatible with this expected type
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
                // Check if this actual type is compatible with any expected type
                bool isCompatible = expectedSet.Any(expectedType =>
                    AreTypesCompatible(actualType, expectedType, runtimeProvider));

                if (!isCompatible)
                {
                    unexpected.Add(actualType);
                }
            }
        }

        return new GDParameterTypeDiff(
            parameterName,
            expected,
            actual,
            missing,
            unexpected,
            matching);
    }

    /// <summary>
    /// Checks if two types are compatible (source can be assigned to target).
    /// </summary>
    private static bool AreTypesCompatible(string sourceType, string targetType, IGDRuntimeProvider? runtimeProvider)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        // Exact match
        if (sourceType == targetType)
            return true;

        // Variant accepts anything
        if (targetType == "Variant")
            return true;

        // null is compatible with nullable types
        if (sourceType == "null")
            return true; // In GDScript, null can be assigned to any type

        // Check inheritance
        if (runtimeProvider != null)
        {
            return runtimeProvider.IsAssignableTo(sourceType, targetType);
        }

        // Basic numeric compatibility
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            // int is compatible with float
            if (sourceType == "int" && targetType == "float")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is numeric.
    /// </summary>
    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "float" or "bool";
    }

    /// <summary>
    /// Gets a human-readable summary of the diff.
    /// </summary>
    public string GetSummary()
    {
        if (ExpectedIsEmpty && ActualIsEmpty)
            return $"{ParameterName}: No type information available";

        if (ExpectedIsEmpty)
            return $"{ParameterName}: No expected types defined, actual: {ActualTypes}";

        if (ActualIsEmpty)
            return $"{ParameterName}: Expected {ExpectedTypes}, no call sites found";

        if (!HasMismatch)
            return $"{ParameterName}: Types match ({MatchingTypes.Count} type(s))";

        var parts = new List<string>();
        if (UnexpectedTypes.Count > 0)
            parts.Add($"unexpected: {string.Join(", ", UnexpectedTypes)}");
        if (MissingTypes.Count > 0)
            parts.Add($"missing: {string.Join(", ", MissingTypes)}");

        return $"{ParameterName}: {string.Join("; ", parts)}";
    }

    public override string ToString() => GetSummary();
}
