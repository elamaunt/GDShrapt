namespace GDShrapt.Reader
{
    public class GDYieldStatement : GDStatement
    {
        public GDYieldStatement(int lineIntendation)
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