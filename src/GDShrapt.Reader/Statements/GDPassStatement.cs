namespace GDShrapt.Reader
{
    public class GDPassStatement : GDStatement
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
            return $"pass";
        }
    }
}