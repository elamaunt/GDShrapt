namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL502: Detects trailing whitespace at end of lines.
    /// </summary>
    public class GDTrailingWhitespaceRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL502";
        public override string Name => "trailing-whitespace";
        public override string Description => "Remove trailing whitespace at end of lines";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => true;

        protected override void AnalyzeContent(string content)
        {
            if (Options?.CheckTrailingWhitespace == false)
                return;

            var lines = SplitLines(content);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Handle \r at end of line (CRLF)
                var lineWithoutCR = line.TrimEnd('\r');

                var trailing = GetTrailingWhitespace(lineWithoutCR);

                if (!string.IsNullOrEmpty(trailing))
                {
                    var trimmedLength = lineWithoutCR.Length - trailing.Length;

                    // 1-based line numbers
                    ReportIssueAt(
                        $"Trailing whitespace ({trailing.Length} character(s))",
                        i + 1, trimmedLength + 1, i + 1, lineWithoutCR.Length + 1,
                        "Remove trailing whitespace");
                }
            }
        }
    }
}
