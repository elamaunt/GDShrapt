using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about object allocations inside loops.
    /// Creating objects in loops can cause performance issues and GC pressure.
    /// </summary>
    public class GDAllocationInLoopRule : GDLintRule
    {
        public override string RuleId => "GDL240";
        public override string Name => "allocation-in-loop";
        public override string Description => "Warn about object allocations inside loops";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDCallExpression call)
        {
            if (Options?.WarnAllocationInLoop != true)
                return;

            // Check for .new() calls (e.g., Vector2.new(), Node.new())
            if (call.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new")
            {
                if (IsInsideLoop(call))
                {
                    ReportIssue(
                        "Object allocation inside loop may cause performance issues",
                        memberOp.Identifier,
                        "Consider caching the object outside the loop or using object pooling");
                }
            }
        }

        public override void Visit(GDDictionaryInitializerExpression dict)
        {
            if (Options?.WarnAllocationInLoop != true)
                return;

            if (IsInsideLoop(dict))
            {
                ReportIssue(
                    "Dictionary creation inside loop may cause performance issues",
                    dict,
                    "Consider creating the dictionary outside the loop if the same structure is needed each iteration");
            }
        }

        public override void Visit(GDArrayInitializerExpression array)
        {
            if (Options?.WarnAllocationInLoop != true)
                return;

            if (IsInsideLoop(array))
            {
                ReportIssue(
                    "Array creation inside loop may cause performance issues",
                    array,
                    "Consider creating the array outside the loop if the same structure is needed each iteration");
            }
        }

        private bool IsInsideLoop(GDNode node)
        {
            foreach (var parent in node.Parents)
            {
                if (parent is GDForStatement || parent is GDWhileStatement)
                    return true;

                // Stop at function boundary - allocation in inner function is OK
                if (parent is GDMethodDeclaration)
                    break;

                // Also stop at lambda boundary
                if (parent is GDMethodExpression)
                    break;
            }
            return false;
        }
    }
}
