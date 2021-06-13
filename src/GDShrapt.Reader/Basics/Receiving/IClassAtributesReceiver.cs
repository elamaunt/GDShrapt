namespace GDShrapt.Reader
{
    internal interface IClassAtributesReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDClassAtribute token);
    }
}
