using System;

namespace GDShrapt.Reader
{
    public sealed class GDInvalidToken : GDCharSequence
    {
        private readonly char[] _stopChars;

        internal GDInvalidToken(params char[] stopChars)
        {
            _stopChars = stopChars;
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return Array.IndexOf(_stopChars, c) == -1;
        }
    }
}
