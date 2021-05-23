namespace GDScriptConverter
{
    public class GDStringExpression : GDCharSequenceNode
    {
        public override void HandleLineFinish(GDReadingState state)
        {
            Append('\n');
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != '"';
        }

        public override void HandleSharpChar(GDReadingState state)
        {
            Append('#');
        }
    }
}