using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// GDScript formatter that applies formatting rules to code.
    /// </summary>
    public class GDFormatter
    {
        private readonly List<GDFormatRule> _rules = new List<GDFormatRule>();
        private readonly GDScriptReader _reader = new GDScriptReader();

        /// <summary>
        /// Formatter options.
        /// </summary>
        public GDFormatterOptions Options { get; set; } = GDFormatterOptions.Default;

        /// <summary>
        /// All registered rules.
        /// </summary>
        public IReadOnlyList<GDFormatRule> Rules => _rules;

        /// <summary>
        /// Creates a new formatter with default rules.
        /// </summary>
        public GDFormatter()
        {
            RegisterDefaultRules();
        }

        /// <summary>
        /// Creates a new formatter with specified options and default rules.
        /// </summary>
        public GDFormatter(GDFormatterOptions options) : this()
        {
            Options = options ?? GDFormatterOptions.Default;
        }

        /// <summary>
        /// Creates a new formatter without any rules.
        /// Use AddRule to add custom rules.
        /// </summary>
        public static GDFormatter CreateEmpty()
        {
            var formatter = new GDFormatter();
            formatter._rules.Clear();
            return formatter;
        }

        /// <summary>
        /// Registers the default formatting rules.
        /// </summary>
        private void RegisterDefaultRules()
        {
            // GDIndentationFormatRule - Uses ConvertPattern for idempotent indentation
            AddRule(new GDIndentationFormatRule());

            // GDBlankLinesFormatRule - Uses state stack for inner class handling
            AddRule(new GDBlankLinesFormatRule());

            // GDSpacingFormatRule - Handles operator, colon, comma spacing
            AddRule(new GDSpacingFormatRule());

            // GDTrailingWhitespaceFormatRule - Removes trailing spaces from lines
            // EOF newline handling moved to FormatCode post-processing
            AddRule(new GDTrailingWhitespaceFormatRule());

            // GDNewLineFormatRule - Placeholder for future line ending rules
            AddRule(new GDNewLineFormatRule());

            // GDLineWrapFormatRule - Wraps long lines exceeding MaxLineLength
            AddRule(new GDLineWrapFormatRule());

            // GDCodeReorderFormatRule - Reorders class members (opt-in, disabled by default)
            AddRule(new GDCodeReorderFormatRule());
        }

        /// <summary>
        /// Adds a custom rule to the formatter.
        /// </summary>
        public void AddRule(GDFormatRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            _rules.Add(rule);
        }

        /// <summary>
        /// Removes a rule by its ID.
        /// </summary>
        public bool RemoveRule(string ruleId)
        {
            return _rules.RemoveAll(r => r.RuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Gets a rule by its ID.
        /// </summary>
        public GDFormatRule GetRule(string ruleId)
        {
            return _rules.FirstOrDefault(r => r.RuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Formats a parsed GDScript node in-place.
        /// </summary>
        public GDNode Format(GDNode node)
        {
            if (node == null)
                return null;

            foreach (var rule in _rules)
            {
                if (Options.IsRuleEnabled(rule))
                {
                    rule.Run(node, Options);
                }
            }

            return node;
        }

        /// <summary>
        /// Parses and formats GDScript source code.
        /// </summary>
        public string FormatCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            var tree = _reader.ParseFileContent(code);
            Format(tree);
            var result = tree.ToString();

            // Apply string-based post-processing
            result = HandleTrailingNewlines(result);
            result = ConvertLineEndings(result);

            return result;
        }

        /// <summary>
        /// Checks if code is already properly formatted without modifying it.
        /// </summary>
        /// <returns>True if code is already formatted, false otherwise</returns>
        public bool IsFormatted(string code)
        {
            if (string.IsNullOrEmpty(code))
                return true;

            var formatted = FormatCode(code);
            return formatted == code;
        }

        /// <summary>
        /// Gets detailed formatting check result.
        /// </summary>
        public FormatCheckResult Check(string code)
        {
            if (string.IsNullOrEmpty(code))
                return FormatCheckResult.AlreadyFormatted(code ?? string.Empty);

            var formatted = FormatCode(code);

            if (formatted == code)
                return FormatCheckResult.AlreadyFormatted(code);

            return FormatCheckResult.NeedsFormatting(code, formatted);
        }

        /// <summary>
        /// Handles trailing newlines at end of file as string post-processing.
        /// This is more reliable than AST-level manipulation because the AST
        /// has nested forms that make it difficult to determine the true EOF.
        /// </summary>
        private string HandleTrailingNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove multiple trailing newlines if requested
            if (Options.RemoveMultipleTrailingNewlines)
            {
                // Trim trailing newlines and whitespace
                int endIndex = text.Length;
                while (endIndex > 0)
                {
                    char c = text[endIndex - 1];
                    if (c == '\n' || c == '\r' || c == ' ' || c == '\t')
                        endIndex--;
                    else
                        break;
                }

                if (endIndex < text.Length)
                {
                    text = text.Substring(0, endIndex);
                }
            }

            // Ensure trailing newline if requested
            if (Options.EnsureTrailingNewline)
            {
                if (text.Length > 0 && !text.EndsWith("\n"))
                {
                    text += "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// Converts line endings in the output string based on options.
        /// </summary>
        private string ConvertLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            switch (Options.LineEnding)
            {
                case LineEndingStyle.CRLF:
                    // First normalize to LF, then convert to CRLF
                    return text.Replace("\r\n", "\n").Replace("\n", "\r\n");

                case LineEndingStyle.Platform:
                    // First normalize to LF, then convert to platform
                    return text.Replace("\r\n", "\n").Replace("\n", System.Environment.NewLine);

                case LineEndingStyle.LF:
                default:
                    // Normalize to LF (remove any \r)
                    return text.Replace("\r\n", "\n").Replace("\r", "");
            }
        }

        /// <summary>
        /// Formats code using style extracted from a sample code.
        /// </summary>
        public string FormatCodeWithStyle(string code, string sampleCode)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            if (string.IsNullOrEmpty(sampleCode))
                return FormatCode(code);

            var extractor = new GDFormatterStyleExtractor();
            var extractedOptions = extractor.ExtractStyleFromCode(sampleCode);

            // Merge extracted options with current options
            var mergedOptions = MergeOptions(Options, extractedOptions);
            var originalOptions = Options;

            try
            {
                Options = mergedOptions;
                return FormatCode(code);
            }
            finally
            {
                Options = originalOptions;
            }
        }

        /// <summary>
        /// Formats a GDScript expression.
        /// </summary>
        public string FormatExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return expression;

            var expr = _reader.ParseExpression(expression);
            Format(expr);
            return expr.ToString();
        }

        /// <summary>
        /// Gets all enabled rules.
        /// </summary>
        public IEnumerable<GDFormatRule> GetEnabledRules()
        {
            return _rules.Where(r => Options.IsRuleEnabled(r));
        }

        /// <summary>
        /// Gets all disabled rules.
        /// </summary>
        public IEnumerable<GDFormatRule> GetDisabledRules()
        {
            return _rules.Where(r => !Options.IsRuleEnabled(r));
        }

        /// <summary>
        /// Merges base options with extracted options, preferring extracted values.
        /// </summary>
        private GDFormatterOptions MergeOptions(GDFormatterOptions baseOptions, GDFormatterOptions extractedOptions)
        {
            if (extractedOptions == null)
                return baseOptions;

            return new GDFormatterOptions
            {
                // Use extracted style where detected, fall back to base
                IndentStyle = extractedOptions.IndentStyle,
                IndentSize = extractedOptions.IndentSize,
                LineEnding = baseOptions.LineEnding, // Keep base line ending preference
                BlankLinesBetweenFunctions = extractedOptions.BlankLinesBetweenFunctions,
                BlankLinesAfterClassDeclaration = extractedOptions.BlankLinesAfterClassDeclaration,
                BlankLinesBetweenMemberTypes = extractedOptions.BlankLinesBetweenMemberTypes,
                SpaceAroundOperators = extractedOptions.SpaceAroundOperators,
                SpaceAfterComma = extractedOptions.SpaceAfterComma,
                SpaceAfterColon = extractedOptions.SpaceAfterColon,
                SpaceBeforeColon = extractedOptions.SpaceBeforeColon,
                SpaceInsideParentheses = extractedOptions.SpaceInsideParentheses,
                SpaceInsideBrackets = extractedOptions.SpaceInsideBrackets,
                SpaceInsideBraces = extractedOptions.SpaceInsideBraces,
                RemoveTrailingWhitespace = baseOptions.RemoveTrailingWhitespace,
                EnsureTrailingNewline = baseOptions.EnsureTrailingNewline,
                RemoveMultipleTrailingNewlines = baseOptions.RemoveMultipleTrailingNewlines,
                MaxLineLength = baseOptions.MaxLineLength,
                // Line wrapping options - keep base settings
                WrapLongLines = baseOptions.WrapLongLines,
                LineWrapStyle = baseOptions.LineWrapStyle,
                ContinuationIndentSize = baseOptions.ContinuationIndentSize,
                UseBackslashContinuation = baseOptions.UseBackslashContinuation,
                // Code reordering - use extracted if available
                ReorderCode = baseOptions.ReorderCode,
                MemberOrder = extractedOptions.MemberOrder?.Count > 0
                    ? extractedOptions.MemberOrder
                    : baseOptions.MemberOrder
            };
        }
    }
}
