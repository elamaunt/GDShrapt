using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// GDS013: Checks for consistent empty lines between functions and classes.
/// </summary>
internal class EmptyLineRule : FormattingRule
{
    public override string RuleId => "GDS013";
    public override string Name => "Empty Lines Between Functions";
    public override string Description => "Ensure consistent spacing between function definitions";
    public override GDFormattingLevel RequiredFormattingLevel => GDFormattingLevel.Full;
    public override Semantics.GDDiagnosticSeverity DefaultSeverity => Semantics.GDDiagnosticSeverity.Hint;

    private const int RequiredEmptyLinesBetweenFunctions = 2;
    private const int RequiredEmptyLinesAfterClassVars = 1;

    public override IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
        if (scriptMap?.Class == null)
            yield break;

        var lines = SplitLines(content);

        // Get all method declarations
        var methods = scriptMap.Class.Members
            .OfType<GDMethodDeclaration>()
            .OrderBy(m => m.StartLine)
            .ToList();

        // Check spacing between consecutive methods
        for (int i = 1; i < methods.Count; i++)
        {
            var prevMethod = methods[i - 1];
            var currentMethod = methods[i];

            // Find end line of previous method
            var prevEndLine = FindMethodEndLine(prevMethod, lines);
            var currentStartLine = currentMethod.StartLine;

            // Count empty lines between
            int emptyLines = CountEmptyLinesBetween(lines, prevEndLine, currentStartLine);

            if (emptyLines < RequiredEmptyLinesBetweenFunctions)
            {
                yield return CreateDiagnostic(
                        $"Expected {RequiredEmptyLinesBetweenFunctions} empty line(s) before function, found {emptyLines}",
                        scriptMap.Reference)
                    .AtLocation(currentStartLine, 0)
                    .Build();
            }
            else if (emptyLines > RequiredEmptyLinesBetweenFunctions + 1)
            {
                yield return CreateDiagnostic(
                        $"Too many empty lines before function ({emptyLines}), expected {RequiredEmptyLinesBetweenFunctions}",
                        scriptMap.Reference)
                    .AtLocation(prevEndLine + 1, 0)
                    .Build();
            }
        }

        // Check for excessive blank lines anywhere in the file
        int consecutiveBlank = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (IsBlankLine(lines[i]))
            {
                consecutiveBlank++;
                if (consecutiveBlank > 3)
                {
                    yield return CreateDiagnostic(
                            $"Too many consecutive blank lines ({consecutiveBlank})",
                            scriptMap.Reference)
                        .AtLocation(i, 0)
                        .Build();
                    // Only report once per block
                    while (i + 1 < lines.Length && IsBlankLine(lines[i + 1]))
                        i++;
                }
            }
            else
            {
                consecutiveBlank = 0;
            }
        }
    }

    private static int FindMethodEndLine(GDMethodDeclaration method, string[] lines)
    {
        // Find the last line of the method by looking at indentation
        int startLine = method.StartLine;
        int endLine = startLine;

        if (startLine >= lines.Length)
            return startLine;

        // Get method indentation level
        var methodIndent = CountLeadingWhitespace(lines[startLine]);

        // Find where the method ends (next line with same or less indentation, or end of file)
        for (int i = startLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip blank lines
            if (IsBlankLine(line))
            {
                continue;
            }

            var lineIndent = CountLeadingWhitespace(line);

            // If we hit a line with same or less indentation and it's not inside the method
            if (lineIndent <= methodIndent && !line.TrimStart().StartsWith("#"))
            {
                break;
            }

            endLine = i;
        }

        return endLine;
    }

    private static int CountEmptyLinesBetween(string[] lines, int fromLine, int toLine)
    {
        int count = 0;
        for (int i = fromLine + 1; i < toLine && i < lines.Length; i++)
        {
            if (IsBlankLine(lines[i]))
                count++;
        }
        return count;
    }
}
