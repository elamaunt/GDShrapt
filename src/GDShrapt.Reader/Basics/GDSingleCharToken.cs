namespace GDShrapt.Reader
{
    public abstract class GDSingleCharToken : GDSimpleSyntaxToken
    {
        public abstract char Char { get; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (Char != c)
                throw new GDInvalidStateException();

            state.Pop();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (Char != '\n')
                throw new GDInvalidStateException();

            state.Pop();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (Char != '#')
                throw new GDInvalidStateException();

            throw new GDInvalidStateException();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (Char != '\r')
                throw new GDInvalidStateException();

            state.Pop();
        }

        public override string ToString()
        {
            return Char.ToString();
        }
    }
}
