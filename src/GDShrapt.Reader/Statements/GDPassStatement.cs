namespace GDShrapt.Reader
{
    public class GDPassStatement : GDStatement
    {
        public GDPassStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDPassStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"pass";
        }
    }
}