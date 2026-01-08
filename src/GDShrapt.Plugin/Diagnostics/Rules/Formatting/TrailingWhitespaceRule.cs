using GDShrapt.Plugin.Config;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Diagnostics.Rules.Formatting;

/// <summary>
/// GDS002: Detects trailing whitespace at end of lines.
/// </summary>
internal class TrailingWhitespaceRule : FormattingRule
{
    public override string RuleId => "GDS002";
    public override string Name => "Trailing Whitespace";
    public override string Description => "Remove trailing whitespace at end of lines";
    public override FormattingLevel RequiredFormattingLevel => FormattingLevel.Light;

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

            // Handle \r at end of line (CRLF)
            var lineWithoutCR = line.TrimEnd('\r');

            var trailing = GetTrailingWhitespace(lineWithoutCR);

            if (!string.IsNullOrEmpty(trailing))
            {
                var trimmedLength = lineWithoutCR.Length - trailing.Length;

                yield return CreateDiagnostic(
                        $"Trailing whitespace ({trailing.Length} character(s))",
                        scriptMap.Reference)
                    .AtSpan(i, trimmedLength, i, lineWithoutCR.Length)
                    .WithFix(CreateRemovalFix("Remove trailing whitespace",
                        i, trimmedLength, lineWithoutCR.Length))
                    .Build();
            }
        }
    }
}
