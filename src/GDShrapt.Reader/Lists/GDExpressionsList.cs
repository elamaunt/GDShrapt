namespace GDShrapt.Reader
{
    public sealed class GDExpressionsList : GDCommaSeparatedList<GDExpression>, IExpressionsReceiver
    {
        internal override GDReader ResolveNode()
        {
            return new GDExpressionResolver(this);
        }

        internal override bool IsStopChar(char c)
        {
            return c.IsExpressionStopChar();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            ListForm.Add(token);
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
        }
    }
}
