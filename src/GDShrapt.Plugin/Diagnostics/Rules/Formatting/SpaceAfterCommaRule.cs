using GDShrapt.Plugin.Config;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin.Diagnostics.Rules.Formatting;

/// <summary>
/// GDS011: Checks for space after commas.
/// </summary>
internal class SpaceAfterCommaRule : FormattingRule
{
    public override string RuleId => "GDS011";
    public override string Name => "Space After Comma";
    public override string Description => "Ensure space after commas in lists and function calls";
    public override FormattingLevel RequiredFormattingLevel => FormattingLevel.Full;

    // Pattern to find commas not followed by space or end of line
    private static readonly Regex MissingSpaceAfterComma = new(@",(?![\s\n\r]|$)", RegexOptions.Compiled);

    public override IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        RuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
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
                    yield return CreateDiagnostic(
                            "Missing space after comma",
                            scriptMap.Reference)
                        .AtLocation(i, col)
                        .WithSeverity(DiagnosticSeverity.Hint)
                        .WithFix(CreateReplacementFix("Add space after comma", i, col, col + 1, ", "))
                        .Build();
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
