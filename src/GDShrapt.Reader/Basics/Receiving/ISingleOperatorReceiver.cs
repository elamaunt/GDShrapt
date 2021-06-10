namespace GDShrapt.Reader
{
    internal interface ISingleOperatorReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDSingleOperator token);
        void HandleSingleOperatorSkip();
    }
}
