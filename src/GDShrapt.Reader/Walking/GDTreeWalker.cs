using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDTreeWalker : GDExpressionWalker
    {
        protected new INodeVisitor Visitor { get; }
        public GDTreeWalker(INodeVisitor visitor) 
            : base(visitor)
        {
            Visitor = visitor;
        }

        public virtual void WalkInNode(GDNode node)
        {
            if (node == null)
                return;

            Visitor.WillVisit(node);

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
                case GDDataParametersList list:
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
                case GDLayersList list:
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

            Visitor.DidLeft(node);
        }

        public void WalkIn(GDElifBranch branch)
        {
            Visitor.Visit(branch);
            Visitor.EnterNode(branch);
            WalkInNodes(WalkBackward ? branch.NodesReversed : branch.Nodes);
            Visitor.LeftNode();
            Visitor.Left(branch);
        }

        public void WalkIn(GDElseBranch branch)
        {
            Visitor.Visit(branch);
            Visitor.EnterNode(branch);
            WalkInNodes(WalkBackward ? branch.NodesReversed : branch.Nodes);
            Visitor.LeftNode();
            Visitor.Left(branch);
        }

        public void WalkIn(GDIfBranch branch)
        {
            Visitor.Visit(branch);
            Visitor.EnterNode(branch);
            WalkInNodes(WalkBackward ? branch.NodesReversed : branch.Nodes);
            Visitor.LeftNode();
            Visitor.Left(branch);
        }

        public void WalkIn(GDStatementsList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDPathList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDLayersList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDParametersList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDMatchCasesList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDExpressionsList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDDataParametersList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDEnumValuesList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDElifBranchesList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDDictionaryKeyValueDeclarationList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDClassMembersList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDClassAtributesList list)
        {
            Visitor.Visit(list);
            Visitor.EnterNode(list);
            WalkInListNodes(WalkBackward ? list.NodesReversed : list.Nodes);
            Visitor.LeftNode();
            Visitor.Left(list);
        }

        public void WalkIn(GDSignalDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDMatchCaseDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDExportDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDEnumValueDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDEnumDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDDictionaryKeyValueDeclaration decl)
        {
            Visitor.Visit(decl);
            Visitor.EnterNode(decl);
            WalkInNodes(WalkBackward ? decl.NodesReversed : decl.Nodes);
            Visitor.LeftNode();
            Visitor.Left(decl);
        }

        public void WalkIn(GDClassDeclaration d)
        {
            Visitor.Visit(d);
            Visitor.EnterNode(d);
            WalkInNodes(WalkBackward ? d.NodesReversed : d.Nodes);
            Visitor.LeftNode();
            Visitor.Left(d);
        }

        public void WalkIn(GDInnerClassDeclaration d)
        {
            Visitor.Visit(d);
            Visitor.EnterNode(d);
            WalkInNodes(WalkBackward ? d.NodesReversed : d.Nodes);
            Visitor.LeftNode();
            Visitor.Left(d);
        }

        public void WalkIn(GDMethodDeclaration d)
        {
            Visitor.Visit(d);
            Visitor.EnterNode(d);
            WalkInNodes(WalkBackward ? d.NodesReversed : d.Nodes);
            Visitor.LeftNode();
            Visitor.Left(d);
        }

        public void WalkIn(GDParameterDeclaration d)
        {
            Visitor.Visit(d);
            Visitor.EnterNode(d);
            WalkInNodes(WalkBackward ? d.NodesReversed : d.Nodes);
            Visitor.LeftNode();
            Visitor.Left(d);
        }

        public void WalkIn(GDVariableDeclaration d)
        {
            Visitor.Visit(d);
            Visitor.EnterNode(d);
            WalkInNodes(WalkBackward ? d.NodesReversed : d.Nodes);
            Visitor.LeftNode();
            Visitor.Left(d);
        }

        public void WalkIn(GDExtendsAtribute a)
        {
            Visitor.Visit(a);
            Visitor.EnterNode(a);
            WalkInNodes(WalkBackward ? a.NodesReversed : a.Nodes);
            Visitor.LeftNode();
            Visitor.Left(a);
        }

        public void WalkIn(GDClassNameAtribute a)
        {
            Visitor.Visit(a);
            Visitor.EnterNode(a);
            WalkInNodes(WalkBackward ? a.NodesReversed : a.Nodes);
            Visitor.LeftNode();
            Visitor.Left(a);
        }

        public void WalkIn(GDToolAtribute a)
        {
            Visitor.Visit(a);
            Visitor.EnterNode(a);
            WalkInNodes(WalkBackward ? a.NodesReversed : a.Nodes);
            Visitor.LeftNode();
            Visitor.Left(a);
        }

        public void WalkIn(GDExpressionStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        public void WalkIn(GDIfStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        public void WalkIn(GDForStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        public void WalkIn(GDMatchStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        public void WalkIn(GDVariableDeclarationStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        protected void WalkIn(GDWhileStatement s)
        {
            Visitor.Visit(s);
            Visitor.EnterNode(s);
            WalkInNodes(WalkBackward ? s.NodesReversed : s.Nodes);
            Visitor.LeftNode();
            Visitor.Left(s);
        }

        protected virtual void WalkInUnknown(GDNode node)
        {
            Visitor.VisitUnknown(node);
            Visitor.EnterNode(node);
            WalkInNodes(WalkBackward ? node.NodesReversed : node.Nodes);
            Visitor.LeftNode();
            Visitor.LeftUnknown(node);
        }

        public virtual void WalkInListNodes(IEnumerable<GDNode> nodes)
        {
            foreach (var node in nodes)
            {
                Visitor.EnterListChild(node);
                WalkInNode(node);
                Visitor.LeftListChild(node);
            }
        }

        public override void WalkInNodes(IEnumerable<GDNode> nodes)
        {
            foreach (var node in nodes)
                WalkInNode(node);
        }
    }
}
