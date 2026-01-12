using System.Text.RegularExpressions;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL511: Checks for space after commas.
    /// </summary>
    public class GDSpaceAfterCommaRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL511";
        public override string Name => "space-after-comma";
        public override string Description => "Ensure space after commas in lists and function calls";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => false;

        // Pattern to find commas not followed by space or end of line
        private static readonly Regex MissingSpaceAfterComma = new Regex(@",(?![\s\n\r]|$)", RegexOptions.Compiled);

        protected override void AnalyzeContent(string content)
        {
            if (Options?.CheckSpaceAfterComma == false)
                return;

            var lines = SplitLines(content);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip comments
                if (IsComment(line))
                    continue;

                // Remove strings before checking (simplified)
                var codeOnly = RemoveStrings(line);

                var matches = MissingSpaceAfterComma.Matches(codeOnly);
                foreach (Match match in matches)
                {
                    // Find position in original line
                    var col = FindCommaPosition(line, match.Index);
                    if (col >= 0)
                    {
                        // 1-based line numbers
                        ReportIssueAt(
                            "Missing space after comma",
                            i + 1, col + 1, i + 1, col + 2,
                            "Add space after comma");
                    }
                }
            }
        }

        private static int FindCommaPosition(string line, int approximateIndex)
        {
            // Try to find the comma at or near the approximate index
            for (int offset = 0; offset <= 10; offset++)
            {
                if (approximateIndex + offset < line.Length && line[approximateIndex + offset] == ',')
                    return approximateIndex + offset;
                if (approximateIndex - offset >= 0 && line[approximateIndex - offset] == ',')
                    return approximateIndex - offset;
            }
            return -1;
        }

        private static string RemoveStrings(string line)
        {
            var result = line;
            result = Regex.Replace(result, @"""[^""]*""", "\"\"");
            result = Regex.Replace(result, @"'[^']*'", "''");
            return result;
        }
    }
}
