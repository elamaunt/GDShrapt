namespace GDShrapt.Reader
{
    internal interface IIdentifierReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDIdentifier token);

    }
}