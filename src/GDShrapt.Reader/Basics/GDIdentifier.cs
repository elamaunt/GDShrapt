namespace GDShrapt.Reader
{
    public class GDIdentifier : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}