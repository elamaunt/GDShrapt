namespace GDShrapt.Reader
{
    public abstract class ExpressionsVisitor : Visitor, IExpressionsNodeVisitor
    {
        public virtual void DidLeft(GDExpression expr)
        {
            // Nothing
        }

        public virtual void WillVisit(GDExpression expr)
        {
            // Nothing
        }

        public virtual void Left(GDArrayInitializerExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDBoolExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDBracketExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDYieldExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDStringExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDSingleOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDReturnExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDPassExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDNumberExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDBreakExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDNodePathExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDMemberOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDMatchDefaultOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDMatchCaseVariableExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDIndexerExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDBreakPointExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDIfExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDIdentifierExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDGetNodeExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDDictionaryInitializerExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDContinueExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDCallExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDDualOperatorExpression e)
        {
            // Nothing
        }

        public virtual void LeftUnknown(GDExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDArrayInitializerExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDBoolExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDBracketExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDBreakExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDBreakPointExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDCallExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDContinueExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDDictionaryInitializerExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDDualOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDGetNodeExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDIdentifierExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDIfExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDIndexerExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDMatchCaseVariableExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDMatchDefaultOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDNodePathExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDNumberExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDMemberOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDPassExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDReturnExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDSingleOperatorExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDStringExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDYieldExpression e)
        {
            // Nothing
        }
        public virtual void VisitUnknown(GDExpression e)
        {
            // Nothing
        }
    }
}