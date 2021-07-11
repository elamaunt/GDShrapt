namespace GDShrapt.Reader
{
    internal sealed class GDElseResolver : GDIntendedSequenceResolver
    {
        new IIntendedTokenOrSkipReceiver<GDElseBranch> Owner { get; }

        public override string Sequence => "else";

        public GDElseResolver(IIntendedTokenOrSkipReceiver<GDElseBranch> owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        protected override void OnFail(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
        }

        protected override void OnMatch(GDReadingState state)
        {
            var branch = new GDElseBranch(LineIntendationThreshold);

            branch.Add(new GDElseKeyword());

            SendIntendationTokensToOwner();
            Owner.HandleReceivedToken(branch);
            state.Push(branch);
        }
    }
}
