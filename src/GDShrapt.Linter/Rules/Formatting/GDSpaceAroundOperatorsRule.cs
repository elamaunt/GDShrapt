using System.Text.RegularExpressions;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL510: Checks for proper spacing around binary operators.
    /// </summary>
    public class GDSpaceAroundOperatorsRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL510";
        public override string Name => "space-around-operators";
        public override string Description => "Ensure single space around binary operators";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => false;

        // Pattern to detect missing space before/after operator
        private static readonly Regex NoSpaceBeforeEquals = new Regex(@"(\w)(==|!=|<=|>=|=)(?!=)", RegexOptions.Compiled);
        private static readonly Regex NoSpaceAfterEquals = new Regex(@"(==|!=|<=|>=|=)(?!=)(\w)", RegexOptions.Compiled);

        protected override void AnalyzeContent(string content)
        {
            if (Options?.CheckSpaceAroundOperators == false)
                return;

            var lines = SplitLines(content);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip comments
                if (IsComment(line))
                    continue;

                // Skip strings (simplified - doesn't handle all cases)
                var codeOnly = RemoveStrings(line);

                // Check for missing space before =, ==, etc.
                var match = NoSpaceBeforeEquals.Match(codeOnly);
                if (match.Success)
                {
                    // Find actual position in original line
                    var col = line.IndexOf(match.Value);
                    if (col >= 0)
                    {
                        var op = match.Groups[2].Value;
                        var insertCol = col + 1; // Position right before operator

                        // 1-based line numbers
                        ReportIssueAt(
                            GDLintSeverity.Hint,
                            $"Missing space before '{op}'",
                            i + 1, insertCol + 1, i + 1, insertCol + 1,
                            $"Add space before '{op}'");
                    }
                }

                // Check for missing space after =, ==, etc.
                match = NoSpaceAfterEquals.Match(codeOnly);
                if (match.Success)
                {
                    var col = line.IndexOf(match.Value);
                    if (col >= 0)
                    {
                        var op = match.Groups[1].Value;
                        var insertCol = col + op.Length; // Position right after operator

                        // 1-based line numbers
                        ReportIssueAt(
                            GDLintSeverity.Hint,
                            $"Missing space after '{op}'",
                            i + 1, insertCol + 1, i + 1, insertCol + 1,
                            $"Add space after '{op}'");
                    }
                }
            }
        }

        private static string RemoveStrings(string line)
        {
            // Very simplified string removal - proper implementation would handle escapes
            var result = line;

            // Remove double-quoted strings
            result = Regex.Replace(result, @"""[^""]*""", "\"\"");
            // Remove single-quoted strings
            result = Regex.Replace(result, @"'[^']*'", "''");

            return result;
        }
    }
}
