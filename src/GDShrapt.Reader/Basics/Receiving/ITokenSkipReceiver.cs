namespace GDShrapt.Reader
{
    public interface ITokenSkipReceiver<T> : ITokenReceiver
        where T : GDSyntaxToken
    {
        void HandleReceivedTokenSkip();
    }
}
