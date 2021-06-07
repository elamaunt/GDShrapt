namespace GDShrapt.Reader
{
    public sealed class GDBreakPointStatement : GDStatement
    {
        internal GDBreakPointStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDBreakPointStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"breakpoint";
        }
    }
}
