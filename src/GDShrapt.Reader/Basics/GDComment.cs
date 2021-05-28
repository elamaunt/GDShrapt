namespace GDShrapt.Reader
{
    public class GDComment : GDCharSequence
    {
        public GDComment()
        {
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.FinishLine();
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