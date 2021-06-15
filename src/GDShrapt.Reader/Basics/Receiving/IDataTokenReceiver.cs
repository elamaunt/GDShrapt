namespace GDShrapt.Reader
{
    internal interface IDataTokenReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDDataToken token);
        void HandleReceivedTokenSkip();
    }
}
