namespace GDShrapt.Reader
{
    internal class GDSequenceTokenResolver<TOKEN> : GDSequenceResolver
        where TOKEN : GDSequenceToken, new()
    {
        public new ITokenOrSkipReceiver<TOKEN> Owner { get; }

        static TOKEN _token = new TOKEN();

        public override string Sequence => _token.Sequence;

        public GDSequenceTokenResolver(ITokenOrSkipReceiver<TOKEN> owner)
            : base(owner)
        {
            Owner = owner;
        }

        protected override void OnMatch(GDReadingState state)
        {
            Owner.HandleReceivedToken(new TOKEN());
        }

        protected override void OnFail(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
        }
    }
}
