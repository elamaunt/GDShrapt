namespace GDShrapt.Reader
{
    public abstract class GDSingleCharToken : GDSimpleSyntaxToken
    {
        public abstract char Char { get; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (Char != c)
                throw new GDInvalidReadingStateException();

            state.Pop();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (Char != '\n')
                throw new GDInvalidReadingStateException();

            state.Pop();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (Char != '#')
                throw new GDInvalidReadingStateException();

            throw new GDInvalidReadingStateException();
        }

        public override string ToString()
        {
            return Char.ToString();
        }
    }
}
