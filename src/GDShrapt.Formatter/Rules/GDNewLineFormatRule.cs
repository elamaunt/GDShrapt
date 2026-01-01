namespace GDShrapt.Reader
{
    /// <summary>
    /// Handles line ending normalization.
    /// Note: The AST uses GDNewLine which always outputs '\n'.
    /// Line ending conversion should be done as a post-processing step on the final string.
    /// </summary>
    public class GDNewLineFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF005";
        public override string Name => "newline";
        public override string Description => "Normalize line endings";

        // GDNewLine tokens always output '\n' in ToString().
        // Line ending conversion (LF to CRLF) should be handled in GDFormatter.FormatCode
        // as a post-processing step since the AST doesn't store the original line ending style.

        // This rule is kept as a placeholder for any future line ending related formatting.
    }
}
