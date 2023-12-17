namespace GDShrapt.Reader
{
    public abstract class GDSequenceToken : GDSimpleSyntaxToken
    {
        public abstract string Sequence { get; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassChar('#');
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            state.Pop();
            state.PassLeftSlashChar();
        }

        public override string ToString()
        {
            return Sequence;
        }
    }
}
