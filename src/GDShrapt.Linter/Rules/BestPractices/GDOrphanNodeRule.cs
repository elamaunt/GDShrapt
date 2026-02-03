using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when Node.new() is called but the node is never added to the tree or freed.
    /// This can lead to memory leaks as orphan nodes are not automatically cleaned up.
    /// </summary>
    public class GDOrphanNodeRule : GDLintRule
    {
        public override string RuleId => "GDL245";
        public override string Name => "orphan-node";
        public override string Description => "Warn about orphan nodes (created but not added to tree)";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        private readonly Dictionary<string, GDNode> _nodeCreations = new Dictionary<string, GDNode>();
        private readonly HashSet<string> _consumedNodes = new HashSet<string>();
        private readonly HashSet<string> _classVariables = new HashSet<string>();

        private static readonly HashSet<string> NodeConsumingMethods = new HashSet<string>
        {
            "add_child",
            "add_sibling",
            "call_deferred",
            "queue_free",
            "free"
        };

        public override void Visit(GDClassDeclaration classDecl)
        {
            _classVariables.Clear();

            // Collect class-level variables
            foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
            {
                if (member is GDVariableDeclaration varDecl)
                {
                    var name = varDecl.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                        _classVariables.Add(name);
                }
            }
        }

        public override void Visit(GDMethodDeclaration method)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            _nodeCreations.Clear();
            _consumedNodes.Clear();
        }

        public override void Visit(GDVariableDeclarationStatement varDecl)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            // Track: var node = SomeNode.new()
            if (varDecl.Initializer != null && IsNodeCreation(varDecl.Initializer))
            {
                var varName = varDecl.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName))
                    _nodeCreations[varName] = varDecl;
            }
        }

        public override void Visit(GDCallExpression call)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            string methodName = GetCallMethodName(call);
            if (methodName == null)
                return;

            // Check for consuming methods
            if (NodeConsumingMethods.Contains(methodName))
            {
                // Get first argument
                var args = call.Parameters?.ToList();
                if (args != null && args.Count > 0)
                {
                    if (args[0] is GDIdentifierExpression idExpr)
                    {
                        var varName = idExpr.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(varName))
                            _consumedNodes.Add(varName);
                    }
                }
            }

            // Check for method call on node: node.queue_free()
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                if (memberOp.CallerExpression is GDIdentifierExpression targetId &&
                    (methodName == "queue_free" || methodName == "free"))
                {
                    var varName = targetId.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(varName))
                        _consumedNodes.Add(varName);
                }
            }
        }

        public override void Visit(GDReturnExpression ret)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            // Node returned from function - considered consumed
            if (ret.Expression is GDIdentifierExpression id)
            {
                var varName = id.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName))
                    _consumedNodes.Add(varName);
            }
        }

        public override void Visit(GDDualOperatorExpression dual)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            // Check for assignment to class variable: self.node = created_node
            if (dual.OperatorType == GDDualOperatorType.Assignment)
            {
                if (dual.RightExpression is GDIdentifierExpression rightId)
                {
                    var varName = rightId.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(varName))
                    {
                        // If assigned to class variable or member, consider it consumed
                        if (dual.LeftExpression is GDMemberOperatorExpression ||
                            (dual.LeftExpression is GDIdentifierExpression leftId &&
                             _classVariables.Contains(leftId.Identifier?.Sequence ?? "")))
                        {
                            _consumedNodes.Add(varName);
                        }
                    }
                }
            }
        }

        public override void Left(GDMethodDeclaration method)
        {
            if (Options?.WarnOrphanNode != true)
                return;

            // Report orphaned nodes
            foreach (var kvp in _nodeCreations)
            {
                if (!_consumedNodes.Contains(kvp.Key))
                {
                    ReportIssue(
                        $"Node '{kvp.Key}' created but never added to scene tree or freed",
                        kvp.Value,
                        "Add the node with add_child() or free it with queue_free() to prevent memory leak");
                }
            }
        }

        private bool IsNodeCreation(GDExpression expr)
        {
            // Check for ClassName.new()
            if (expr is GDCallExpression call &&
                call.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new")
            {
                return true;
            }

            // Check for load("scene.tscn").instantiate()
            if (expr is GDCallExpression instantiateCall &&
                instantiateCall.CallerExpression is GDMemberOperatorExpression instMemberOp &&
                instMemberOp.Identifier?.Sequence == "instantiate")
            {
                return true;
            }

            return false;
        }

        private string GetCallMethodName(GDCallExpression call)
        {
            if (call.CallerExpression is GDIdentifierExpression idExpr)
                return idExpr.Identifier?.Sequence;

            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
                return memberOp.Identifier?.Sequence;

            return null;
        }
    }
}
