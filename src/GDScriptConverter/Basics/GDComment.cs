namespace GDScriptConverter
{
    public class GDComment : GDCharSequence
    {
        public GDComment()
        {
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.LineFinished();
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return true;
        }

        protected internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }
    }
}