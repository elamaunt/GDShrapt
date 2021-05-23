namespace GDScriptConverter
{
    public class GDIfStatement : GDStatement
    {
        public GDExpression Condition { get; set; }
        public GDStatement TrueStatement { get; set; }
        public GDStatement FalseStatement { get; set; }

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