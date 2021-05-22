namespace GDScriptConverter
{
    public class GDComment : GDCharSequenceNode
    {
        public GDComment()
        {
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return true;
        }

        public override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }
    }
}