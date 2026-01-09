using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Parser for .gdlintrc configuration files (gdtoolkit format).
    /// </summary>
    public static class GDLintrcParser
    {
        // Rule name to RuleId mapping for gdlint compatibility
        private static readonly Dictionary<string, string> RuleNameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Naming rules
            { "class-name", "GDL001" },
            { "function-name", "GDL002" },
            { "class-variable-name", "GDL003" },
            { "function-variable-name", "GDL003" },
            { "function-argument-name", "GDL003" },
            { "loop-variable-name", "GDL003" },
            { "constant-name", "GDL004" },
            { "signal-name", "GDL005" },
            { "enum-name", "GDL006" },
            { "enum-element-name", "GDL007" },
            { "sub-class-name", "GDL009" },

            // Style rules
            { "max-line-length", "GDL101" },
            { "max-file-lines", "GDL102" },
            { "trailing-whitespace", "GDL101" }, // Not directly mapped but acknowledged
            { "no-elif-return", "GDL216" },
            { "no-else-return", "GDL217" },

            // Best practices rules
            { "unused-argument", "GDL202" },
            { "unnecessary-pass", "GDL203" },
            { "function-arguments-number", "GDL205" },
            { "comparison-with-itself", "GDL213" },
            { "duplicated-load", "GDL219" },
            { "private-method-call", "GDL218" },
            { "class-definitions-order", "GDL301" }
        };

        /// <summary>
        /// Parses a .gdlintrc file and returns GDLinterOptions.
        /// </summary>
        public static GDLinterOptions Parse(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var content = File.ReadAllText(filePath);
            return ParseContent(content);
        }

        /// <summary>
        /// Parses .gdlintrc content string and returns GDLinterOptions.
        /// </summary>
        public static GDLinterOptions ParseContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return GDLinterOptions.Default;

            var options = new GDLinterOptions();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Parse key: value pairs
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var key = trimmed.Substring(0, colonIndex).Trim();
                var value = trimmed.Substring(colonIndex + 1).Trim();

                ApplyOption(options, key, value);
            }

            return options;
        }

        private static void ApplyOption(GDLinterOptions options, string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "max-line-length":
                    if (int.TryParse(value, out var maxLineLength))
                        options.MaxLineLength = maxLineLength;
                    break;

                case "max-file-lines":
                    if (int.TryParse(value, out var maxFileLines))
                        options.MaxFileLines = maxFileLines;
                    break;

                case "function-arguments-number":
                    if (int.TryParse(value, out var maxParams))
                        options.MaxParameters = maxParams;
                    break;

                case "disable":
                    ParseDisabledRules(options, value);
                    break;

                // Naming patterns - we try to detect the convention from regex
                case "class-name":
                    options.ClassNameCase = DetectNamingCase(value, NamingCase.PascalCase);
                    break;

                case "function-name":
                    options.FunctionNameCase = DetectNamingCase(value, NamingCase.SnakeCase);
                    break;

                case "class-variable-name":
                case "function-variable-name":
                case "function-argument-name":
                case "loop-variable-name":
                    options.VariableNameCase = DetectNamingCase(value, NamingCase.SnakeCase);
                    break;

                case "constant-name":
                    options.ConstantNameCase = DetectNamingCase(value, NamingCase.ScreamingSnakeCase);
                    break;

                case "signal-name":
                    options.SignalNameCase = DetectNamingCase(value, NamingCase.SnakeCase);
                    break;

                case "enum-name":
                    options.EnumNameCase = DetectNamingCase(value, NamingCase.PascalCase);
                    break;

                case "enum-element-name":
                    options.EnumValueCase = DetectNamingCase(value, NamingCase.ScreamingSnakeCase);
                    break;

                case "sub-class-name":
                    options.InnerClassNameCase = DetectNamingCase(value, NamingCase.PascalCase);
                    break;
            }
        }

        private static void ParseDisabledRules(GDLinterOptions options, string value)
        {
            // Parse [rule1, rule2, rule3] format
            var match = Regex.Match(value, @"\[(.*)\]");
            if (!match.Success)
                return;

            var rulesStr = match.Groups[1].Value;
            var rules = rulesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rule in rules)
            {
                var ruleName = rule.Trim().Trim('"', '\'');

                // Try to map rule name to rule ID
                if (RuleNameToId.TryGetValue(ruleName, out var ruleId))
                {
                    options.DisableRule(ruleId);
                }
                else
                {
                    // Maybe it's already a rule ID
                    options.DisableRule(ruleName);
                }
            }
        }

        private static NamingCase DetectNamingCase(string regexPattern, NamingCase defaultCase)
        {
            if (string.IsNullOrEmpty(regexPattern))
                return defaultCase;

            // Keep original case for proper detection
            var pattern = regexPattern;

            // SCREAMING_SNAKE_CASE patterns - [A-Z][A-Z0-9_]* without lowercase
            if (pattern.Contains("[A-Z]") && !pattern.Contains("[a-z]") && !pattern.Contains("[a-z0-9]"))
                return NamingCase.ScreamingSnakeCase;

            // PascalCase patterns - typically ([A-Z][a-z0-9]*)+
            if (pattern.Contains("([A-Z][a-z") || pattern.Contains("[A-Z][a-z0-9]*)+"))
                return NamingCase.PascalCase;

            // snake_case patterns - typically _?[a-z][a-z0-9_]* with lowercase
            if (pattern.Contains("[a-z]") && (pattern.Contains("_") || pattern.Contains("[a-z0-9_]")))
                return NamingCase.SnakeCase;

            // camelCase patterns - starts with lowercase, then has uppercase
            if (pattern.StartsWith("[a-z]") && pattern.Contains("[A-Z]"))
                return NamingCase.CamelCase;

            return defaultCase;
        }

        /// <summary>
        /// Gets the GDShrapt rule ID for a gdlint rule name.
        /// </summary>
        public static string GetRuleId(string gdlintRuleName)
        {
            if (RuleNameToId.TryGetValue(gdlintRuleName, out var ruleId))
                return ruleId;
            return null;
        }

        /// <summary>
        /// Gets all known gdlint rule names.
        /// </summary>
        public static IEnumerable<string> GetKnownRuleNames()
        {
            return RuleNameToId.Keys;
        }
    }
}
