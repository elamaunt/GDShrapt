namespace GDShrapt.Reader
{
    internal class GDKeywordResolver<KEYWORD> : GDSequenceResolver
        where KEYWORD : IGDKeywordToken, new()
    {
        public new IKeywordReceiver<KEYWORD> Owner { get; }

        static KEYWORD _keyword = new KEYWORD();

        public override string Sequence => _keyword.Sequence;

        public GDKeywordResolver(IKeywordReceiver<KEYWORD> owner)
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
            Owner.HandleReceivedKeywordSkip();
        }
    }
}
