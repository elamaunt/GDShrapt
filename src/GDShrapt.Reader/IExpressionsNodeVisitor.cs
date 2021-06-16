namespace GDShrapt.Reader
{
    public interface IExpressionsNodeVisitor
    {
        void Visit(GDArrayInitializerExpression e);
        void Visit(GDBracketExpression e);
        void Visit(GDCallExression e);
        void Visit(GDDualOperatorExression e);
        void Visit(GDIdentifierExpression e);
        void Visit(GDIndexerExression e);
        void Visit(GDMemberOperatorExpression e);
        void Visit(GDNumberExpression e);
        void Visit(GDSingleOperatorExpression e);
        void Visit(GDStringExpression e);
        void Visit(GDReturnExpression e);
        void Visit(GDPassExpression e);

        void LeftNode();
    }
}