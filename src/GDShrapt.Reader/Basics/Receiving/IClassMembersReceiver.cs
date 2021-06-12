namespace GDShrapt.Reader
{
    internal interface IClassMembersReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDClassMember token);
    }
}
