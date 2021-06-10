namespace GDShrapt.Reader
{
    internal interface ITokenReceiver<T> : IStyleTokensReceiver
        where T : GDSyntaxToken
    {
        void HandleReceivedToken(T token);
        void HandleReceivedTokenSkip<B>()
            where B : T;
    }

    internal interface ITokenReceiver
    {
        void HandleReceivedToken(GDInvalidToken token);
    }
}