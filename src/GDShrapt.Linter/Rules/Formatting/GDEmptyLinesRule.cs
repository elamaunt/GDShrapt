using System.Text.RegularExpressions;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL513: Checks for consistent empty lines between functions and classes.
    /// </summary>
    public class GDEmptyLinesRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL513";
        public override string Name => "empty-lines";
        public override string Description => "Ensure consistent spacing between function definitions";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => false;

        // Pattern to detect function declaration
        private static readonly Regex FuncPattern = new Regex(@"^(\t*)func\s+\w+", RegexOptions.Compiled);

        protected override void AnalyzeContent(string content)
        {
            var requiredEmptyLines = Options?.EmptyLinesBetweenFunctions ?? 2;
            var maxConsecutiveEmpty = Options?.MaxConsecutiveEmptyLines ?? 3;

            if (requiredEmptyLines <= 0 && maxConsecutiveEmpty <= 0)
                return;

            var lines = SplitLines(content);

            // Find function declarations
            var funcLines = new System.Collections.Generic.List<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (FuncPattern.IsMatch(lines[i]))
                {
                    funcLines.Add(i);
                }
            }

            // Check spacing between consecutive functions
            if (requiredEmptyLines > 0)
            {
                for (int i = 1; i < funcLines.Count; i++)
                {
                    var prevFuncLine = funcLines[i - 1];
                    var currentFuncLine = funcLines[i];

                    // Find end of previous function (simplified: next line with same or less indentation)
                    var prevFuncEndLine = FindFunctionEndLine(lines, prevFuncLine);

                    // Count empty lines between
                    int emptyLines = CountEmptyLinesBetween(lines, prevFuncEndLine, currentFuncLine);

                    if (emptyLines < requiredEmptyLines)
                    {
                        // 1-based line numbers
                        ReportIssueAt(
                            GDLintSeverity.Hint,
                            $"Expected {requiredEmptyLines} empty line(s) before function, found {emptyLines}",
                            currentFuncLine + 1, 1, currentFuncLine + 1, 1,
                            $"Add {requiredEmptyLines - emptyLines} empty line(s)");
                    }
                    else if (emptyLines > requiredEmptyLines + 1)
                    {
                        // 1-based line numbers
                        ReportIssueAt(
                            GDLintSeverity.Hint,
                            $"Too many empty lines before function ({emptyLines}), expected {requiredEmptyLines}",
                            prevFuncEndLine + 2, 1, currentFuncLine, 1,
                            $"Remove {emptyLines - requiredEmptyLines} empty line(s)");
                    }
                }
            }

            // Check for excessive blank lines anywhere in the file
            if (maxConsecutiveEmpty > 0)
            {
                int consecutiveBlank = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (IsBlankLine(lines[i]))
                    {
                        consecutiveBlank++;
                        if (consecutiveBlank > maxConsecutiveEmpty)
                        {
                            // 1-based line numbers
                            ReportIssueAt(
                                GDLintSeverity.Hint,
                                $"Too many consecutive blank lines ({consecutiveBlank})",
                                i + 1, 1, i + 1, 1,
                                $"Reduce to {maxConsecutiveEmpty} blank line(s)");

                            // Only report once per block
                            while (i + 1 < lines.Length && IsBlankLine(lines[i + 1]))
                                i++;
                        }
                    }
                    else
                    {
                        consecutiveBlank = 0;
                    }
                }
            }
        }

        private static int FindFunctionEndLine(string[] lines, int funcStartLine)
        {
            int endLine = funcStartLine;

            if (funcStartLine >= lines.Length)
                return funcStartLine;

            // Get function indentation level
            var funcIndent = CountLeadingWhitespace(lines[funcStartLine]);

            // Find where the function ends (next line with same or less indentation)
            for (int i = funcStartLine + 1; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip blank lines
                if (IsBlankLine(line))
                    continue;

                var lineIndent = CountLeadingWhitespace(line);

                if (lineIndent <= funcIndent)
                {
                    break;
                }

                endLine = i;
            }

            return endLine;
        }

        private static int CountEmptyLinesBetween(string[] lines, int fromLine, int toLine)
        {
            int count = 0;
            for (int i = fromLine + 1; i < toLine && i < lines.Length; i++)
            {
                if (IsBlankLine(lines[i]))
                    count++;
            }
            return count;
        }
    }
}
