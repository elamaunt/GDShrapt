using System;

namespace GDShrapt.Reader
{
    public sealed class GDInvalidToken : GDCharSequence
    {
        readonly Predicate<char> _stop;

        private GDInvalidToken()
        {
        }

        internal GDInvalidToken(Predicate<char> stop)
        {
            _stop = stop;
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return !_stop(c);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDInvalidToken()
            { 
                Sequence = Sequence
            };
        }
    }
}
