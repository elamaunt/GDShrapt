using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Configuration options for the GDScript formatter.
    /// </summary>
    public class GDFormatterOptions
    {
        private readonly HashSet<string> _disabledRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _enabledRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Indentation options

        /// <summary>
        /// Indentation style: Tabs or Spaces. Default: Tabs.
        /// </summary>
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Tabs;

        /// <summary>
        /// Number of spaces per indentation level (when using spaces). Default: 4.
        /// </summary>
        public int IndentSize { get; set; } = 4;

        /// <summary>
        /// The indentation pattern string (computed from IndentStyle and IndentSize).
        /// </summary>
        public string IndentPattern => IndentStyle == IndentStyle.Tabs
            ? "\t"
            : new string(' ', IndentSize);

        // Line ending options

        /// <summary>
        /// Line ending style: LF, CRLF, or Platform. Default: LF.
        /// </summary>
        public LineEndingStyle LineEnding { get; set; } = LineEndingStyle.LF;

        // Blank lines options

        /// <summary>
        /// Number of blank lines between top-level functions.
        /// GDScript style guide recommends 2. Default: 2.
        /// </summary>
        public int BlankLinesBetweenFunctions { get; set; } = 2;

        /// <summary>
        /// Number of blank lines after class declaration (extends/class_name). Default: 1.
        /// </summary>
        public int BlankLinesAfterClassDeclaration { get; set; } = 1;

        /// <summary>
        /// Number of blank lines between different member types
        /// (e.g., between variables and functions). Default: 1.
        /// </summary>
        public int BlankLinesBetweenMemberTypes { get; set; } = 1;

        // Spacing options

        /// <summary>
        /// Add space around binary operators (=, +, -, etc.). Default: true.
        /// </summary>
        public bool SpaceAroundOperators { get; set; } = true;

        /// <summary>
        /// Add space after commas in parameter lists, arrays, etc. Default: true.
        /// </summary>
        public bool SpaceAfterComma { get; set; } = true;

        /// <summary>
        /// Add space after colons in type hints (var x: int). Default: true.
        /// </summary>
        public bool SpaceAfterColon { get; set; } = true;

        /// <summary>
        /// Add space before colons in type hints (var x : int). Default: false.
        /// </summary>
        public bool SpaceBeforeColon { get; set; } = false;

        /// <summary>
        /// Add space inside parentheses ( a, b ). Default: false.
        /// </summary>
        public bool SpaceInsideParentheses { get; set; } = false;

        /// <summary>
        /// Add space inside brackets [ 1, 2 ]. Default: false.
        /// </summary>
        public bool SpaceInsideBrackets { get; set; } = false;

        /// <summary>
        /// Add space inside braces { "a": 1 }. Default: true.
        /// </summary>
        public bool SpaceInsideBraces { get; set; } = true;

        // Trailing whitespace options

        /// <summary>
        /// Remove trailing whitespace from lines. Default: true.
        /// </summary>
        public bool RemoveTrailingWhitespace { get; set; } = true;

        /// <summary>
        /// Ensure file ends with a newline. Default: true.
        /// </summary>
        public bool EnsureTrailingNewline { get; set; } = true;

        /// <summary>
        /// Remove multiple trailing newlines at end of file. Default: true.
        /// </summary>
        public bool RemoveMultipleTrailingNewlines { get; set; } = true;

        // Line length options

        /// <summary>
        /// Maximum line length (0 to disable). Default: 100.
        /// Note: The formatter doesn't auto-wrap; this is for future use.
        /// </summary>
        public int MaxLineLength { get; set; } = 100;

        // Rule management

        /// <summary>
        /// Disables a rule by its ID.
        /// </summary>
        public void DisableRule(string ruleId)
        {
            _disabledRules.Add(ruleId);
            _enabledRules.Remove(ruleId);
        }

        /// <summary>
        /// Enables a rule by its ID.
        /// </summary>
        public void EnableRule(string ruleId)
        {
            _enabledRules.Add(ruleId);
            _disabledRules.Remove(ruleId);
        }

        /// <summary>
        /// Checks if a rule is enabled.
        /// </summary>
        public bool IsRuleEnabled(GDFormatRule rule)
        {
            if (_disabledRules.Contains(rule.RuleId))
                return false;

            if (_enabledRules.Contains(rule.RuleId))
                return true;

            return rule.EnabledByDefault;
        }

        // Presets

        /// <summary>
        /// Default options.
        /// </summary>
        public static GDFormatterOptions Default => new GDFormatterOptions();

        /// <summary>
        /// Options following the official GDScript style guide.
        /// </summary>
        public static GDFormatterOptions GDScriptStyleGuide => new GDFormatterOptions
        {
            IndentStyle = IndentStyle.Tabs,
            BlankLinesBetweenFunctions = 2,
            BlankLinesAfterClassDeclaration = 1,
            BlankLinesBetweenMemberTypes = 1,
            SpaceAroundOperators = true,
            SpaceAfterComma = true,
            SpaceAfterColon = true,
            SpaceBeforeColon = false,
            RemoveTrailingWhitespace = true,
            EnsureTrailingNewline = true
        };

        /// <summary>
        /// Minimal formatting options - only essential cleanup.
        /// </summary>
        public static GDFormatterOptions Minimal => new GDFormatterOptions
        {
            RemoveTrailingWhitespace = true,
            EnsureTrailingNewline = true,
            RemoveMultipleTrailingNewlines = true
        };
    }

    /// <summary>
    /// Indentation style options.
    /// </summary>
    public enum IndentStyle
    {
        /// <summary>
        /// Use tabs for indentation.
        /// </summary>
        Tabs,

        /// <summary>
        /// Use spaces for indentation.
        /// </summary>
        Spaces
    }

    /// <summary>
    /// Line ending style options.
    /// </summary>
    public enum LineEndingStyle
    {
        /// <summary>
        /// Unix style line endings (\n).
        /// </summary>
        LF,

        /// <summary>
        /// Windows style line endings (\r\n).
        /// </summary>
        CRLF,

        /// <summary>
        /// Use the platform's default line ending.
        /// </summary>
        Platform
    }
}
