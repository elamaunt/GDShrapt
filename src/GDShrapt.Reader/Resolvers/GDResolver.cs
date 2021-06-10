namespace GDShrapt.Reader
{
    internal abstract class GDResolver : GDReader
    {
        public IStyleTokensReceiver Owner { get; }

        public GDResolver(IStyleTokensReceiver owner)
        {
            Owner = owner;
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            AppendAndPush(new GDComment(), state);
        }

        protected void AppendAndPush(GDComment token, GDReadingState state)
        {
            Owner.HandleReceivedToken(token);
            state.Push(token);
        }
        protected void AppendAndPush(GDNewLine token, GDReadingState state)
        {
            Owner.HandleReceivedToken(token);
            state.Push(token);
        }
        protected void AppendAndPush(GDSpace token, GDReadingState state)
        {
            Owner.HandleReceivedToken(token);
            state.Push(token);
        }

        protected void AppendAndPush(GDInvalidToken token, GDReadingState state)
        {
            Owner.HandleReceivedToken(token);
            state.Push(token);
        }
    }
}