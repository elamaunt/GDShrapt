namespace GDSharp.Reader
{
    public class GDWhileStatement : GDStatement
    {
        public GDExpression Condition { get; set; }
        public GDStatement Body { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
