namespace GDScriptConverter
{
    public abstract class GDNode
    {
        public GDComment EndLineComment { get; set; }

        protected internal abstract void HandleChar(char c, GDReadingState state);
        protected internal abstract void HandleLineFinish(GDReadingState state);

        protected bool IsSpace(char c) => c == ' ' || c == '\t';

        protected internal virtual void HandleSharpChar(GDReadingState state)
        {
            state.PushNode(EndLineComment = new GDComment());
        }

        protected internal virtual void ForceComplete(GDReadingState state)
        {
            state.PopNode();
        }
    }
}