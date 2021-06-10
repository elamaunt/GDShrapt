namespace GDShrapt.Reader
{
    internal interface IExpressionsReceiver : IStyleTokensReceiver
    {
        void HandleReceivedToken(GDExpression token);
        void HandleReceivedExpressionSkip();
    }
}
