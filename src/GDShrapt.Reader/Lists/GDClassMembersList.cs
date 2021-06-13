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

        public GDClassMembersList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDClassMemberResolver(this, _lineIntendationThreshold));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
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
