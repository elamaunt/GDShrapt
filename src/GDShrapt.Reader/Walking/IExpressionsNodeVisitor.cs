namespace GDShrapt.Reader
{
    public interface IExpressionsNodeVisitor : IVisitor
    {
        void WillVisit(GDExpression expr);
        void DidLeft(GDExpression expr);

        void Visit(GDArrayInitializerExpression e);
        void Visit(GDBoolExpression e);
        void Visit(GDBracketExpression e);
        void Visit(GDBreakExpression e);
        void Visit(GDBreakPointExpression e);
        void Visit(GDCallExpression e);
        void Visit(GDContinueExpression e);
        void Visit(GDDictionaryInitializerExpression e);
        void Visit(GDDualOperatorExpression e);
        void Visit(GDGetNodeExpression e);
        void Visit(GDIdentifierExpression e);
        void Visit(GDIfExpression e);
        void Visit(GDIndexerExpression e);
        void Visit(GDMatchCaseVariableExpression e);
        void Left(GDArrayInitializerExpression e);
        void Visit(GDMatchDefaultOperatorExpression e);
        void Visit(GDNodePathExpression e);
        void Visit(GDNumberExpression e);
        void Visit(GDMemberOperatorExpression e);
        void Visit(GDPassExpression e);
        void Left(GDBoolExpression e);
        void Visit(GDReturnExpression e);
        void Visit(GDSingleOperatorExpression e);
        void Visit(GDStringExpression e);
        void Visit(GDYieldExpression e);
        void VisitUnknown(GDExpression e);
        void LeftUnknown(GDExpression e);
        void Left(GDBracketExpression e);
        void Left(GDYieldExpression e);
        void Left(GDStringExpression e);
        void Left(GDSingleOperatorExpression e);
        void Left(GDReturnExpression e);
        void Left(GDPassExpression e);
        void Left(GDNumberExpression e);
        void Left(GDBreakExpression e);
        void Left(GDNodePathExpression e);
        void Left(GDMemberOperatorExpression e);
        void Left(GDMatchDefaultOperatorExpression e);
        void Left(GDMatchCaseVariableExpression e);
        void Left(GDIndexerExpression e);
        void Left(GDBreakPointExpression e);
        void Left(GDIfExpression e);
        void Left(GDIdentifierExpression e);
        void Left(GDGetNodeExpression e);
        void Left(GDDictionaryInitializerExpression e);
        void Left(GDContinueExpression e);
        void Left(GDCallExpression e);
        void Left(GDDualOperatorExpression e);
    }
}