namespace GDShrapt.Reader
{
    internal interface IClassMembersReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDClassMember token);

    }
}
