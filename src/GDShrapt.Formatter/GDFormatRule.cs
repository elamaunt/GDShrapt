using GDShrapt.Reader;

namespace GDShrapt.Formatter
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
        /// Ensures there is at least one space before the specified token.
        /// Idempotent: preserves existing multi-space formatting for alignment.
        /// Also checks if the previous token (e.g., a child expression) has trailing space.
        /// </summary>
        protected void EnsureSpaceBefore(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            if (form == null)
                return;

            var prevToken = form.PreviousTokenBefore(token);

            if (prevToken is GDSpace)
            {
                // Already has space - preserve it as-is for idempotency
                // (user may have multi-space for alignment)
            }
            else if (prevToken is GDNode prevNode)
            {
                // Previous token is a child node (e.g., expression).
                // Check if it has trailing space in its own form.
                if (!HasTrailingSpace(prevNode))
                {
                    form.AddBeforeToken(new GDSpace() { Sequence = " " }, token);
                }
            }
            else if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDIntendation))
            {
                // No space exists, add one
                form.AddBeforeToken(new GDSpace() { Sequence = " " }, token);
            }
            // If prevToken is null, NewLine, or Indentation - don't add space
        }

        /// <summary>
        /// Ensures there is a single space after the specified token.
        /// Idempotent: does nothing if a proper space already exists.
        /// Also checks if the next token (e.g., a child expression) has leading space.
        /// </summary>
        protected void EnsureSpaceAfter(GDSyntaxToken token, GDNode parent)
        {
            if (token == null || parent == null)
                return;

            var form = parent.Form;
            if (form == null)
                return;

            var nextToken = form.NextTokenAfter(token);

            if (nextToken is GDSpace)
            {
                // Already has space - preserve it as-is for idempotency
                // (user may have multi-space for alignment)
            }
            else if (nextToken is GDNode nextNode)
            {
                // Next token is a child node (e.g., expression).
                // Check if it has leading space in its own form.
                if (!HasLeadingSpace(nextNode))
                {
                    form.AddAfterToken(new GDSpace() { Sequence = " " }, token);
                }
            }
            else if (nextToken != null && !(nextToken is GDNewLine) && !(nextToken is GDIntendation))
            {
                // No space exists, add one
                form.AddAfterToken(new GDSpace() { Sequence = " " }, token);
            }
            // If nextToken is null, NewLine, or Indentation - don't add space
        }

        /// <summary>
        /// Checks if a node has trailing whitespace in its form.
        /// </summary>
        private bool HasTrailingSpace(GDNode node)
        {
            if (node?.Form == null)
                return false;

            GDSyntaxToken lastToken = null;
            foreach (var t in node.Form)
            {
                lastToken = t;
            }

            if (lastToken is GDSpace)
                return true;

            // Recursively check the last child node
            if (lastToken is GDNode childNode)
                return HasTrailingSpace(childNode);

            return false;
        }

        /// <summary>
        /// Checks if a node has leading whitespace in its form.
        /// </summary>
        private bool HasLeadingSpace(GDNode node)
        {
            if (node?.Form == null)
                return false;

            GDSyntaxToken firstToken = null;
            foreach (var t in node.Form)
            {
                firstToken = t;
                break;
            }

            if (firstToken is GDSpace)
                return true;

            // Recursively check the first child node
            if (firstToken is GDNode childNode)
                return HasLeadingSpace(childNode);

            return false;
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
        /// For tokens inside indented blocks (inner classes, etc.), adds proper indentation
        /// after blank lines to preserve the block structure during reparsing.
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
                // Calculate indentation level for this token
                int indentLevel = CalculateIndentationLevel(token);
                string indentPattern = GetIndentationPattern();

                // Add more newlines with proper indentation
                for (int i = existingBlankLines; i < neededBlankLines; i++)
                {
                    // Add the newline
                    form.AddBeforeToken(new GDNewLine(), token);

                    // Add indentation after the newline if we're inside an indented block
                    // This is critical for GDScript parsing - blank lines inside inner classes
                    // must have proper indentation, otherwise the parser will think
                    // the following content belongs to the outer scope
                    if (indentLevel > 0)
                    {
                        var indent = CreateIndentation(indentLevel, indentPattern);
                        form.AddBeforeToken(indent, token);
                    }
                }
            }
            // Note: We don't remove extra newlines in this method to preserve user formatting
        }

        /// <summary>
        /// Calculates the indentation level for a token by counting GDIntendedNode ancestors.
        /// </summary>
        private int CalculateIndentationLevel(GDSyntaxToken token)
        {
            int level = 0;
            var node = token.Parent;

            while (node != null)
            {
                if (node is GDIntendedNode)
                    level++;
                node = node.Parent;
            }

            return level;
        }

        /// <summary>
        /// Gets the indentation pattern from options (tab or spaces).
        /// </summary>
        private string GetIndentationPattern()
        {
            if (Options == null || Options.IndentStyle == IndentStyle.Tabs)
                return "\t";

            return new string(' ', Options.IndentSize);
        }

        /// <summary>
        /// Creates a GDIntendation token with the specified level and pattern.
        /// </summary>
        private GDIntendation CreateIndentation(int level, string pattern)
        {
            var indent = new GDIntendation();

            if (level <= 0)
            {
                indent.Sequence = string.Empty;
                indent.LineIntendationThreshold = 0;
            }
            else if (level == 1)
            {
                indent.Sequence = pattern;
                indent.LineIntendationThreshold = 1;
            }
            else
            {
                var builder = new System.Text.StringBuilder(pattern.Length * level);
                for (int i = 0; i < level; i++)
                    builder.Append(pattern);
                indent.Sequence = builder.ToString();
                indent.LineIntendationThreshold = level;
            }

            return indent;
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
