namespace GDShrapt.Reader
{
    public class GDExpressionWalker
    {
        private IExpressionsNodeVisitor _visitor;

        public GDExpressionWalker(IExpressionsNodeVisitor visitor)
        {
            _visitor = visitor;
        }

        protected void WalkIn(GDArrayInitializerExpression e)
        {
            _visitor.Visit(e);

            _visitor.LeftNode();
        }

        protected void WalkIn(GDBracketExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDCallExression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDDualOperatorExression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDIdentifierExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDIndexerExression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDMemberOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDNumberExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDSingleOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDStringExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDReturnExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDPassExpression e)
        {
            _visitor.Visit(e);
            _visitor.LeftNode();
        }

        public void WalkInNode(GDExpression expr)
        {
            if (expr == null)
                return;

            switch (expr)
            {
                // Expressions
                case GDArrayInitializerExpression arrayInitializerExpression:
                    WalkIn(arrayInitializerExpression);
                    break;
                case GDBracketExpression bracketExpression:
                    WalkIn(bracketExpression);
                    break;
                case GDCallExression callExression:
                    WalkIn(callExression);
                    break;
                case GDDualOperatorExression dualOperatorExression:
                    WalkIn(dualOperatorExression);
                    break;
                case GDIdentifierExpression identifierExpression:
                    WalkIn(identifierExpression);
                    break;
                case GDIndexerExression indexerExression:
                    WalkIn(indexerExression);
                    break;
                case GDMemberOperatorExpression memberOperatorExpression:
                    WalkIn(memberOperatorExpression);
                    break;
                case GDNumberExpression numberExpression:
                    WalkIn(numberExpression);
                    break;
                case GDSingleOperatorExpression singleOperatorExpression:
                    WalkIn(singleOperatorExpression);
                    break;
                case GDStringExpression stringExpression:
                    WalkIn(stringExpression);
                    break;
                case GDReturnExpression returnExpression:
                    WalkIn(returnExpression);
                    break;
                case GDPassExpression passExpression:
                    WalkIn(passExpression);
                    break;
                default:
                    throw new System.Exception($"Walked in unknown node: {expr.NodeName}");
            }
        }
    }
}
