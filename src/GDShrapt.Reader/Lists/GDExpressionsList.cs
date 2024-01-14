namespace GDShrapt.Reader
{
    public sealed class GDExpressionsList : GDCommaSeparatedList<GDExpression>,
        ITokenOrSkipReceiver<GDExpression>
    {
        readonly int _intendation;

        internal GDExpressionsList(int intendation)
        {
            _intendation = intendation;
        }

        public GDExpressionsList()
        {
        }

        internal override GDReader ResolveNode()
        {
            return new GDExpressionResolver(this, _intendation);
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

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            SetAsCompleted();
        }
    }
}
