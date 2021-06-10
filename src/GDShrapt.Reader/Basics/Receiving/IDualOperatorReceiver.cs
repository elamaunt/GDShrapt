namespace GDShrapt.Reader
{
    internal interface IDualOperatorReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDDualOperator token);
        void HandleDualOperatorSkip();

    }
}
