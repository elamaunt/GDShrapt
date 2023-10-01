namespace GDShrapt.Reader
{
    public sealed class GDExpressionsList : GDCommaSeparatedList<GDExpression>,
        ITokenReceiver<GDExpression>
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

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
