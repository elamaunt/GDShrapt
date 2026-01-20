using System;
using System.Collections.Generic;
using System.IO;

namespace GDShrapt.Semantics;

/// <summary>
/// Parser for gdtoolkit's .gdlintrc file format.
/// Provides compatibility with existing gdtoolkit configurations.
/// Format is INI-like:
/// [section]
/// key = value
/// </summary>
public static class GDGdlintConfigParser
{
    /// <summary>
    /// Default filename for gdtoolkit config.
    /// </summary>
    public const string ConfigFileName = ".gdlintrc";

    /// <summary>
    /// Alternative filename for gdtoolkit config.
    /// </summary>
    public const string AltConfigFileName = ".gdlint.cfg";

    /// <summary>
    /// Parses a .gdlintrc file and converts it to GDProjectConfig.
    /// </summary>
    /// <param name="filePath">Path to the .gdlintrc file.</param>
    /// <returns>Parsed configuration, or null if parsing fails.</returns>
    public static GDProjectConfig? Parse(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var lines = File.ReadAllLines(filePath);
            return ParseLines(lines);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses .gdlintrc content from lines.
    /// </summary>
    private static GDProjectConfig ParseLines(string[] lines)
    {
        var config = new GDProjectConfig();
        var currentSection = "";
        var disabledRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            // Check for section header [section]
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                continue;
            }

            // Parse key = value
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
                continue;

            var key = line.Substring(0, equalsIndex).Trim().ToLowerInvariant();
            var value = line.Substring(equalsIndex + 1).Trim();

            // Remove quotes from value
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                value = value.Substring(1, value.Length - 2);
            }

            ProcessKeyValue(config, currentSection, key, value, disabledRules);
        }

        // Apply disabled rules
        foreach (var ruleId in disabledRules)
        {
            var mappedId = MapGdlintRuleId(ruleId);
            if (mappedId != null && !config.Linting.Rules.ContainsKey(mappedId))
            {
                config.Linting.Rules[mappedId] = new GDRuleConfig { Enabled = false };
            }
        }

        return config;
    }

    private static void ProcessKeyValue(
        GDProjectConfig config,
        string section,
        string key,
        string value,
        HashSet<string> disabledRules)
    {
        switch (section)
        {
            case "":
            case "general":
                ProcessGeneralKey(config, key, value, disabledRules);
                break;
            case "naming":
                ProcessNamingKey(config, key, value);
                break;
            case "format":
            case "formatting":
                ProcessFormattingKey(config, key, value);
                break;
        }
    }

    private static void ProcessGeneralKey(
        GDProjectConfig config,
        string key,
        string value,
        HashSet<string> disabledRules)
    {
        switch (key)
        {
            case "disabled":
            case "disable":
                // Comma-separated list of disabled rules
                foreach (var rule in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    disabledRules.Add(rule);
                }
                break;

            case "exclude":
            case "excluded":
                // Comma-separated list of excluded paths
                foreach (var path in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    config.Cli.Exclude.Add(path);
                }
                break;

            case "max-line-length":
            case "max_line_length":
                if (int.TryParse(value, out var maxLineLength))
                    config.Linting.MaxLineLength = maxLineLength;
                break;

            case "max-file-lines":
            case "max_file_lines":
                if (int.TryParse(value, out var maxFileLines))
                    config.AdvancedLinting.MaxFileLines = maxFileLines;
                break;

            case "max-function-length":
            case "max_function_length":
                if (int.TryParse(value, out var maxFuncLength))
                    config.AdvancedLinting.MaxFunctionLength = maxFuncLength;
                break;

            case "max-arguments":
            case "max_arguments":
            case "max-parameters":
            case "max_parameters":
                if (int.TryParse(value, out var maxParams))
                    config.AdvancedLinting.MaxParameters = maxParams;
                break;

            case "max-complexity":
            case "max_complexity":
            case "max-cyclomatic-complexity":
            case "max_cyclomatic_complexity":
                if (int.TryParse(value, out var maxComplexity))
                    config.AdvancedLinting.MaxCyclomaticComplexity = maxComplexity;
                break;

            case "tab-width":
            case "tab_width":
            case "indent-size":
            case "indent_size":
                if (int.TryParse(value, out var tabWidth))
                {
                    config.Formatter.IndentSize = tabWidth;
                    config.Linting.TabWidth = tabWidth;
                }
                break;

            case "use-tabs":
            case "use_tabs":
            case "indent-type":
            case "indent_type":
                if (bool.TryParse(value, out var useTabs))
                {
                    config.Formatter.IndentStyle = useTabs ? GDIndentationStyle.Tabs : GDIndentationStyle.Spaces;
                    config.Linting.IndentationStyle = useTabs ? GDIndentationStyle.Tabs : GDIndentationStyle.Spaces;
                }
                else if (value.Equals("tabs", StringComparison.OrdinalIgnoreCase) || value == "tab")
                {
                    config.Formatter.IndentStyle = GDIndentationStyle.Tabs;
                    config.Linting.IndentationStyle = GDIndentationStyle.Tabs;
                }
                else if (value.Equals("spaces", StringComparison.OrdinalIgnoreCase) || value == "space")
                {
                    config.Formatter.IndentStyle = GDIndentationStyle.Spaces;
                    config.Linting.IndentationStyle = GDIndentationStyle.Spaces;
                }
                break;
        }
    }

    private static void ProcessNamingKey(GDProjectConfig config, string key, string value)
    {
        var namingCase = ParseNamingCase(value);

        switch (key)
        {
            case "function-name":
            case "function_name":
            case "function-naming":
            case "function_naming":
                config.AdvancedLinting.FunctionNameCase = namingCase;
                break;

            case "class-name":
            case "class_name":
            case "class-naming":
            case "class_naming":
                config.AdvancedLinting.ClassNameCase = namingCase;
                break;

            case "variable-name":
            case "variable_name":
            case "variable-naming":
            case "variable_naming":
                config.AdvancedLinting.VariableNameCase = namingCase;
                break;

            case "constant-name":
            case "constant_name":
            case "constant-naming":
            case "constant_naming":
                config.AdvancedLinting.ConstantNameCase = namingCase;
                break;

            case "signal-name":
            case "signal_name":
            case "signal-naming":
            case "signal_naming":
                config.AdvancedLinting.SignalNameCase = namingCase;
                break;

            case "enum-name":
            case "enum_name":
            case "enum-naming":
            case "enum_naming":
                config.AdvancedLinting.EnumNameCase = namingCase;
                break;

            case "enum-element-name":
            case "enum_element_name":
            case "enum-value-naming":
            case "enum_value_naming":
                config.AdvancedLinting.EnumValueCase = namingCase;
                break;
        }
    }

    private static void ProcessFormattingKey(GDProjectConfig config, string key, string value)
    {
        switch (key)
        {
            case "indent-size":
            case "indent_size":
                if (int.TryParse(value, out var indentSize))
                    config.Formatter.IndentSize = indentSize;
                break;

            case "use-tabs":
            case "use_tabs":
                if (bool.TryParse(value, out var useTabs))
                    config.Formatter.IndentStyle = useTabs ? GDIndentationStyle.Tabs : GDIndentationStyle.Spaces;
                break;

            case "max-line-length":
            case "max_line_length":
                if (int.TryParse(value, out var maxLineLength))
                    config.Linting.MaxLineLength = maxLineLength;
                break;
        }
    }

    private static GDNamingCase ParseNamingCase(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "snake" or "snake_case" => GDNamingCase.SnakeCase,
            "pascal" or "pascalcase" or "pascal_case" => GDNamingCase.PascalCase,
            "camel" or "camelcase" or "camel_case" => GDNamingCase.CamelCase,
            "screaming" or "screaming_snake" or "screaming_snake_case" or "upper" or "uppercase" => GDNamingCase.ScreamingSnakeCase,
            "any" or "none" or "ignore" => GDNamingCase.Any,
            _ => GDNamingCase.Any
        };
    }

    /// <summary>
    /// Maps gdtoolkit rule names to GDShrapt rule IDs.
    /// </summary>
    private static string? MapGdlintRuleId(string gdlintRule)
    {
        // gdtoolkit uses string names, GDShrapt uses GDLxxx codes
        // This maps common gdtoolkit rule names to GDShrapt equivalents
        return gdlintRule.ToLowerInvariant() switch
        {
            "function-name" or "function_name" => "GDL001",
            "class-name" or "class_name" => "GDL002",
            "variable-name" or "variable_name" => "GDL003",
            "constant-name" or "constant_name" => "GDL004",
            "signal-name" or "signal_name" => "GDL005",
            "enum-name" or "enum_name" => "GDL006",
            "enum-element-name" or "enum_element_name" => "GDL007",
            "max-line-length" or "max_line_length" => "GDL101",
            "max-file-lines" or "max_file_lines" => "GDL102",
            "max-public-methods" or "max_public_methods" => "GDL103",
            "max-returns" or "max_returns" => "GDL104",
            "max-nesting-depth" or "max_nesting_depth" => "GDL105",
            "max-local-variables" or "max_local_variables" => "GDL106",
            "max-class-variables" or "max_class_variables" => "GDL107",
            "max-function-length" or "max_function_length" => "GDL108",
            "cyclomatic-complexity" or "cyclomatic_complexity" or "max-complexity" or "max_complexity" => "GDL109",
            "max-parameters" or "max_parameters" or "max-arguments" or "max_arguments" => "GDL110",
            "unused-variable" or "unused_variable" => "GDL201",
            "unused-parameter" or "unused_parameter" => "GDL202",
            "unused-signal" or "unused_signal" => "GDL203",
            "empty-function" or "empty_function" => "GDL204",
            "magic-number" or "magic_number" => "GDL205",
            "variable-shadowing" or "variable_shadowing" => "GDL206",
            "await-in-loop" or "await_in_loop" => "GDL207",
            "no-elif-return" or "no_elif_return" => "GDL208",
            "no-else-return" or "no_else_return" => "GDL209",
            "private-method-call" or "private_method_call" => "GDL210",
            "duplicated-load" or "duplicated_load" => "GDL211",
            "trailing-whitespace" or "trailing_whitespace" => "GDL301",
            "mixed-indentation" or "mixed_indentation" => "GDL302",
            "trailing-comma" or "trailing_comma" => "GDL303",
            _ => null // Unknown rule, ignore
        };
    }
}
