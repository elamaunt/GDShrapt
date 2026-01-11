using System.Collections.Generic;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks that lines don't exceed the maximum length.
    /// Based on GDScript style guide: "Keep lines of code under 100 characters."
    /// </summary>
    public class GDLineLengthRule : GDLintRule
    {
        public override string RuleId => "GDL101";
        public override string Name => "line-length";
        public override string Description => "Lines should not exceed maximum length";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        private readonly HashSet<int> _reportedLines = new HashSet<int>();

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            var maxLength = Options?.MaxLineLength ?? 100;
            if (maxLength <= 0)
                return;

            _reportedLines.Clear();

            // Iterate through all tokens to build lines and check their length
            var currentLineLength = 0;
            var currentLineNumber = 1;
            GDSyntaxToken firstTokenOnLine = null;

            foreach (var token in classDeclaration.AllTokens)
            {
                if (token is GDNewLine)
                {
                    // Check current line before moving to next
                    CheckLineLength(currentLineNumber, currentLineLength, maxLength, firstTokenOnLine);

                    // Reset for next line
                    currentLineLength = 0;
                    currentLineNumber++;
                    firstTokenOnLine = null;
                }
                else
                {
                    // Track first token on line for reporting
                    if (firstTokenOnLine == null)
                        firstTokenOnLine = token;

                    // Add token length to current line
                    currentLineLength += token.Length;
                }
            }

            // Check the last line
            CheckLineLength(currentLineNumber, currentLineLength, maxLength, firstTokenOnLine);
        }

        private void CheckLineLength(int lineNumber, int length, int maxLength, GDSyntaxToken firstToken)
        {
            if (length > maxLength && firstToken != null && !_reportedLines.Contains(lineNumber))
            {
                _reportedLines.Add(lineNumber);
                ReportIssue(
                    $"Line {lineNumber} exceeds {maxLength} characters (length: {length})",
                    firstToken,
                    "Break line into multiple lines or shorten identifiers");
            }
        }
    }
}
