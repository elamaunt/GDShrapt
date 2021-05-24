namespace GDScriptConverter
{
    public class GDBracketExpression : GDExpression
    {
        public GDExpression InnerExpression { get; set; }
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