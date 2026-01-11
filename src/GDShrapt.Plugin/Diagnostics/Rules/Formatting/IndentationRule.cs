using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin;

/// <summary>
/// GDS001: Checks for consistent indentation (tabs vs spaces).
/// GDScript convention uses tabs for indentation.
/// </summary>
internal class IndentationRule : FormattingRule
{
    public override string RuleId => "GDS001";
    public override string Name => "Indentation Consistency";
    public override string Description => "Ensure consistent use of tabs or spaces for indentation";
    public override GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Light;

    // Regex to detect spaces used for indentation (at start of line, 2+ spaces followed by code)
    private static readonly Regex SpaceIndentPattern = new(@"^( {2,})(?=\S)", RegexOptions.Compiled);
    // Regex to detect tabs used for indentation
    private static readonly Regex TabIndentPattern = new(@"^(\t+)(?=\S)", RegexOptions.Compiled);
    // Regex to detect mixed indentation (tabs followed by spaces or vice versa)
    private static readonly Regex MixedIndentPattern = new(@"^([\t ]*\t [\t ]*|[\t ]* \t[\t ]*)(?=\S)", RegexOptions.Compiled);

    public override IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
        var lines = SplitLines(content);
        var lintingConfig = projectConfig.Linting;
        var preferTabs = lintingConfig.IndentationStyle == GDIndentationStyle.Tabs;
        var tabWidth = lintingConfig.TabWidth;

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
                var fixedIndent = preferTabs
                    ? ConvertToTabs(indent, tabWidth)
                    : ConvertToSpaces(indent, tabWidth);

                yield return CreateDiagnostic(
                        "Mixed tabs and spaces in indentation",
                        scriptMap.Reference)
                    .AtLocation(i, 0)
                    .WithFix(CreateReplacementFix("Convert to consistent indentation",
                        i, 0, indent.Length, fixedIndent))
                    .Build();
                continue;
            }

            // Check if using wrong indentation style
            if (preferTabs && SpaceIndentPattern.IsMatch(line))
            {
                var fixedIndent = ConvertToTabs(indent, tabWidth);

                yield return CreateDiagnostic(
                        "Spaces used for indentation (expected tabs)",
                        scriptMap.Reference)
                    .AtLocation(i, 0)
                    .WithFix(CreateReplacementFix("Convert spaces to tabs",
                        i, 0, indent.Length, fixedIndent))
                    .Build();
            }
            else if (!preferTabs && TabIndentPattern.IsMatch(line))
            {
                var fixedIndent = ConvertToSpaces(indent, tabWidth);

                yield return CreateDiagnostic(
                        "Tabs used for indentation (expected spaces)",
                        scriptMap.Reference)
                    .AtLocation(i, 0)
                    .WithFix(CreateReplacementFix("Convert tabs to spaces",
                        i, 0, indent.Length, fixedIndent))
                    .Build();
            }
        }
    }

    private static string ConvertToTabs(string indent, int tabWidth)
    {
        // Count spaces and convert to equivalent tabs
        int spaces = 0;
        int tabs = 0;

        foreach (char c in indent)
        {
            if (c == '\t')
            {
                tabs++;
                spaces = 0; // Reset space count
            }
            else if (c == ' ')
            {
                spaces++;
                if (spaces >= tabWidth)
                {
                    tabs++;
                    spaces = 0;
                }
            }
        }

        // Add remaining spaces as a partial tab
        if (spaces > 0)
            tabs++;

        return new string('\t', tabs);
    }

    private static string ConvertToSpaces(string indent, int tabWidth)
    {
        int totalSpaces = 0;

        foreach (char c in indent)
        {
            if (c == '\t')
            {
                // Tab moves to next tab stop
                totalSpaces = ((totalSpaces / tabWidth) + 1) * tabWidth;
            }
            else if (c == ' ')
            {
                totalSpaces++;
            }
        }

        return new string(' ', totalSpaces);
    }
}
