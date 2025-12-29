namespace GDShrapt.Reader
{
    /// <summary>
    /// Base class for all formatting rules.
    /// </summary>
    public abstract class GDFormatRule : GDVisitor
    {
        private GDFormatterOptions _options;

        /// <summary>
        /// Unique identifier for this rule (e.g., "GDF001").
        /// </summary>
        public abstract string RuleId { get; }

        /// <summary>
        /// Human-readable name of the rule.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Description of what this rule formats.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Whether this rule is enabled by default.
        /// </summary>
        public virtual bool EnabledByDefault => true;

        /// <summary>
        /// Current formatter options.
        /// </summary>
        protected GDFormatterOptions Options => _options;

        /// <summary>
        /// Runs this rule on the given node.
        /// </summary>
        internal void Run(GDNode node, GDFormatterOptions options)
        {
            _options = options;
            node?.WalkIn(this);
        }

        #region Helper Methods for Token Manipulation

        /// <summary>
        /// Ensures there is a single space before the specified token.
        /// </summary>
        protected void EnsureSpaceBefore(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            var prevToken = form.PreviousTokenBefore(token);

            if (prevToken is GDSpace space)
            {
                // Already has space, ensure it's a single space
                if (space.Sequence != " ")
                    space.Sequence = " ";
            }
            else if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDIntendation))
            {
                // No space exists, add one
                form.AddBeforeToken(new GDSpace() { Sequence = " " }, token);
            }
        }

        /// <summary>
        /// Ensures there is a single space after the specified token.
        /// </summary>
        protected void EnsureSpaceAfter(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            var nextToken = form.NextTokenAfter(token);

            if (nextToken is GDSpace space)
            {
                // Already has space, ensure it's a single space
                if (space.Sequence != " ")
                    space.Sequence = " ";
            }
            else if (nextToken != null && !(nextToken is GDNewLine) && !(nextToken is GDIntendation))
            {
                // No space exists, add one
                form.AddAfterToken(new GDSpace() { Sequence = " " }, token);
            }
        }

        /// <summary>
        /// Removes any space before the specified token.
        /// </summary>
        protected void RemoveSpaceBefore(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            var prevToken = form.PreviousTokenBefore(token);

            if (prevToken is GDSpace)
            {
                form.Remove(prevToken);
            }
        }

        /// <summary>
        /// Removes any space after the specified token.
        /// </summary>
        protected void RemoveSpaceAfter(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            var nextToken = form.NextTokenAfter(token);

            if (nextToken is GDSpace)
            {
                form.Remove(nextToken);
            }
        }

        /// <summary>
        /// Ensures a specific number of blank lines before the token.
        /// </summary>
        protected void EnsureBlankLinesBefore(GDSyntaxToken token, GDNode parent, int count)
        {
            if (token == null || parent == null || count < 0)
                return;

            var form = parent.Form;

            // Count existing newlines before this token
            int existingNewlines = 0;
            GDSyntaxToken current = form.PreviousTokenBefore(token);

            while (current != null)
            {
                if (current is GDNewLine)
                {
                    existingNewlines++;
                    current = form.PreviousTokenBefore(current);
                }
                else if (current is GDSpace || current is GDIntendation)
                {
                    current = form.PreviousTokenBefore(current);
                }
                else
                {
                    break;
                }
            }

            // Blank lines = newlines - 1 (since one newline ends the previous line)
            int existingBlankLines = existingNewlines > 0 ? existingNewlines - 1 : 0;
            int neededBlankLines = count;

            if (existingBlankLines < neededBlankLines)
            {
                // Add more newlines
                for (int i = existingBlankLines; i < neededBlankLines; i++)
                {
                    form.AddBeforeToken(new GDNewLine(), token);
                }
            }
            // Note: We don't remove extra newlines in this method to preserve user formatting
        }

        /// <summary>
        /// Updates indentation for a token using the specified pattern.
        /// </summary>
        protected void UpdateIndentation(GDIntendation indentation, string pattern)
        {
            if (indentation == null || pattern == null)
                return;

            indentation.Update(pattern);
        }

        /// <summary>
        /// Gets the line ending string based on the current options.
        /// </summary>
        protected string GetLineEnding()
        {
            switch (Options?.LineEnding ?? LineEndingStyle.LF)
            {
                case LineEndingStyle.CRLF:
                    return "\r\n";
                case LineEndingStyle.Platform:
                    return System.Environment.NewLine;
                case LineEndingStyle.LF:
                default:
                    return "\n";
            }
        }

        #endregion
    }
}
