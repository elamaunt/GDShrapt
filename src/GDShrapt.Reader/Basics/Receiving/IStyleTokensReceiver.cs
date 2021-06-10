namespace GDShrapt.Reader
{
    internal interface IStyleTokensReceiver : ITokenReceiver
    {
        void HandleReceivedToken(GDComment token);
        void HandleReceivedToken(GDNewLine token);
        void HandleReceivedToken(GDSpace token);
    }
}