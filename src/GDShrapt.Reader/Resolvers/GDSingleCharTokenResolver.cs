namespace GDShrapt.Reader
{
    internal class GDSingleCharTokenResolver<TOKEN> : GDResolver
        where TOKEN : GDSingleCharToken, new()
    {
        public new ITokenOrSkipReceiver<TOKEN> Owner { get; }

        static TOKEN _token = new TOKEN();

        public GDSingleCharTokenResolver(ITokenOrSkipReceiver<TOKEN> owner)
            : base(owner)
        {
            Owner = owner;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();

            if (_token.Char == c)
            {
                Owner.HandleReceivedToken(new TOKEN());
                return;
            }

            Owner.HandleReceivedTokenSkip();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            HandleChar('\n', state);
        }
    }
}
