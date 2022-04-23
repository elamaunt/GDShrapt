using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDExpressionWalker : Walker
    {
        private IExpressionsNodeVisitor _visitor;

        public bool WalkBackward { get; set; }

        public GDExpressionWalker(IExpressionsNodeVisitor visitor)
        {
            _visitor = visitor;
        }

        public override void WalkInNodes(IEnumerable<GDNode> nodes)
        {
            if (WalkBackward)
            {
                foreach (var node in nodes.Reverse().OfType<GDExpression>())
                    WalkInNode(node);
            }
            else
            {
                foreach (var node in nodes.OfType<GDExpression>())
                    WalkInNode(node);
            }
        }

        protected void WalkIn(GDArrayInitializerExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDBoolExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDBracketExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDBreakExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDBreakPointExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDCallExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDContinueExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDDictionaryInitializerExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDDualOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDGetNodeExpression e)
        {
            _visitor.Visit(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDIdentifierExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDIfExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDIndexerExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDMatchCaseVariableExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDMatchDefaultOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDMemberOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDNodePathExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDNumberExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDPassExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDReturnExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDSingleOperatorExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDStringExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkIn(GDYieldExpression e)
        {
            _visitor.Visit(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.Left(e);
        }

        protected void WalkInUnknownExpression(GDExpression e)
        {
            _visitor.VisitUnknown(e);
            _visitor.EnterNode(e);
            WalkInNodes(e.Nodes);
            _visitor.LeftNode();
            _visitor.LeftUnknown(e);
        }

        public void WalkInNode(GDExpression expr)
        {
            if (expr == null)
                return;

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
        }
    }
}
