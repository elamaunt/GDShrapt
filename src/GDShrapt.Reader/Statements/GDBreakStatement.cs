namespace GDShrapt.Reader
{
    public sealed class GDBreakStatement : GDStatement
    {

        public GDBreakKeyword Break { get; set; }

        internal GDBreakStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDBreakStatement()
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
            return $"break";
        }
    }
}
