namespace GDShrapt.Reader
{
    public sealed class GDPassStatement : GDStatement
    {
        internal GDPassStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDPassStatement()
        {

        }

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

        public override string ToString()
        {
            return $"pass";
        }
    }
}