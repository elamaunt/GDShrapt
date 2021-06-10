namespace GDShrapt.Reader
{
    public sealed class GDBreakPointStatement : GDStatement
    {
        public GDBreakPointKeyword BreakPoint { get; set; }

        internal GDBreakPointStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDBreakPointStatement()
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
            return $"breakpoint";
        }
    }
}
