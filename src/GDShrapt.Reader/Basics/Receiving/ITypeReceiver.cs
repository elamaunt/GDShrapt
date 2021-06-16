namespace GDShrapt.Reader
{
    internal interface ITypeReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDType token);
        void HandleReceivedTypeSkip();
    }
}