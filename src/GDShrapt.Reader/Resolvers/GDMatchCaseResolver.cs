namespace GDShrapt.Reader
{
    internal class GDMatchCaseResolver : GDIntendedResolver
    {
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
                AppendAndPush(new GDSpace(), state);
                state.PassChar(c);
                return;
            }

            var declaration = new GDMatchCaseDeclaration(LineIntendationThreshold);
            Owner.HandleReceivedToken(declaration);
            state.Push(declaration);
            state.PassChar(c);
        }
    }
}
