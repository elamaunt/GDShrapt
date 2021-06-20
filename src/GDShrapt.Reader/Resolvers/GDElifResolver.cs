namespace GDShrapt.Reader
{
    internal class GDElifResolver : GDIntendedSequenceResolver
    {
        new IElifBranchReceiver Owner { get; }

        public override string Sequence => "elif";

        public GDElifResolver(IElifBranchReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        protected override void OnFail(GDReadingState state)
        {
            // Ignore
        }

        protected override void OnMatch(GDReadingState state)
        {
            var branch = new GDElifBranch();

            branch.SendKeyword(new GDElifKeyword());

            SendIntendationToOwner();
            Owner.HandleReceivedToken(branch);
            state.Push(branch);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (Index == 0)
            {
                SendIntendationToOwner();
                Owner.HandleReceivedToken(new GDNewLine());
                ResetIntendation();
                return;
            }

            base.HandleNewLineChar(state);
        }
    }
}
