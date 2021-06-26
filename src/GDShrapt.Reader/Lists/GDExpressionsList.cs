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

        public override GDNode CreateEmptyInstance()
        {
            return new GDExpressionsList();
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
