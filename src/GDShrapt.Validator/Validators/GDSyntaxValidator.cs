namespace GDShrapt.Reader
{
    /// <summary>
    /// Reports all GDInvalidToken nodes found during parsing.
    /// Groups consecutive invalid tokens (separated only by spaces/comments)
    /// into a single diagnostic on the first token in the group.
    /// </summary>
    public class GDSyntaxValidator : GDValidationVisitor
    {
        readonly bool _showKeywordHints;

        public GDSyntaxValidator(GDValidationContext context, bool showKeywordHints = true) : base(context)
        {
            _showKeywordHints = showKeywordHints;
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            foreach (var invalidToken in node.AllInvalidTokens)
            {
                if (IsPartOfPreviousErrorZone(invalidToken))
                    continue;

                ReportInvalidToken(invalidToken);
            }
        }

        /// <summary>
        /// Walks backwards through GlobalPreviousToken to check if there is
        /// another GDInvalidToken before this one, separated only by
        /// GDSpace/GDComment (no valid tokens, no newlines).
        /// </summary>
        private static bool IsPartOfPreviousErrorZone(GDInvalidToken token)
        {
            var prev = token.GlobalPreviousToken;

            while (prev != null)
            {
                if (prev is GDInvalidToken)
                    return true;

                if (prev is GDSpace || prev is GDComment)
                {
                    prev = prev.GlobalPreviousToken;
                    continue;
                }

                break;
            }

            return false;
        }

        private void ReportInvalidToken(GDInvalidToken invalidToken)
        {
            string message;

            if (_showKeywordHints && invalidToken.PossibleKeyword != null)
            {
                message = $"Invalid token: '{invalidToken.Sequence}'. Did you mean '{invalidToken.PossibleKeyword}'?";
            }
            else if (_showKeywordHints && invalidToken.StartsWithKeyword != null)
            {
                var remainder = invalidToken.Sequence.Substring(invalidToken.StartsWithKeyword.Length);
                message = $"Invalid token: '{invalidToken.Sequence}'. Missing space between '{invalidToken.StartsWithKeyword}' and '{remainder}'?";
            }
            else
            {
                message = $"Invalid token: '{invalidToken.Sequence}'";
            }

            Context.AddError(GDDiagnosticCode.InvalidToken, message, invalidToken);
        }
    }
}
