using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about get_node() or $path usage in _process/_physics_process methods.
    /// These lookups should be cached in @onready variables for performance.
    /// </summary>
    public class GDProcessGetNodeRule : GDLintRule
    {
        public override string RuleId => "GDL241";
        public override string Name => "process-get-node";
        public override string Description => "Warn about node lookups in process methods";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        private bool _inProcessMethod;

        private static readonly HashSet<string> ProcessMethods = new HashSet<string>
        {
            "_process",
            "_physics_process",
            "_input",
            "_unhandled_input",
            "_unhandled_key_input"
        };

        private static readonly HashSet<string> NodeLookupMethods = new HashSet<string>
        {
            "get_node",
            "get_node_or_null",
            "find_child",
            "find_children",
            "get_child",
            "get_children"
        };

        public override void Visit(GDMethodDeclaration method)
        {
            if (Options?.WarnProcessGetNode != true)
                return;

            var name = method.Identifier?.Sequence;
            _inProcessMethod = name != null && ProcessMethods.Contains(name);
        }

        public override void Left(GDMethodDeclaration method)
        {
            _inProcessMethod = false;
        }

        public override void Visit(GDCallExpression call)
        {
            if (!_inProcessMethod || Options?.WarnProcessGetNode != true)
                return;

            string methodName = GetCallMethodName(call);
            if (methodName != null && NodeLookupMethods.Contains(methodName))
            {
                ReportIssue(
                    $"'{methodName}' called in process method impacts performance",
                    call,
                    "Cache the node reference in an @onready variable");
            }
        }

        public override void Visit(GDGetNodeExpression getNode)
        {
            if (!_inProcessMethod || Options?.WarnProcessGetNode != true)
                return;

            ReportIssue(
                "$ node path access in process method impacts performance",
                getNode,
                "Cache the node reference in an @onready variable");
        }

        private string GetCallMethodName(GDCallExpression call)
        {
            // Direct call: get_node("path")
            if (call.CallerExpression is GDIdentifierExpression idExpr)
                return idExpr.Identifier?.Sequence;

            // Member call: self.get_node("path")
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
                return memberOp.Identifier?.Sequence;

            return null;
        }
    }
}
