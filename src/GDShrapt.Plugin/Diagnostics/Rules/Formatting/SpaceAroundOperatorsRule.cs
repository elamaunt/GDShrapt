using GDShrapt.Plugin.Config;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin.Diagnostics.Rules.Formatting;

/// <summary>
/// GDS010: Checks for proper spacing around binary operators.
/// </summary>
internal class SpaceAroundOperatorsRule : FormattingRule
{
    public override string RuleId => "GDS010";
    public override string Name => "Space Around Operators";
    public override string Description => "Ensure single space around binary operators";
    public override GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Full;

    // Binary operators that should have space around them
    private static readonly string[] BinaryOperators = { "==", "!=", "<=", ">=", "&&", "||", "+=", "-=", "*=", "/=", "%=", "<<", ">>", "->", "<", ">", "+", "-", "*", "/", "%", "=", "and", "or" };

    // Pattern to detect missing space before/after operator
    // This is simplified - a full implementation would use AST analysis
    private static readonly Regex NoSpaceBeforeEquals = new(@"(\w)(==|!=|<=|>=|=)(?!=)", RegexOptions.Compiled);
    private static readonly Regex NoSpaceAfterEquals = new(@"(==|!=|<=|>=|=)(?!=)(\w)", RegexOptions.Compiled);

    public override IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
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
                    yield return CreateDiagnostic(
                            $"Missing space before '{op}'",
                            scriptMap.Reference)
                        .AtLocation(i, insertCol)
                        .WithSeverity(GDDiagnosticSeverity.Hint)
                        .WithFix(CreateReplacementFix(
                            $"Add space before '{op}'",
                            i, insertCol, insertCol, " "))
                        .Build();
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
                    yield return CreateDiagnostic(
                            $"Missing space after '{op}'",
                            scriptMap.Reference)
                        .AtLocation(i, insertCol)
                        .WithSeverity(GDDiagnosticSeverity.Hint)
                        .WithFix(CreateReplacementFix(
                            $"Add space after '{op}'",
                            i, insertCol, insertCol, " "))
                        .Build();
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
