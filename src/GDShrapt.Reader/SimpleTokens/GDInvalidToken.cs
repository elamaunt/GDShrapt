using System;

namespace GDShrapt.Reader
{
    public sealed class GDInvalidToken : GDCharSequence
    {
        readonly char[] _stopChars;
        readonly Predicate<char> _stop;

        internal GDInvalidToken(params char[] stopChars)
        {
            _stopChars = stopChars;
        }

        internal GDInvalidToken(Predicate<char> stop)
        {
            _stop = stop;
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            if (_stop != null)
                return !_stop(c);

            return Array.IndexOf(_stopChars, c) == -1;
        }
    }
}
