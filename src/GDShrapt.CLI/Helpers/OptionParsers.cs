using System;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI;

/// <summary>
/// Parsers for CLI option values.
/// </summary>
public static class OptionParsers
{
    /// <summary>
    /// Parses a naming case string to NamingCase enum.
    /// </summary>
    public static NamingCase ParseNamingCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NamingCase.SnakeCase;

        return value.ToLowerInvariant() switch
        {
            "pascal" or "pascalcase" => NamingCase.PascalCase,
            "snake" or "snakecase" or "snake_case" => NamingCase.SnakeCase,
            "camel" or "camelcase" => NamingCase.CamelCase,
            "screaming" or "screamingsnake" or "screaming_snake_case" => NamingCase.ScreamingSnakeCase,
            "any" => NamingCase.Any,
            _ => NamingCase.SnakeCase
        };
    }

    /// <summary>
    /// Parses an indent style string to IndentStyle enum.
    /// </summary>
    public static IndentStyle ParseIndentStyle(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "spaces" or "space" => IndentStyle.Spaces,
            "tabs" or "tab" => IndentStyle.Tabs,
            _ => IndentStyle.Tabs
        };
    }

    /// <summary>
    /// Parses a line ending string to LineEndingStyle enum.
    /// </summary>
    public static LineEndingStyle ParseLineEnding(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "lf" or "unix" => LineEndingStyle.LF,
            "crlf" or "windows" => LineEndingStyle.CRLF,
            "platform" or "auto" => LineEndingStyle.Platform,
            _ => LineEndingStyle.LF
        };
    }

    /// <summary>
    /// Parses a severity string to GDLintSeverity enum.
    /// </summary>
    public static GDLintSeverity? ParseSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "error" => GDLintSeverity.Error,
            "warning" or "warn" => GDLintSeverity.Warning,
            "info" or "information" => GDLintSeverity.Info,
            "hint" => GDLintSeverity.Hint,
            "off" or "none" or "disable" => null,
            _ => null
        };
    }

    /// <summary>
    /// Parses a comma-separated category string to GDLintCategory array.
    /// </summary>
    public static GDLintCategory[] ParseCategories(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Array.Empty<GDLintCategory>();

        return category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToLowerInvariant() switch
            {
                "naming" => GDLintCategory.Naming,
                "style" => GDLintCategory.Style,
                "best-practices" or "bestpractices" => GDLintCategory.BestPractices,
                "organization" => GDLintCategory.Organization,
                "documentation" => GDLintCategory.Documentation,
                _ => GDLintCategory.Naming
            }).ToArray();
    }

    /// <summary>
    /// Parses a comma-separated validation checks string to GDValidationChecks flags.
    /// </summary>
    public static GDValidationChecks ParseValidationChecks(string? checks)
    {
        if (string.IsNullOrWhiteSpace(checks) || checks.Equals("all", StringComparison.OrdinalIgnoreCase))
            return GDValidationChecks.All;

        if (checks.Equals("basic", StringComparison.OrdinalIgnoreCase))
            return GDValidationChecks.Basic;

        var result = GDValidationChecks.None;
        var parts = checks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            result |= part.ToLowerInvariant() switch
            {
                // Basic checks
                "syntax" => GDValidationChecks.Syntax,
                "scope" => GDValidationChecks.Scope,
                "types" => GDValidationChecks.Types,
                "calls" => GDValidationChecks.Calls,
                "controlflow" or "control-flow" => GDValidationChecks.ControlFlow,
                "indentation" => GDValidationChecks.Indentation,
                // Advanced checks
                "memberaccess" or "member-access" => GDValidationChecks.MemberAccess,
                "abstract" => GDValidationChecks.Abstract,
                "signals" => GDValidationChecks.Signals,
                "resourcepaths" or "resource-paths" => GDValidationChecks.ResourcePaths,
                _ => GDValidationChecks.None
            };
        }

        return result == GDValidationChecks.None ? GDValidationChecks.All : result;
    }

    /// <summary>
    /// Parses a line wrap style string to LineWrapStyle enum.
    /// </summary>
    public static LineWrapStyle ParseLineWrapStyle(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "afteropen" or "afteropening" or "afteropeningbracket" => LineWrapStyle.AfterOpeningBracket,
            "before" or "beforeelements" => LineWrapStyle.BeforeElements,
            _ => LineWrapStyle.AfterOpeningBracket
        };
    }
}
