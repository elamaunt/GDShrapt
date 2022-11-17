namespace GDShrapt.Reader
{
    internal class GDElifResolver : GDIntendedSequenceResolver
    {
        new IIntendedTokenReceiver<GDElifBranch> Owner { get; }

        public override string Sequence => "elif";

        public GDElifResolver(IIntendedTokenReceiver<GDElifBranch> owner, int lineIntendation)
            : base(owner, lineIntendation, false)
        {
            Owner = owner;
        }

        protected override void OnFail(GDReadingState state)
        {
            // Ignore
        }

        protected override void OnMatch(GDReadingState state)
        {
            var branch = new GDElifBranch(LineIntendationThreshold);

            branch.Add(new GDElifKeyword());

            SendIntendationTokensToOwner();
            Owner.HandleReceivedToken(branch);
            state.Push(branch);
        }
    }
}
