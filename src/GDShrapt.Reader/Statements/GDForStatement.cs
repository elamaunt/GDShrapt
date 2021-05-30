namespace GDShrapt.Reader
{
    public class GDForStatement : GDStatement
    {
        internal GDForStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDForStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
