using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents the type comparison between an argument and its expected parameter.
/// Provides detailed information for diagnostic messages.
/// </summary>
public class GDArgumentTypeDiff
{
    /// <summary>
    /// The parameter name (e.g., "speed", "target").
    /// </summary>
    public string? ParameterName { get; set; }

    /// <summary>
    /// The 0-based index of the parameter/argument.
    /// </summary>
    public int ParameterIndex { get; set; }

    #region Actual Type (What was passed)

    /// <summary>
    /// The actual argument type that was passed.
    /// </summary>
    public GDSemanticType? ActualType { get; set; }

    /// <summary>
    /// Source of the actual type (e.g., "string literal", "variable 'x'", "function call result").
    /// </summary>
    public string? ActualTypeSource { get; set; }

    #endregion

    #region Expected Types (Constraints)

    /// <summary>
    /// The expected parameter type(s) from the function signature.
    /// May contain multiple types for Union types or type guards.
    /// </summary>
    public IReadOnlyList<GDSemanticType> ExpectedTypes { get; set; } = new List<GDSemanticType>();

    /// <summary>
    /// Source of the expected types (e.g., "type annotation", "type guard", "usage analysis").
    /// </summary>
    public string? ExpectedTypeSource { get; set; }

    /// <summary>
    /// Duck typing constraints (required methods/properties).
    /// </summary>
    public GDDuckType? DuckConstraints { get; set; }

    #endregion

    #region Analysis Result

    /// <summary>
    /// Whether the actual type is compatible with expected types.
    /// </summary>
    public bool IsCompatible { get; set; }

    /// <summary>
    /// Reason why types are incompatible (null if compatible).
    /// </summary>
    public string? IncompatibilityReason { get; set; }

    /// <summary>
    /// Confidence level of the type analysis.
    /// </summary>
    public GDReferenceConfidence Confidence { get; set; }

    /// <summary>
    /// True if parameter is Variant (no type annotation) - validation may be skipped.
    /// </summary>
    public bool IsVariantParameter { get; set; }

    /// <summary>
    /// True if this diff should be skipped (Variant with no constraints).
    /// </summary>
    public bool ShouldSkip => IsVariantParameter && (ExpectedTypes.Count == 0) && (DuckConstraints == null || !DuckConstraints.HasRequirements);

    #endregion

    /// <summary>
    /// Creates a skip result for Variant parameters without constraints.
    /// </summary>
    public static GDArgumentTypeDiff Skip(int index = 0, string? parameterName = null)
    {
        return new GDArgumentTypeDiff
        {
            ParameterIndex = index,
            ParameterName = parameterName,
            IsVariantParameter = true,
            IsCompatible = true,
            Confidence = GDReferenceConfidence.NameMatch
        };
    }

    /// <summary>
    /// Creates a compatible result.
    /// </summary>
    public static GDArgumentTypeDiff Compatible(
        int index,
        string? parameterName,
        string? actualType,
        string? actualSource,
        IReadOnlyList<string> expectedTypes,
        string? expectedSource,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict)
    {
        return new GDArgumentTypeDiff
        {
            ParameterIndex = index,
            ParameterName = parameterName,
            ActualType = actualType != null ? GDSemanticType.FromRuntimeTypeName(actualType) : null,
            ActualTypeSource = actualSource,
            ExpectedTypes = expectedTypes.Select(t => GDSemanticType.FromRuntimeTypeName(t)).ToList(),
            ExpectedTypeSource = expectedSource,
            IsCompatible = true,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Creates an incompatible result.
    /// </summary>
    public static GDArgumentTypeDiff Incompatible(
        int index,
        string? parameterName,
        string? actualType,
        string? actualSource,
        IReadOnlyList<string> expectedTypes,
        string? expectedSource,
        string incompatibilityReason,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict)
    {
        return new GDArgumentTypeDiff
        {
            ParameterIndex = index,
            ParameterName = parameterName,
            ActualType = actualType != null ? GDSemanticType.FromRuntimeTypeName(actualType) : null,
            ActualTypeSource = actualSource,
            ExpectedTypes = expectedTypes.Select(t => GDSemanticType.FromRuntimeTypeName(t)).ToList(),
            ExpectedTypeSource = expectedSource,
            IsCompatible = false,
            IncompatibilityReason = incompatibilityReason,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Formats a detailed diagnostic message.
    /// </summary>
    public string FormatDetailedMessage()
    {
        var sb = new StringBuilder();

        // Header
        sb.Append($"Argument type mismatch at position {ParameterIndex + 1}");
        if (!string.IsNullOrEmpty(ParameterName))
            sb.Append($" (parameter '{ParameterName}')");
        sb.AppendLine();
        sb.AppendLine();

        // What was passed
        sb.Append($"  Actual type:    {ActualType?.DisplayName ?? "unknown"}");
        if (!string.IsNullOrEmpty(ActualTypeSource))
            sb.Append($" (from {ActualTypeSource})");
        sb.AppendLine();

        // What was expected
        if (ExpectedTypes.Count == 1)
        {
            sb.Append($"  Expected type:  {ExpectedTypes[0].DisplayName}");
            if (!string.IsNullOrEmpty(ExpectedTypeSource))
                sb.Append($" (from {ExpectedTypeSource})");
            sb.AppendLine();
        }
        else if (ExpectedTypes.Count > 1)
        {
            sb.Append($"  Expected types: {string.Join(" | ", ExpectedTypes.Select(t => t.DisplayName))}");
            if (!string.IsNullOrEmpty(ExpectedTypeSource))
                sb.Append($" (from {ExpectedTypeSource})");
            sb.AppendLine();
        }

        // Duck typing constraints
        if (DuckConstraints != null && DuckConstraints.HasRequirements)
        {
            if (DuckConstraints.RequiredMethods.Count > 0)
            {
                sb.AppendLine($"  Required methods: {string.Join(", ", DuckConstraints.RequiredMethods)}() (from usage in function body)");
            }
            if (DuckConstraints.RequiredProperties.Count > 0)
            {
                sb.AppendLine($"  Required properties: {string.Join(", ", DuckConstraints.RequiredProperties)} (from usage in function body)");
            }
        }

        sb.AppendLine();

        // Conclusion
        if (!string.IsNullOrEmpty(IncompatibilityReason))
        {
            sb.AppendLine($"  {IncompatibilityReason}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a short diagnostic message for single-line display.
    /// </summary>
    public string FormatShortMessage()
    {
        if (IsCompatible)
            return string.Empty;

        var paramInfo = !string.IsNullOrEmpty(ParameterName)
            ? $"'{ParameterName}'"
            : $"at position {ParameterIndex + 1}";

        if (ExpectedTypes.Count == 1)
        {
            return $"Argument {paramInfo} expects '{ExpectedTypes[0].DisplayName}', got '{ActualType?.DisplayName ?? "unknown"}'";
        }
        else if (ExpectedTypes.Count > 1)
        {
            return $"Argument {paramInfo} expects [{string.Join("|", ExpectedTypes.Select(t => t.DisplayName))}], got '{ActualType?.DisplayName ?? "unknown"}'";
        }
        else if (DuckConstraints != null && DuckConstraints.HasRequirements)
        {
            var missing = new List<string>();
            if (DuckConstraints.RequiredMethods.Count > 0)
                missing.Add($"methods: {string.Join(", ", DuckConstraints.RequiredMethods)}");
            if (DuckConstraints.RequiredProperties.Count > 0)
                missing.Add($"properties: {string.Join(", ", DuckConstraints.RequiredProperties)}");

            return $"Type '{ActualType?.DisplayName ?? "unknown"}' does not have {string.Join(" and ", missing)}";
        }

        return IncompatibilityReason ?? "Type mismatch";
    }

    public override string ToString() => IsCompatible
        ? $"[OK] arg {ParameterIndex}: {ActualType?.DisplayName}"
        : $"[MISMATCH] arg {ParameterIndex}: {ActualType?.DisplayName} vs {string.Join("|", ExpectedTypes.Select(t => t.DisplayName))}";
}
