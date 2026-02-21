using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Validates a .gdshrapt.json configuration file for errors and warnings.
/// </summary>
public static class GDConfigValidator
{
    private static readonly HashSet<string> ValidNullableStrictness = new(StringComparer.OrdinalIgnoreCase)
        { "error", "strict", "normal", "relaxed", "off" };

    private static readonly HashSet<string> ValidAbstractMethodPosition = new(StringComparer.OrdinalIgnoreCase)
        { "first", "last", "none" };

    private static readonly HashSet<string> ValidPrivateMethodPosition = new(StringComparer.OrdinalIgnoreCase)
        { "after_public", "before_public", "none" };

    private static readonly HashSet<string> ValidStaticMethodPosition = new(StringComparer.OrdinalIgnoreCase)
        { "first", "after_constants", "none" };

    /// <summary>
    /// Validates a configuration file and returns a result with errors/warnings.
    /// </summary>
    public static GDConfigValidationResult Validate(string configPath, bool includeExplanations = false)
    {
        var result = new GDConfigValidationResult();

        if (!File.Exists(configPath))
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Configuration file not found: {configPath}",
                Explanation = includeExplanations
                    ? "Create a configuration file with 'gdshrapt config init'."
                    : null
            });
            return result;
        }

        // 1. JSON schema validation
        string json;
        GDProjectConfig? config;
        try
        {
            json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<GDProjectConfig>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            });
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid JSON: {ex.Message}",
                Explanation = includeExplanations
                    ? "The configuration file must be valid JSON. Check for missing commas, brackets, or quotes."
                    : null
            });
            return result;
        }

        if (config == null)
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = "Configuration deserialized to null"
            });
            return result;
        }

        // 2. Range validation
        ValidateRanges(config, result, includeExplanations);

        // 3. Enum value validation
        ValidateEnumValues(config, result, includeExplanations);

        // 4. Conflict detection (warnings only)
        ValidateConflicts(config, result, includeExplanations);

        return result;
    }

    private static void ValidateRanges(GDProjectConfig config, GDConfigValidationResult result, bool explain)
    {
        // Linting
        ValidateNonNegative(result, "linting.maxLineLength", config.Linting.MaxLineLength, explain);
        ValidatePositive(result, "linting.tabWidth", config.Linting.TabWidth, explain);

        // Advanced linting
        ValidateNonNegative(result, "advancedLinting.maxCyclomaticComplexity", config.AdvancedLinting.MaxCyclomaticComplexity, explain);
        ValidateNonNegative(result, "advancedLinting.maxFunctionLength", config.AdvancedLinting.MaxFunctionLength, explain);
        ValidateNonNegative(result, "advancedLinting.maxParameters", config.AdvancedLinting.MaxParameters, explain);
        ValidateNonNegative(result, "advancedLinting.maxNestingDepth", config.AdvancedLinting.MaxNestingDepth, explain);
        ValidateNonNegative(result, "advancedLinting.maxFileLines", config.AdvancedLinting.MaxFileLines, explain);
        ValidateNonNegative(result, "advancedLinting.maxLocalVariables", config.AdvancedLinting.MaxLocalVariables, explain);
        ValidateNonNegative(result, "advancedLinting.maxPublicMethods", config.AdvancedLinting.MaxPublicMethods, explain);
        ValidateNonNegative(result, "advancedLinting.maxReturns", config.AdvancedLinting.MaxReturns, explain);
        ValidateNonNegative(result, "advancedLinting.maxBranches", config.AdvancedLinting.MaxBranches, explain);
        ValidateNonNegative(result, "advancedLinting.maxBooleanExpressions", config.AdvancedLinting.MaxBooleanExpressions, explain);
        ValidateNonNegative(result, "advancedLinting.maxInnerClasses", config.AdvancedLinting.MaxInnerClasses, explain);
        ValidateNonNegative(result, "advancedLinting.maxClassVariables", config.AdvancedLinting.MaxClassVariables, explain);

        // Formatter
        ValidatePositive(result, "formatter.indentSize", config.Formatter.IndentSize, explain);
        ValidateNonNegative(result, "formatter.maxLineLength", config.Formatter.MaxLineLength, explain);
    }

    private static void ValidateEnumValues(GDProjectConfig config, GDConfigValidationResult result, bool explain)
    {
        if (!ValidNullableStrictness.Contains(config.Validation.NullableStrictness))
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for validation.nullableStrictness: '{config.Validation.NullableStrictness}'",
                Explanation = explain
                    ? $"Valid values: {string.Join(", ", ValidNullableStrictness)}"
                    : null
            });
        }

        if (!ValidAbstractMethodPosition.Contains(config.AdvancedLinting.AbstractMethodPosition))
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for advancedLinting.abstractMethodPosition: '{config.AdvancedLinting.AbstractMethodPosition}'",
                Explanation = explain
                    ? $"Valid values: {string.Join(", ", ValidAbstractMethodPosition)}"
                    : null
            });
        }

        if (!ValidPrivateMethodPosition.Contains(config.AdvancedLinting.PrivateMethodPosition))
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for advancedLinting.privateMethodPosition: '{config.AdvancedLinting.PrivateMethodPosition}'",
                Explanation = explain
                    ? $"Valid values: {string.Join(", ", ValidPrivateMethodPosition)}"
                    : null
            });
        }

        if (!ValidStaticMethodPosition.Contains(config.AdvancedLinting.StaticMethodPosition))
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for advancedLinting.staticMethodPosition: '{config.AdvancedLinting.StaticMethodPosition}'",
                Explanation = explain
                    ? $"Valid values: {string.Join(", ", ValidStaticMethodPosition)}"
                    : null
            });
        }
    }

    private static void ValidateConflicts(GDProjectConfig config, GDConfigValidationResult result, bool explain)
    {
        // Linting disabled but advanced linting configured
        if (!config.Linting.Enabled)
        {
            bool hasAdvancedWarnings =
                config.AdvancedLinting.WarnUnusedVariables ||
                config.AdvancedLinting.WarnUnusedParameters ||
                config.AdvancedLinting.WarnUnusedSignals ||
                config.AdvancedLinting.WarnEmptyFunctions ||
                config.AdvancedLinting.WarnMagicNumbers ||
                config.AdvancedLinting.WarnVariableShadowing;

            if (hasAdvancedWarnings)
            {
                result.Errors.Add(new GDConfigValidationError
                {
                    Severity = "warning",
                    Message = "Linting is disabled but advanced linting warnings are configured",
                    Explanation = explain
                        ? "Advanced linting rules won't run when linting.enabled is false. Either enable linting or remove the warning flags."
                        : null
                });
            }
        }

        // Conflicting line lengths: warn if linting limit is stricter than formatter limit
        // (formatter may produce lines that the linter then rejects)
        if (config.Linting.MaxLineLength > 0 && config.Formatter.MaxLineLength > 0
            && config.Linting.MaxLineLength < config.Formatter.MaxLineLength)
        {
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "warning",
                Message = $"Conflicting max line lengths: linting={config.Linting.MaxLineLength}, formatter={config.Formatter.MaxLineLength}",
                Explanation = explain
                    ? "The formatter's max line length is greater than the linter's. The formatter may produce lines that the linter reports as too long."
                    : null
            });
        }
    }

    private static void ValidateNonNegative(GDConfigValidationResult result, string path, int value, bool explain)
    {
        if (value < 0)
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for {path}: {value} (must be >= 0)",
                Explanation = explain
                    ? $"Set {path} to 0 to disable the limit, or a positive value to enable it."
                    : null
            });
        }
    }

    private static void ValidatePositive(GDConfigValidationResult result, string path, int value, bool explain)
    {
        if (value <= 0)
        {
            result.IsValid = false;
            result.Errors.Add(new GDConfigValidationError
            {
                Severity = "error",
                Message = $"Invalid value for {path}: {value} (must be > 0)",
                Explanation = explain
                    ? $"The value of {path} must be a positive integer."
                    : null
            });
        }
    }
}

/// <summary>
/// Result of configuration validation.
/// </summary>
public class GDConfigValidationResult
{
    /// <summary>
    /// Whether the configuration is valid (no errors).
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of errors and warnings found.
    /// </summary>
    public List<GDConfigValidationError> Errors { get; } = new();

    /// <summary>
    /// Whether there are any warnings (non-error issues).
    /// </summary>
    public bool HasWarnings => Errors.Exists(e => e.Severity == "warning");
}

/// <summary>
/// A single validation error or warning.
/// </summary>
public class GDConfigValidationError
{
    /// <summary>
    /// Severity: "error" or "warning".
    /// </summary>
    public string Severity { get; init; } = "error";

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; init; } = "";

    /// <summary>
    /// Detailed explanation (only when --explain is used).
    /// </summary>
    public string? Explanation { get; init; }
}
