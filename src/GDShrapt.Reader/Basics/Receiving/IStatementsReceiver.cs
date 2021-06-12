namespace GDShrapt.Reader
{
    internal interface IStatementsReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDStatement token);
    }
}
