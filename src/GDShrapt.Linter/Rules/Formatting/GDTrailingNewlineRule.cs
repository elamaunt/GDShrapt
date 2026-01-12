namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL503: Ensures file ends with a single newline.
    /// </summary>
    public class GDTrailingNewlineRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL503";
        public override string Name => "trailing-newline";
        public override string Description => "Ensure file ends with exactly one newline";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => true;

        protected override void AnalyzeContent(string content)
        {
            if (Options?.CheckTrailingNewline == false)
                return;

            if (string.IsNullOrEmpty(content))
                return;

            var lines = SplitLines(content);

            // Check for missing newline at end
            if (!content.EndsWith("\n"))
            {
                var lastLineIndex = lines.Length;
                var lastLineLength = lines.Length > 0 ? lines[lines.Length - 1].TrimEnd('\r').Length : 0;

                // 1-based line numbers
                ReportIssueAt(
                    "File should end with a newline",
                    lastLineIndex, lastLineLength + 1, lastLineIndex, lastLineLength + 1,
                    "Add trailing newline");

                return;
            }

            // Check for multiple trailing newlines
            int trailingNewlines = 0;
            for (int i = content.Length - 1; i >= 0; i--)
            {
                if (content[i] == '\n')
                    trailingNewlines++;
                else if (content[i] != '\r') // Skip \r in CRLF
                    break;
            }

            if (trailingNewlines > 1)
            {
                var lastNonEmptyLine = lines.Length - trailingNewlines + 1;

                // 1-based line numbers
                ReportIssueAt(
                    $"File has {trailingNewlines} trailing newlines (should be 1)",
                    lastNonEmptyLine, 1, lines.Length, 1,
                    "Remove extra trailing newlines");
            }
        }
    }
}
