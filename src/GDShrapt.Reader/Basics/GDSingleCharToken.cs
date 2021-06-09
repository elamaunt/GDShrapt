namespace GDShrapt.Reader
{
    public abstract class GDSingleCharToken : GDSimpleSyntaxToken
    {
        public abstract char Char { get; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassChar('#');
        }

        public override string ToString()
        {
            return Char.ToString();
        }
    }
}
