namespace GDShrapt.Reader
{
    internal class GDSingleCharTokenResolver<TOKEN> : GDResolver
        where TOKEN : GDSingleCharToken, new()
    {
        public new ITokenReceiver<TOKEN> Owner { get; }

        static TOKEN _token = new TOKEN();

        public GDSingleCharTokenResolver(ITokenReceiver<TOKEN> owner)
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

            Owner.HandleReceivedTokenSkip<TOKEN>();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
