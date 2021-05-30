namespace GDShrapt.Reader
{
    public class GDMatchStatement : GDStatement
    {
        internal GDMatchStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDMatchStatement()
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