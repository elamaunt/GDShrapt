namespace GDShrapt.Reader
{
    public class GDClassMembersList : GDSeparatedList<GDClassMember, GDNewLine>, IClassMembersReceiver
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

        }

        internal override void HandleLineFinish(GDReadingState state)
        {

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
