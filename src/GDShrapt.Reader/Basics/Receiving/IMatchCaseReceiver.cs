namespace GDShrapt.Reader
{
    internal interface IMatchCaseReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDMatchCaseDeclaration token);
    }
}
