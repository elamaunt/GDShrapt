namespace GDShrapt.Reader
{
    internal interface IMatchCaseReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDMatchCaseDeclaration token);
    }
}
