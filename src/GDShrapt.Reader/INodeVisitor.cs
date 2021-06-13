namespace GDShrapt.Reader
{
    public interface INodeVisitor : IExpressionsNodeVisitor
    {
        void Visit(GDClassDeclaration d);
        void Visit(GDInnerClassDeclaration d);
        void Visit(GDParameterDeclaration d);
        void Visit(GDVariableDeclaration d);
        void Visit(GDMethodDeclaration d);
        void Visit(GDToolAtribute a);
        void Visit(GDClassNameAtribute a);
        void Visit(GDExtendsAtribute a);
        void Visit(GDExpressionStatement s);
        void Visit(GDIfStatement s);
        void Visit(GDForStatement s);
        void Visit(GDMatchStatement s);
        void Visit(GDVariableDeclarationStatement s);
        void Visit(GDWhileStatement s);
        void Visit(GDYieldStatement s);
        new void LeftNode();
    }
}