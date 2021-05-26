namespace GDScriptConverter
{
    public class GDIdentifier : GDCharSequence
    {
        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.FinishLine();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}