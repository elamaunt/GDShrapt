using System.Text.RegularExpressions;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL501: Checks for consistent indentation (tabs vs spaces).
    /// GDScript convention uses tabs for indentation.
    /// </summary>
    public class GDIndentationConsistencyRule : GDTextBasedLintRule
    {
        public override string RuleId => "GDL501";
        public override string Name => "indentation-consistency";
        public override string Description => "Ensure consistent use of tabs or spaces for indentation";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        // Regex to detect spaces used for indentation (at start of line, 2+ spaces followed by code)
        private static readonly Regex SpaceIndentPattern = new Regex(@"^( {2,})(?=\S)", RegexOptions.Compiled);
        // Regex to detect tabs used for indentation
        private static readonly Regex TabIndentPattern = new Regex(@"^(\t+)(?=\S)", RegexOptions.Compiled);
        // Regex to detect mixed indentation (tabs followed by spaces or vice versa)
        private static readonly Regex MixedIndentPattern = new Regex(@"^([\t ]*\t [\t ]*|[\t ]* \t[\t ]*)(?=\S)", RegexOptions.Compiled);

        protected override void AnalyzeContent(string content)
        {
            var lines = SplitLines(content);
            var preferTabs = Options?.IndentationStyle == GDIndentationStyle.Tabs;
            var tabWidth = Options?.TabWidth ?? 4;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var indent = GetIndentation(line);
                if (string.IsNullOrEmpty(indent))
                    continue;

                // Check for mixed indentation first (most severe)
                if (MixedIndentPattern.IsMatch(line))
                {
                    var suggestion = preferTabs
                        ? "Convert to tabs"
                        : "Convert to spaces";

                    // 1-based line numbers
                    ReportIssueAt(
                        "Mixed tabs and spaces in indentation",
                        i + 1, 1, i + 1, indent.Length + 1,
                        suggestion);
                    continue;
                }

                // Check if using wrong indentation style
                if (preferTabs && SpaceIndentPattern.IsMatch(line))
                {
                    ReportIssueAt(
                        "Spaces used for indentation (expected tabs)",
                        i + 1, 1, i + 1, indent.Length + 1,
                        "Convert spaces to tabs");
                }
                else if (!preferTabs && TabIndentPattern.IsMatch(line))
                {
                    ReportIssueAt(
                        "Tabs used for indentation (expected spaces)",
                        i + 1, 1, i + 1, indent.Length + 1,
                        "Convert tabs to spaces");
                }
            }
        }
    }
}
