namespace GDShrapt.Reader
{
    public interface INodeVisitor : IExpressionsNodeVisitor
    {
        void WillVisit(GDNode node);
        void DidLeft(GDNode node);

        void Visit(GDClassDeclaration d);
        void Visit(GDDictionaryKeyValueDeclaration d);
        void Visit(GDEnumDeclaration d);
        void Visit(GDEnumValueDeclaration d);
        void Visit(GDExportDeclaration d);
        void Visit(GDInnerClassDeclaration d);
        void Visit(GDMatchCaseDeclaration d);
        void Visit(GDParameterDeclaration d);
        void Visit(GDSignalDeclaration d);
        void Visit(GDVariableDeclaration d);

        void Visit(GDIfBranch b);
        void Visit(GDElseBranch b);
        void Visit(GDElifBranch b);

        void Visit(GDClassAtributesList list);
        void Visit(GDClassMembersList list);
        void Visit(GDDictionaryKeyValueDeclarationList list);
        void Visit(GDElifBranchesList list);
        void Visit(GDEnumValuesList list);
        void Visit(GDExportParametersList list);
        void Visit(GDExpressionsList list);
        void Visit(GDMatchCasesList list);
        void Visit(GDParametersList list);
        void Visit(GDPathList list);
        void Visit(GDLayersList list);
        void Visit(GDStatementsList list);

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
        void VisitUnknown(GDNode node);
        void LeftUnknown(GDNode node);
        void Left(GDWhileStatement s);
        void Left(GDVariableDeclarationStatement s);
        void Left(GDMatchStatement s);
        void Left(GDForStatement s);
        void Left(GDIfStatement s);
        void Left(GDExpressionStatement s);
        void Left(GDToolAtribute a);
        void Left(GDClassNameAtribute a);
        void Left(GDExtendsAtribute a);
        void Left(GDVariableDeclaration d);
        void Left(GDMethodDeclaration d);
        void Left(GDInnerClassDeclaration d);
        void Left(GDParameterDeclaration d);
        void Left(GDClassDeclaration d);
        void Left(GDDictionaryKeyValueDeclaration decl);
        void Left(GDEnumDeclaration decl);
        void Left(GDEnumValueDeclaration decl);
        void Left(GDExportDeclaration decl);
        void Left(GDMatchCaseDeclaration decl);
        void Left(GDSignalDeclaration decl);
        void Left(GDClassAtributesList list);
        void Left(GDClassMembersList list);
        void Left(GDDictionaryKeyValueDeclarationList list);
        void Left(GDElifBranchesList list);
        void Left(GDEnumValuesList list);
        void Left(GDExportParametersList list);
        void Left(GDExpressionsList list);
        void Left(GDMatchCasesList list);
        void Left(GDParametersList list);
        void Left(GDPathList list);
        void Left(GDLayersList list);
        void Left(GDStatementsList list);
        void Left(GDIfBranch branch);
        void Left(GDElseBranch branch);
        void Left(GDElifBranch branch);

        void EnterListChild(GDNode node);
        void LeftListChild(GDNode node);
    }
}