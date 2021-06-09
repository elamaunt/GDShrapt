namespace GDShrapt.Reader
{
    public sealed class GDContinueStatement : GDStatement
    {
        internal GDContinueStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDContinueStatement()
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
            return $"continue";
        }
    }
}