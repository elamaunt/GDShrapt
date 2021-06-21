namespace GDShrapt.Reader
{
    public sealed class GDClassMembersList : GDSeparatedList<GDClassMember, GDNewLine>, IClassMembersReceiver
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDClassMembersList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.PushAndPass(new GDClassMembersResolver(this, _lineIntendationThreshold), c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDClassMembersResolver(this, _lineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.PopAndPassNewLine();
        }

        void IClassMembersReceiver.HandleReceivedToken(GDClassMember token)
        {
            ListForm.Add(token);
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }
    }
}
