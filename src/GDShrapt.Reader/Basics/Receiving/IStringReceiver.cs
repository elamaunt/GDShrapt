namespace GDShrapt.Reader
{
    internal interface IStringReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDString token);
        void HandleReceivedStringSkip();
    }
}
