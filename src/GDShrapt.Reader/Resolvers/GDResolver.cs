namespace GDShrapt.Reader
{
    internal abstract class GDResolver : GDReader
    {
        public ITokenReceiver Owner { get; }

        public GDResolver(ITokenReceiver owner)
        {
            Owner = owner;
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            Owner.HandleReceivedToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            Owner.HandleReceivedToken(state.Push(new GDMultiLineSplitToken()));
            state.PassLeftSlashChar();
        }
    }
}