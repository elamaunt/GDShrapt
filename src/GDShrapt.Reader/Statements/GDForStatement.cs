namespace GDShrapt.Reader
{
    public class GDForStatement : GDStatement
    {
        public GDForStatement(int lineIntendation)
            : base(lineIntendation)
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
