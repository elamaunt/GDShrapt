namespace GDShrapt.Reader
{
    public abstract class NodeVisitor : ExpressionsVisitor, INodeVisitor
    {
        public virtual void DidLeft(GDNode expr)
        {
            // Nothing
        }

        public virtual void WillVisit(GDNode expr)
        {
            // Nothing
        }
        public virtual void Left(GDWhileStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDVariableDeclarationStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDMatchStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDForStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDIfStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDExpressionStatement s)
        {
            // Nothing
        }

        public virtual void Left(GDToolAtribute a)
        {
            // Nothing
        }

        public virtual void Left(GDClassNameAtribute a)
        {
            // Nothing
        }

        public virtual void Left(GDExtendsAtribute a)
        {
            // Nothing
        }

        public virtual void Left(GDVariableDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDMethodDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDInnerClassDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDParameterDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDClassDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDDictionaryKeyValueDeclaration decl)
        {
            // Nothing
        }

        public virtual void Left(GDEnumDeclaration decl)
        {
            // Nothing
        }

        public virtual void Left(GDEnumValueDeclaration decl)
        {
            // Nothing
        }

        public virtual void Left(GDMatchCaseDeclaration decl)
        {
            // Nothing
        }

        public virtual void Left(GDSignalDeclaration decl)
        {
            // Nothing
        }

        public virtual void Left(GDClassAtributesList list)
        {
            // Nothing
        }

        public virtual void Left(GDClassMembersList list)
        {
            // Nothing
        }

        public virtual void Left(GDDictionaryKeyValueDeclarationList list)
        {
            // Nothing
        }

        public virtual void Left(GDElifBranchesList list)
        {
            // Nothing
        }

        public virtual void Left(GDEnumValuesList list)
        {
            // Nothing
        }

        public virtual void Left(GDDataParametersList list)
        {
            // Nothing
        }

        public virtual void Left(GDExpressionsList list)
        {
            // Nothing
        }

        public virtual void Left(GDMatchCasesList list)
        {
            // Nothing
        }

        public virtual void Left(GDParametersList list)
        {
            // Nothing
        }

        public virtual void Left(GDPathList list)
        {
            // Nothing
        }

        public virtual void Left(GDLayersList list)
        {
            // Nothing
        }

        public virtual void Left(GDStatementsList list)
        {
            // Nothing
        }

        public virtual void Left(GDIfBranch branch)
        {
            // Nothing
        }

        public virtual void Left(GDElseBranch branch)
        {
            // Nothing
        }

        public virtual void Left(GDElifBranch branch)
        {
            // Nothing
        }

        public virtual void LeftUnknown(GDNode node)
        {
            // Nothing
        }

        public virtual void Visit(GDClassDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDDictionaryKeyValueDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDEnumDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDEnumValueDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDInnerClassDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDMatchCaseDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDParameterDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDSignalDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDVariableDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDIfBranch b)
        {
            // Nothing
        }

        public virtual void Visit(GDElseBranch b)
        {
            // Nothing
        }

        public virtual void Visit(GDElifBranch b)
        {
            // Nothing
        }

        public virtual void Visit(GDClassAtributesList list)
        {
            // Nothing
        }

        public virtual void Visit(GDClassMembersList list)
        {
            // Nothing
        }

        public virtual void Visit(GDDictionaryKeyValueDeclarationList list)
        {
            // Nothing
        }

        public virtual void Visit(GDElifBranchesList list)
        {
            // Nothing
        }

        public virtual void Visit(GDEnumValuesList list)
        {
            // Nothing
        }

        public virtual void Visit(GDDataParametersList list)
        {
            // Nothing
        }

        public virtual void Visit(GDExpressionsList list)
        {
            // Nothing
        }

        public virtual void Visit(GDMatchCasesList list)
        {
            // Nothing
        }

        public virtual void Visit(GDParametersList list)
        {
            // Nothing
        }

        public virtual void Visit(GDPathList list)
        {
            // Nothing
        }

        public virtual void Visit(GDLayersList list)
        {
            // Nothing
        }

        public virtual void Visit(GDStatementsList list)
        {
            // Nothing
        }

        public virtual void Visit(GDMethodDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDToolAtribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDClassNameAtribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDExtendsAtribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDExpressionStatement s)
        {
            // Nothing
        }

        public virtual void Visit(GDIfStatement s)
        {
            // Nothing
        }

        public virtual void Visit(GDForStatement s)
        {
            // Nothing
        }

        public virtual void Visit(GDMatchStatement s)
        {
            // Nothing
        }

        public virtual void Visit(GDVariableDeclarationStatement s)
        {
            // Nothing
        }

        public virtual void Visit(GDWhileStatement s)
        {
            // Nothing
        }

        public virtual void VisitUnknown(GDNode node)
        {
            // Nothing
        }

        public virtual void EnterListChild(GDNode node)
        {
            // Nothing
        }
        public virtual void LeftListChild(GDNode node)
        {
            // Nothing
        }
    }
}
