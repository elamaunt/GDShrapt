namespace GDShrapt.Reader
{
    internal class GDMatchCaseResolver : GDIntendedResolver
    {
        GDSpace _lastSpace;

        new IMatchCaseReceiver Owner { get; }

        public GDMatchCaseResolver(IMatchCaseReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                state.Push(_lastSpace = new GDSpace());
                state.PassChar(c);
                return;
            }

            SendIntendationToOwner();

            if (_lastSpace != null)
            {
                Owner.HandleReceivedToken(_lastSpace);
                _lastSpace = null;
            }

            Owner.HandleReceivedToken(state.Push(new GDMatchCaseDeclaration(LineIntendationThreshold)));
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            SendIntendationToOwner();

            if (_lastSpace != null)
            {
                Owner.HandleReceivedToken(_lastSpace);
                _lastSpace = null;
            }

            Owner.HandleReceivedToken(new GDNewLine());
            ResetIntendation();
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_lastSpace != null)
            {
                Owner.HandleReceivedToken(_lastSpace);
                _lastSpace = null;
            }

            SendIntendationToOwner();
            base.ForceComplete(state);
        }
    }
}
