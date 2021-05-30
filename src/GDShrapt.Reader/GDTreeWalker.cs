using System;

namespace GDShrapt.Reader
{
    public class GDTreeWalker : GDExpressionWalker
    {
        private readonly INodeVisitor _visitor;

        public GDTreeWalker(INodeVisitor visitor) 
            : base(visitor)
        {
            _visitor = visitor;
        }

        public void WalkInNode(GDNode node)
        {
            if (node == null)
                return;

            switch (node)
            {
                // Declarations
                case GDClassDeclaration classDeclaration:
                    WalkIn(classDeclaration);
                    break;
                case GDInnerClassDeclaration innerClassDeclaration:
                    WalkIn(innerClassDeclaration);
                    break;
                case GDMethodDeclaration methodDeclaration:
                    WalkIn(methodDeclaration);
                    break;
                case GDParameterDeclaration parameterDeclaration:
                    WalkIn(parameterDeclaration);
                    break;
                case GDVariableDeclaration variableDeclaration:
                    WalkIn(variableDeclaration);
                    break;

                // Atributes
                case GDExtendsAtribute extendsAtribute:
                    WalkIn(extendsAtribute);
                    break;
                case GDExportAtribute exportAtribute:
                    WalkIn(exportAtribute);
                    break;
                case GDClassNameAtribute classNameAtribute:
                    WalkIn(classNameAtribute);
                    break;
                case GDToolAtribute toolAtribute:
                    WalkIn(toolAtribute);
                    break;

                // Statements
                case GDExpressionStatement expressionStatement:
                    WalkIn(expressionStatement);
                    break;
                case GDIfStatement ifStatement:
                    WalkIn(ifStatement);
                    break;
                case GDReturnStatement returnStatement:
                    WalkIn(returnStatement);
                    break;
                case GDForStatement forStatement:
                    WalkIn(forStatement);
                    break;
                case GDMatchStatement matchStatement:
                    WalkIn(matchStatement);
                    break;
                case GDPassStatement passStatement:
                    WalkIn(passStatement);
                    break;
                case GDVariableDeclarationStatement variableDeclarationStatement:
                    WalkIn(variableDeclarationStatement);
                    break;
                case GDWhileStatement whileStatement:
                    WalkIn(whileStatement);
                    break;
                case GDYieldStatement yieldStatement:
                    WalkIn(yieldStatement);
                    break;

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
                case GDParametersExpression parametersExpression:
                    WalkIn(parametersExpression);
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
                    throw new System.Exception($"Walked in unknown node: {node.NodeName}");
            }
        }

        protected void WalkIn(GDClassDeclaration d)
        {
            _visitor.Visit(d);

            foreach (var method in d.Members)
                WalkInNode(method);

            _visitor.LeftNode();
        }

        protected void WalkIn(GDInnerClassDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDMethodDeclaration d)
        {
            _visitor.Visit(d);

            WalkInNode(d.Parameters);

            foreach (var method in d.Statements)
                WalkInNode(method);

            _visitor.LeftNode();
        }

        protected void WalkIn(GDParameterDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDVariableDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.LeftNode();
        }

        private void WalkIn(GDExtendsAtribute a)
        {
            _visitor.Visit(a);
            _visitor.LeftNode();
        }

        private void WalkIn(GDExportAtribute a)
        {
            _visitor.Visit(a);
            _visitor.LeftNode();
        }

        private void WalkIn(GDClassNameAtribute a)
        {
            _visitor.Visit(a);
            _visitor.LeftNode();
        }

        private void WalkIn(GDToolAtribute a)
        {
            _visitor.Visit(a);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDExpressionStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDIfStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDReturnStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDForStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDMatchStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDPassStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDVariableDeclarationStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDWhileStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }

        protected void WalkIn(GDYieldStatement s)
        {
            _visitor.Visit(s);
            _visitor.LeftNode();
        }
    }
}
