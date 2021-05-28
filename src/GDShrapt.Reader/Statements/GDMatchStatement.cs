namespace GDShrapt.Reader
{
    public class GDMatchStatement : GDStatement
    {
        public GDMatchStatement(int lineIntendation)
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