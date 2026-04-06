using System;

namespace GDShrapt.Reader
{
    public sealed class GDInvalidToken : GDCharSequence
    {
        readonly Predicate<char> _stop;

        /// <summary>
        /// When this token is a prefix of a keyword (e.g., "va" for "var"), stores the keyword.
        /// Set during parsing when DetectKeywordFragments is enabled.
        /// </summary>
        public string PossibleKeyword { get; internal set; }

        /// <summary>
        /// When this token starts with a keyword followed by identifier chars
        /// (e.g., "varx" → keyword "var"), stores the keyword.
        /// Set during parsing when DetectKeywordFragments is enabled.
        /// </summary>
        public string StartsWithKeyword { get; internal set; }

        private GDInvalidToken()
        {
        }

        internal GDInvalidToken(Predicate<char> stop)
        {
            _stop = stop;
        }

        internal GDInvalidToken(string sequence)
        {
            Sequence = sequence;
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return !_stop(c);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDInvalidToken()
            {
                Sequence = Sequence,
                PossibleKeyword = PossibleKeyword,
                StartsWithKeyword = StartsWithKeyword
            };
        }
    }
}
