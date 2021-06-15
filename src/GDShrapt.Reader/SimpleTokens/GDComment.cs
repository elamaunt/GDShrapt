namespace GDShrapt.Reader
{
    public class GDComment : GDCharSequence
    {
        public GDComment()
        {
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassNewLine();
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return true;
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        public override string ToString()
        {
            return $"#{Sequence}";
        }
    }
}