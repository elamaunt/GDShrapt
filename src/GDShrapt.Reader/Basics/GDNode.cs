namespace GDShrapt.Reader
{
    public abstract class GDNode
    {
        public string NodeName => GetType().Name;
        public GDComment EndLineComment { get; set; }

        internal abstract void HandleChar(char c, GDReadingState state);
        internal abstract void HandleLineFinish(GDReadingState state);

        internal bool IsSpace(char c) => c == ' ' || c == '\t';

        internal virtual void HandleSharpChar(GDReadingState state)
        {
            state.PushNode(EndLineComment = new GDComment());
        }

        internal virtual void ForceComplete(GDReadingState state)
        {
            state.PopNode();
        }
    }
}