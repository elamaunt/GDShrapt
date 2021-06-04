namespace GDShrapt.Reader
{
    public class GDContinueStatement : GDStatement
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
            return $"continue";
        }
    }
}