namespace GDShrapt.Reader
{
    internal class GDKeywordResolver<KEYWORD> : GDSequenceResolver
        where KEYWORD : GDKeyword, new()
    {
        public new ITokenOrSkipReceiver<KEYWORD> Owner { get; }

        static KEYWORD _keyword = new KEYWORD();

        public override string Sequence => _keyword.Sequence;

        public GDKeywordResolver(ITokenOrSkipReceiver<KEYWORD> owner)
            : base(owner)
        {
            Owner = owner;
        }

        protected override void OnMatch(GDReadingState state)
        {
            Owner.HandleReceivedToken(new KEYWORD());
        }

        protected override void OnFail(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
        }
    }
}
