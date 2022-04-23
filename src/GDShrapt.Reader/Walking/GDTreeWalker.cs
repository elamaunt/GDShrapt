using System.Collections.Generic;
using System.Linq;

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
                case GDClassDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDDictionaryKeyValueDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDEnumDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDEnumValueDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDExportDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDInnerClassDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDMatchCaseDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDMethodDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDParameterDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDSignalDeclaration decl:
                    WalkIn(decl);
                    break;
                case GDVariableDeclaration decl:
                    WalkIn(decl);
                    break;

                // Lists
                case GDClassAtributesList list:
                    WalkIn(list);
                    break;
                case GDClassMembersList list:
                    WalkIn(list);
                    break;
                case GDDictionaryKeyValueDeclarationList list:
                    WalkIn(list);
                    break;
                case GDElifBranchesList list:
                    WalkIn(list);
                    break;
                case GDEnumValuesList list:
                    WalkIn(list);
                    break;
                case GDExportParametersList list:
                    WalkIn(list);
                    break;
                case GDExpressionsList list:
                    WalkIn(list);
                    break;
                case GDMatchCasesList list:
                    WalkIn(list);
                    break;
                case GDParametersList list:
                    WalkIn(list);
                    break;
                case GDPathList list:
                    WalkIn(list);
                    break;
                case GDStatementsList list:
                    WalkIn(list);
                    break;

                // Atributes
                case GDExtendsAtribute atr:
                    WalkIn(atr);
                    break;
                case GDClassNameAtribute atr:
                    WalkIn(atr);
                    break;
                case GDToolAtribute atr:
                    WalkIn(atr);
                    break;

                // Statements
                case GDExpressionStatement st:
                    WalkIn(st);
                    break;
                case GDForStatement st:
                    WalkIn(st);
                    break;
                case GDIfStatement st:
                    WalkIn(st);
                    break;
                case GDMatchStatement st:
                    WalkIn(st);
                    break;
                case GDVariableDeclarationStatement st:
                    WalkIn(st);
                    break;
                case GDWhileStatement st:
                    WalkIn(st);
                    break;

                // Branches
                case GDIfBranch branch:
                    WalkIn(branch);
                    break;
                case GDElseBranch branch:
                    WalkIn(branch);
                    break;
                case GDElifBranch branch:
                    WalkIn(branch);
                    break;

                // Expressions
                case GDExpression e:
                    base.WalkInNode(e);
                    break;
                default:
                    WalkInUnknown(node);
                    break;
            }
        }

        private void WalkIn(GDElifBranch branch)
        {
            _visitor.Visit(branch);
            _visitor.EnterNode(branch);
            WalkInNodes(branch.Nodes);
            _visitor.LeftNode();
            _visitor.Left(branch);
        }

        private void WalkIn(GDElseBranch branch)
        {
            _visitor.Visit(branch);
            _visitor.EnterNode(branch);
            WalkInNodes(branch.Nodes);
            _visitor.LeftNode();
            _visitor.Left(branch);
        }

        private void WalkIn(GDIfBranch branch)
        {
            _visitor.Visit(branch);
            _visitor.EnterNode(branch);
            WalkInNodes(branch.Nodes);
            _visitor.LeftNode();
            _visitor.Left(branch);
        }

        private void WalkIn(GDStatementsList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDPathList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDParametersList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDMatchCasesList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDExpressionsList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDExportParametersList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDEnumValuesList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDElifBranchesList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDDictionaryKeyValueDeclarationList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDClassMembersList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDClassAtributesList list)
        {
            _visitor.Visit(list);
            _visitor.EnterNode(list);
            WalkInNodes(list.Nodes);
            _visitor.LeftNode();
            _visitor.Left(list);
        }

        private void WalkIn(GDSignalDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        private void WalkIn(GDMatchCaseDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        private void WalkIn(GDExportDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        private void WalkIn(GDEnumValueDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        private void WalkIn(GDEnumDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        private void WalkIn(GDDictionaryKeyValueDeclaration decl)
        {
            _visitor.Visit(decl);
            _visitor.EnterNode(decl);
            WalkInNodes(decl.Nodes);
            _visitor.LeftNode();
            _visitor.Left(decl);
        }

        protected void WalkIn(GDClassDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.EnterNode(d);
            WalkInNodes(d.Nodes);
            _visitor.LeftNode();
            _visitor.Left(d);
        }

        protected void WalkIn(GDInnerClassDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.EnterNode(d);
            WalkInNodes(d.Nodes);
            _visitor.LeftNode();
            _visitor.Left(d);
        }

        protected void WalkIn(GDMethodDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.EnterNode(d);
            WalkInNodes(d.Nodes);
            _visitor.LeftNode();
            _visitor.Left(d);
        }

        protected void WalkIn(GDParameterDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.EnterNode(d);
            WalkInNodes(d.Nodes);
            _visitor.LeftNode();
            _visitor.Left(d);
        }

        protected void WalkIn(GDVariableDeclaration d)
        {
            _visitor.Visit(d);
            _visitor.EnterNode(d);
            WalkInNodes(d.Nodes);
            _visitor.LeftNode();
            _visitor.Left(d);
        }

        private void WalkIn(GDExtendsAtribute a)
        {
            _visitor.Visit(a);
            _visitor.EnterNode(a);
            WalkInNodes(a.Nodes);
            _visitor.LeftNode();
            _visitor.Left(a);
        }

        private void WalkIn(GDClassNameAtribute a)
        {
            _visitor.Visit(a);
            _visitor.EnterNode(a);
            WalkInNodes(a.Nodes);
            _visitor.LeftNode();
            _visitor.Left(a);
        }

        private void WalkIn(GDToolAtribute a)
        {
            _visitor.Visit(a);
            _visitor.EnterNode(a);
            WalkInNodes(a.Nodes);
            _visitor.LeftNode();
            _visitor.Left(a);
        }

        protected void WalkIn(GDExpressionStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkIn(GDIfStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkIn(GDForStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkIn(GDMatchStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkIn(GDVariableDeclarationStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkIn(GDWhileStatement s)
        {
            _visitor.Visit(s);
            _visitor.EnterNode(s);
            WalkInNodes(s.Nodes);
            _visitor.LeftNode();
            _visitor.Left(s);
        }

        protected void WalkInUnknown(GDNode node)
        {
            _visitor.VisitUnknown(node);
            _visitor.EnterNode(node);
            WalkInNodes(node.Nodes);
            _visitor.LeftNode();
            _visitor.LeftUnknown(node);
        }
        public override void WalkInNodes(IEnumerable<GDNode> nodes)
        {
            if (WalkBackward)
            {
                foreach (var node in nodes.Reverse())
                    WalkInNode(node);
            }
            else
            {
                foreach (var node in nodes)
                    WalkInNode(node);
            }
        }
    }
}
