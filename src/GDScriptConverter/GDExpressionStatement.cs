namespace GDScriptConverter
{
    public class GDExpressionStatement : GDStatement
    {
        public GDExpression Expression { get; set; }
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