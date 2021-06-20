namespace GDShrapt.Reader
{
    internal interface IElifBranchReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDElifBranch token);
    }
}
