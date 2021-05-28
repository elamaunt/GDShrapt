namespace GDShrapt.Reader
{
    public class GDWhileStatement : GDStatement
    {
        public GDWhileStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDExpression Condition { get; set; }
        public GDStatement Body { get; set; }

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
