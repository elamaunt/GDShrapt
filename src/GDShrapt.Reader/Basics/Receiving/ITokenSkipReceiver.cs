namespace GDShrapt.Reader
{
    public interface ITokenSkipReceiver<in T> : ITokenReceiver
        where T : GDSyntaxToken
    {
        void HandleReceivedTokenSkip();
    }
}
