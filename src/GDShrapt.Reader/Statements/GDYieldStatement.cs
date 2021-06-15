namespace GDShrapt.Reader
{
    public sealed class GDYieldStatement : GDStatement
    {
        internal GDYieldStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDYieldStatement()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}