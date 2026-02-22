using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL513: Checks for consistent empty lines between functions and classes.
    /// Uses AST-based analysis to avoid false positives from multiline strings.
    /// </summary>
    public class GDEmptyLinesRule : GDLintRule
    {
        public override string RuleId => "GDL513";
        public override string Name => "empty-lines";
        public override string Description => "Ensure consistent spacing between function definitions";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => false;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            var requiredEmptyLines = Options?.EmptyLinesBetweenFunctions ?? 2;
            var maxConsecutiveEmpty = Options?.MaxConsecutiveEmptyLines ?? 3;

            if (requiredEmptyLines <= 0 && maxConsecutiveEmpty <= 0)
                return;

            if (requiredEmptyLines > 0)
                CheckFunctionSpacing(classDeclaration, requiredEmptyLines);

            if (maxConsecutiveEmpty > 0)
                CheckConsecutiveEmptyLines(classDeclaration, maxConsecutiveEmpty);
        }

        private void CheckFunctionSpacing(GDClassDeclaration classDeclaration, int requiredEmptyLines)
        {
            if (classDeclaration.Members == null)
                return;

            var methods = classDeclaration.Members
                .OfType<GDMethodDeclaration>()
                .OrderBy(m => m.StartLine)
                .ToList();

            if (methods.Count < 2)
                return;

            for (int i = 1; i < methods.Count; i++)
            {
                var prevMethod = methods[i - 1];
                var currentMethod = methods[i];

                int emptyLines = CountNewLinesBetweenMembers(classDeclaration.Members, prevMethod, currentMethod);

                var funcToken = currentMethod.FuncKeyword;

                if (emptyLines < requiredEmptyLines)
                {
                    ReportIssue(
                        GDLintSeverity.Hint,
                        $"Expected {requiredEmptyLines} empty line(s) before function, found {emptyLines}",
                        funcToken ?? (GDSyntaxToken)currentMethod.AllTokens.FirstOrDefault(),
                        $"Add {requiredEmptyLines - emptyLines} empty line(s)");
                }
                else if (emptyLines > requiredEmptyLines + 1)
                {
                    ReportIssue(
                        GDLintSeverity.Hint,
                        $"Too many empty lines before function ({emptyLines}), expected {requiredEmptyLines}",
                        funcToken ?? (GDSyntaxToken)currentMethod.AllTokens.FirstOrDefault(),
                        $"Remove {emptyLines - requiredEmptyLines} empty line(s)");
                }
            }
        }

        private static int CountNewLinesBetweenMembers(GDClassMembersList members, GDMethodDeclaration prev, GDMethodDeclaration current)
        {
            bool foundPrev = false;
            int consecutiveNewLines = 0;
            int maxConsecutive = 0;

            foreach (var token in members.Tokens)
            {
                if (token == prev)
                {
                    foundPrev = true;
                    consecutiveNewLines = 0;
                    maxConsecutive = 0;
                    continue;
                }

                if (token == current)
                    break;

                if (!foundPrev)
                    continue;

                if (token is GDNewLine)
                {
                    consecutiveNewLines++;
                    if (consecutiveNewLines > maxConsecutive)
                        maxConsecutive = consecutiveNewLines;
                }
                else if (token is GDCarriageReturnToken || token is GDIntendation || token is GDSpace)
                {
                    // Skip whitespace tokens — they don't break consecutive newline counting
                }
                else
                {
                    consecutiveNewLines = 0;
                }
            }

            // Empty lines = consecutive newlines - 1 (first newline ends the previous code line)
            return maxConsecutive > 0 ? maxConsecutive - 1 : 0;
        }

        private void CheckConsecutiveEmptyLines(GDClassDeclaration classDeclaration, int maxConsecutiveEmpty)
        {
            if (classDeclaration.Members == null)
                return;

            int consecutiveNewLines = 0;
            bool reported = false;

            foreach (var token in classDeclaration.Members.Tokens)
            {
                if (token is GDNewLine newLine)
                {
                    consecutiveNewLines++;
                    if (consecutiveNewLines > maxConsecutiveEmpty + 1 && !reported)
                    {
                        reported = true;
                        ReportIssue(
                            GDLintSeverity.Hint,
                            $"Too many consecutive blank lines ({consecutiveNewLines - 1})",
                            newLine,
                            $"Reduce to {maxConsecutiveEmpty} blank line(s)");
                    }
                }
                else if (token is GDCarriageReturnToken || token is GDIntendation || token is GDSpace)
                {
                    // Skip whitespace tokens — they don't break consecutive newline counting
                }
                else
                {
                    consecutiveNewLines = 0;
                    reported = false;
                }
            }
        }
    }
}
