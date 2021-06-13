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
            Owner.HandleReceivedToken(state.Push(new GDComment()));
        }
    }
}