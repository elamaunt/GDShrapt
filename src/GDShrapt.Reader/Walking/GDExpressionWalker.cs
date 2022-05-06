using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDExpressionWalker : Walker
    {
        protected IExpressionsNodeVisitor Visitor { get; }

        public GDExpressionWalker(IExpressionsNodeVisitor visitor)
        {
            Visitor = visitor;
        }

        public override void WalkInNodes(IEnumerable<GDNode> nodes)
        {
            foreach (var node in nodes.OfType<GDExpression>())
                WalkInNode(node);
        }

        protected virtual void WalkIn(GDArrayInitializerExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDBoolExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDBracketExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDBreakExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDBreakPointExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDCallExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDContinueExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDDictionaryInitializerExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDDualOperatorExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDGetNodeExpression e)
        {
            Visitor.Visit(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDIdentifierExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDIfExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDIndexerExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDMatchCaseVariableExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDMatchDefaultOperatorExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDMemberOperatorExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDNodePathExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDNumberExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDPassExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDReturnExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDSingleOperatorExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDStringExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkIn(GDYieldExpression e)
        {
            Visitor.Visit(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.Left(e);
        }

        protected virtual void WalkInUnknownExpression(GDExpression e)
        {
            Visitor.VisitUnknown(e);
            Visitor.EnterNode(e);
            WalkInNodes(WalkBackward ? e.NodesReversed : e.Nodes);
            Visitor.LeftNode();
            Visitor.LeftUnknown(e);
        }

        public virtual void WalkInNode(GDExpression expr)
        {
            if (expr == null)
                return;

            Visitor.WillVisit(expr);

            switch (expr)
            {
                // Expressions
                case GDArrayInitializerExpression e:
                    WalkIn(e);
                    break;
                case GDBoolExpression e:
                    WalkIn(e);
                    break;
                case GDBracketExpression e:
                    WalkIn(e);
                    break;
                case GDBreakExpression e:
                    WalkIn(e);
                    break;
                case GDBreakPointExpression e:
                    WalkIn(e);
                    break;
                case GDCallExpression e:
                    WalkIn(e);
                    break;
                case GDContinueExpression e:
                    WalkIn(e);
                    break;
                case GDDictionaryInitializerExpression e:
                    WalkIn(e);
                    break;
                case GDDualOperatorExpression e:
                    WalkIn(e);
                    break;
                case GDGetNodeExpression e:
                    WalkIn(e);
                    break;
                case GDIdentifierExpression e:
                    WalkIn(e);
                    break;
                case GDIfExpression e:
                    WalkIn(e);
                    break;
                case GDIndexerExpression e:
                    WalkIn(e);
                    break;
                case GDMatchCaseVariableExpression e:
                    WalkIn(e);
                    break;
                case GDMatchDefaultOperatorExpression e:
                    WalkIn(e);
                    break;
                case GDNodePathExpression e:
                    WalkIn(e);
                    break;
                case GDNumberExpression e:
                    WalkIn(e);
                    break;
                case GDMemberOperatorExpression e:
                    WalkIn(e);
                    break;
                case GDPassExpression e:
                    WalkIn(e);
                    break;
                case GDReturnExpression e:
                    WalkIn(e);
                    break;
                case GDSingleOperatorExpression e:
                    WalkIn(e);
                    break;
                case GDStringExpression e:
                    WalkIn(e);
                    break;
                case GDYieldExpression e:
                    WalkIn(e);
                    break;
                default:
                    WalkInUnknownExpression(expr);
                    break;
            }

            Visitor.DidLeft(expr);
        }
    }
}
