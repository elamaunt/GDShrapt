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
        void HandleReceivedToken(GDInvalidToken token);
        void HandleReceivedAbstractToken(GDSyntaxToken token);
    }
}