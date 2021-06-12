namespace GDShrapt.Reader
{
    internal interface IIntendationReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDIntendation token);
    }
}
