using GDShrapt.Plugin.Config;
using GDShrapt.Semantics;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Diagnostics.Rules.Formatting;

/// <summary>
/// GDS003: Ensures file ends with a single newline.
/// </summary>
internal class TrailingNewlineRule : FormattingRule
{
    public override string RuleId => "GDS003";
    public override string Name => "Trailing Newline";
    public override string Description => "Ensure file ends with exactly one newline";
    public override GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Light;

    public override IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
        if (string.IsNullOrEmpty(content))
            yield break;

        var lineEnding = DetectLineEnding(content);

        // Check for missing newline at end
        if (!content.EndsWith("\n"))
        {
            var lines = SplitLines(content);
            var lastLineIndex = lines.Length - 1;

            yield return CreateDiagnostic(
                    "File should end with a newline",
                    scriptMap.Reference)
                .AtLocation(lastLineIndex, lines[lastLineIndex].Length)
                .WithFix(CreateFileFix("Add trailing newline", c => c + lineEnding))
                .Build();

            yield break;
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
            var lines = SplitLines(content);
            var lastNonEmptyLine = lines.Length - trailingNewlines;

            yield return CreateDiagnostic(
                    $"File has {trailingNewlines} trailing newlines (should be 1)",
                    scriptMap.Reference)
                .AtLocation(lastNonEmptyLine, 0)
                .WithFix(CreateFileFix("Remove extra trailing newlines", c =>
                {
                    var trimmed = c.TrimEnd('\r', '\n');
                    return trimmed + lineEnding;
                }))
                .Build();
        }
    }
}
