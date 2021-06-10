namespace GDShrapt.Reader
{
    internal interface IStatementsReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDStatement token);
    }
}
