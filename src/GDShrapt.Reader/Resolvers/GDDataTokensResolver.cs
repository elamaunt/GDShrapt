namespace GDShrapt.Reader
{
    internal class GDDataTokensResolver : GDResolver
    {
        new ITokenReceiver<GDDataToken> Owner { get; }

        public GDDataTokensResolver(ITokenReceiver<GDDataToken> owner)
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

            if (IsNumberStartChar(c) || c == '-')
            {
                Owner.HandleReceivedToken(state.Push(new GDNumber()));
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

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }
    }
}
