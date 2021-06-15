namespace GDShrapt.Reader
{
    internal class GDDataTokenResolver : GDResolver
    {
        new IDataTokenReceiver Owner { get; }

        public GDDataTokenResolver(IDataTokenReceiver owner)
            : base(owner)
        {
            Owner = owner;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                Owner.HandleReceivedToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (IsStringStartChar(c))
            {
                Owner.HandleReceivedToken(state.Push(new GDString()));
                state.PassChar(c);
                return;
            }

            if (IsIdentifierStartChar(c))
            {
                Owner.HandleReceivedToken(state.Push(new GDIdentifier()));
                state.PassChar(c);
                return;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
            state.Pop();
            state.PassNewLine();
        }
    }
}
