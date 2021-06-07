namespace GDShrapt.Reader
{
    public sealed class GDBreakStatement : GDStatement
    {
        internal GDBreakStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDBreakStatement()
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
            return $"break";
        }
    }
}
