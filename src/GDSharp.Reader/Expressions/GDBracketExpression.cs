namespace GDSharp.Reader
{
    public class GDBracketExpression : GDExpression
    {
        public override int Priority => 20;

        public GDExpression InnerExpression { get; set; }
        protected internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"({InnerExpression})";
        }
    }
}