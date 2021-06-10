namespace GDShrapt.Reader
{
    public class GDClassMembersList : GDSeparatedList<GDClassMember, GDNewLine>, IClassMembersReceiver
    {
        internal override void HandleChar(char c, GDReadingState state)
        {

        }

        internal override void HandleLineFinish(GDReadingState state)
        {

        }

        void IClassMembersReceiver.HandleReceivedToken(GDClassMember token)
        {
            TokensList.AddLast(token);
        }
    }
}
