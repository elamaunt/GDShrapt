namespace GDShrapt.Reader
{
    internal interface ITokenReceiver<T> : IStyleTokensReceiver
        where T : GDSyntaxToken
    {
        void HandleReceivedToken(T token);
        void HandleReceivedTokenSkip();
    }

    internal interface ITokenReceiver
    {
        void HandleAbstractToken(GDSyntaxToken token);
        void HandleReceivedToken(GDInvalidToken token);
    }
}