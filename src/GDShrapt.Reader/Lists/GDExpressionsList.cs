namespace GDShrapt.Reader
{
    public sealed class GDExpressionsList : GDCommaSeparatedList<GDExpression>,
        ITokenReceiver<GDExpression>,
        ITokenReceiver<GDComma>

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

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }
    }
}
