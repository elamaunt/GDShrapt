using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Base class for text-based lint rules that analyze raw source code content
    /// rather than using AST visitor pattern.
    /// </summary>
    public abstract class GDTextBasedLintRule : GDLintRule
    {
        private GDLintResult _result;
        private GDLinterOptions _options;
        private string _content;

        /// <summary>
        /// Current source code content being analyzed.
        /// </summary>
        protected string Content => _content;

        /// <summary>
        /// Current linter options (override to use new field).
        /// </summary>
        protected new GDLinterOptions Options => _options;

        /// <summary>
        /// Runs this text-based rule on the given node.
        /// Extracts source content and calls AnalyzeContent.
        /// </summary>
        internal override void Run(GDNode node, GDLintResult result, GDLinterOptions options)
        {
            _result = result;
            _options = options;

            if (node == null)
                return;

            // Extract source content from the node
            _content = node.ToString();
            if (string.IsNullOrEmpty(_content))
                return;

            AnalyzeContent(_content);
        }

        /// <summary>
        /// Analyzes the source code content. Override this method in derived classes.
        /// </summary>
        /// <param name="content">The source code content to analyze.</param>
        protected abstract void AnalyzeContent(string content);

        /// <summary>
        /// Reports an issue at a specific location (1-based line and column).
        /// </summary>
        protected void ReportIssueAt(string message, int line, int column, string suggestion = null)
        {
            ReportIssueAt(DefaultSeverity, message, line, column, line, column, suggestion);
        }

        /// <summary>
        /// Reports an issue at a specific span (1-based line and column).
        /// </summary>
        protected void ReportIssueAt(string message, int startLine, int startColumn, int endLine, int endColumn, string suggestion = null)
        {
            ReportIssueAt(DefaultSeverity, message, startLine, startColumn, endLine, endColumn, suggestion);
        }

        /// <summary>
        /// Reports an issue with a specific severity at a location (1-based line and column).
        /// </summary>
        protected void ReportIssueAt(GDLintSeverity severity, string message, int startLine, int startColumn, int endLine, int endColumn, string suggestion = null)
        {
            _result?.AddIssue(new GDLintIssue(
                RuleId,
                Name,
                severity,
                Category,
                message,
                startLine,
                startColumn,
                endLine,
                endColumn,
                suggestion));
        }

        /// <summary>
        /// Splits content into lines.
        /// </summary>
        protected static string[] SplitLines(string content)
        {
            return content.Split('\n');
        }

        /// <summary>
        /// Gets the line ending used in the content.
        /// </summary>
        protected static string DetectLineEnding(string content)
        {
            if (content.Contains("\r\n"))
                return "\r\n";
            if (content.Contains("\n"))
                return "\n";
            return "\n";
        }

        /// <summary>
        /// Counts leading whitespace characters.
        /// </summary>
        protected static int CountLeadingWhitespace(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t')
                    count++;
                else
                    break;
            }
            return count;
        }

        /// <summary>
        /// Gets the indentation string from the start of a line.
        /// </summary>
        protected static string GetIndentation(string line)
        {
            int count = CountLeadingWhitespace(line);
            return line.Substring(0, count);
        }

        /// <summary>
        /// Checks if a line contains only whitespace.
        /// </summary>
        protected static bool IsBlankLine(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        /// <summary>
        /// Checks if a line is a comment.
        /// </summary>
        protected static bool IsComment(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("#");
        }

        /// <summary>
        /// Gets trailing whitespace from a line.
        /// </summary>
        protected static string GetTrailingWhitespace(string line)
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Length < line.Length)
            {
                return line.Substring(trimmed.Length);
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if line contains code (not blank, not comment).
        /// </summary>
        protected static bool IsCodeLine(string line)
        {
            var trimmed = line.TrimStart();
            return !string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#");
        }

        /// <summary>
        /// Computes a set of line indices (0-based) that start inside a multiline string literal.
        /// Covers triple-quoted strings (""") and regular strings that span lines when left unclosed.
        /// </summary>
        protected static HashSet<int> GetLinesInsideStrings(string[] lines)
        {
            var result = new HashSet<int>();
            bool inTripleQuote = false;
            bool inRegularString = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (inTripleQuote || inRegularString)
                    result.Add(i);

                var line = lines[i];

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];

                    if (c == '\\' && (inRegularString || inTripleQuote))
                    {
                        j++;
                        continue;
                    }

                    if (c == '#' && !inRegularString && !inTripleQuote)
                        break;

                    if (c == '"')
                    {
                        if (j + 2 < line.Length && line[j + 1] == '"' && line[j + 2] == '"')
                        {
                            if (inTripleQuote)
                            {
                                inTripleQuote = false;
                                j += 2;
                            }
                            else if (!inRegularString)
                            {
                                inTripleQuote = true;
                                j += 2;
                            }
                        }
                        else if (!inTripleQuote)
                        {
                            inRegularString = !inRegularString;
                        }
                    }
                }
            }

            return result;
        }
    }
}
