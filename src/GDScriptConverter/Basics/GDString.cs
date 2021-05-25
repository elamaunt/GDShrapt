namespace GDScriptConverter
{
    public class GDString : GDCharSequence
    {
        protected internal override void HandleLineFinish(GDReadingState state)
        {
            Append('\n');
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != '"';
        }

        protected internal override void HandleSharpChar(GDReadingState state)
        {
            Append('#');
        }
    }
}