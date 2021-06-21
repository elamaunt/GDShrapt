namespace GDShrapt.Reader
{
    internal interface IElseBranchReceiver : IIntendationReceiver
    {
        void HandleReceivedToken(GDElseBranch token);
        void HandleReceivedElseBranchSkip();
    }
}
