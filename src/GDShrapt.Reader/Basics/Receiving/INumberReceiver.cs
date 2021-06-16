namespace GDShrapt.Reader
{
    internal interface INumberReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDNumber token);
        void HandleReceivedNumberSkip();
    }
}