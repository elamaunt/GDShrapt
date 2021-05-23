namespace GDScriptConverter
{
    public class GDIdentifier : GDCharSequenceNode
    {
        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
        }
    }
}