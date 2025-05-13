namespace GDShrapt.Reader
{
    public abstract class GDVisitor : GDBaseVisitor, IGDVisitor
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

        public virtual void Left(GDToolAttribute a)
        {
            // Nothing
        }

        public virtual void Left(GDClassNameAttribute a)
        {
            // Nothing
        }

        public virtual void Left(GDExtendsAttribute a)
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

        public virtual void Visit(GDToolAttribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDClassNameAttribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDExtendsAttribute a)
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

        public virtual void EnterListChild(GDNode node)
        {
            // Nothing
        }

        public virtual void LeftListChild(GDNode node)
        {
            // Nothing
        }

        public virtual void Visit(GDAttribute a)
        {
            // Nothing
        }

        public virtual void Left(GDAttribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDGetAccessorBodyDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDGetAccessorBodyDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDSetAccessorBodyDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDSetAccessorBodyDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDSetAccessorMethodDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDSetAccessorMethodDeclaration d)
        {
            // Nothing
        }

        public virtual void Visit(GDSingleTypeNode t)
        {
            // Nothing
        }

        public virtual void Left(GDSingleTypeNode t)
        {
			// Nothing
		}

		public virtual void Visit(GDArrayTypeNode t)
		{
			// Nothing
		}

        public virtual void Visit(GDDictionaryTypeNode t)
        {
            // Nothing
        }

        public virtual void Left(GDArrayTypeNode t)
        {
            // Nothing
        }

        public virtual void Left(GDDictionaryTypeNode t)
        {
            // Nothing
        }

        public virtual void Visit(GDGetAccessorMethodDeclaration d)
        {
            // Nothing
        }

        public virtual void Left(GDGetAccessorMethodDeclaration d)
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

        public virtual void Left(GDMethodExpression e)
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

        public virtual void Visit(GDMethodExpression e)
        {
            // Nothing
        }

        public virtual void VisitUnknown(GDExpression e)
        {
            // Nothing
        }

        public virtual void WillVisitExpression(GDExpression e)
        {
            // Nothing
        }

        public virtual void DidLeftExpression(GDExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDAwaitExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDStringTypeNode type)
        {
            // Nothing
        }

        public virtual void Visit(GDCustomAttribute a)
        {
            // Nothing
        }

        public virtual void Visit(GDStringPartsList list)
        {
            // Nothing
        }

        public virtual void Left(GDAwaitExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDStringTypeNode type)
        {
            // Nothing
        }

        public virtual void Left(GDCustomAttribute a)
        {
            // Nothing
        }

        public virtual void Left(GDStringPartsList list)
        {
            // Nothing
        }

        public virtual void Left(GDTripleDoubleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Left(GDTripleSingleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Left(GDDoubleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Left(GDSingleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Visit(GDTripleSingleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Visit(GDTripleDoubleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Visit(GDDoubleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Visit(GDSingleQuotasStringNode sn)
        {
            // Nothing
        }

        public virtual void Visit(GDGetUniqueNodeExpression e)
        {
            // Nothing
        }

        public virtual void Left(GDGetUniqueNodeExpression e)
        {
            // Nothing
        }

        public virtual void Visit(GDSubTypeNode t)
        {
            // Nothing
        }

        public virtual void Left(GDSubTypeNode t)
        {
            // Nothing
        }
    }
}
