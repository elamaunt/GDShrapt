namespace GDShrapt.Reader.Basics
{
    public abstract class GDSingleCharToken : GDSimpleSyntaxToken
    {
        public abstract char Char { get; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.DropReadingToken();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.DropReadingToken();
            state.PassLineFinish();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.DropReadingToken();
            state.PassChar('#');
        }

        public override string ToString()
        {
            return Char.ToString();
        }
    }
}
