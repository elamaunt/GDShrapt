namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that files don't exceed the maximum line count.
    /// Compatible with gdtoolkit's max-file-lines rule.
    /// </summary>
    public class GDMaxFileLinesRule : GDLintRule
    {
        public override string RuleId => "GDL102";
        public override string Name => "max-file-lines";
        public override string Description => "Files should not exceed the maximum line count";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            var maxFileLines = Options?.MaxFileLines ?? 1000;
            if (maxFileLines <= 0)
            {
                base.Visit(classDeclaration);
                return;
            }

            // Count lines by counting newlines + 1 (for the last line)
            int lineCount = 1;
            foreach (var token in classDeclaration.AllTokens)
            {
                if (token is GDNewLine)
                    lineCount++;
            }

            if (lineCount > maxFileLines)
            {
                ReportIssue(
                    $"File has {lineCount} lines, exceeds maximum of {maxFileLines}",
                    classDeclaration.FirstChildToken,
                    $"Consider splitting into multiple files");
            }

            base.Visit(classDeclaration);
        }
    }
}
