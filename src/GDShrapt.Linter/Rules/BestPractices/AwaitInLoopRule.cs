namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when 'await' is used inside a loop (for/while).
    /// This can cause performance issues as each iteration waits for the previous one to complete.
    /// </summary>
    public class GDAwaitInLoopRule : GDLintRule
    {
        public override string RuleId => "GDL212";
        public override string Name => "await-in-loop";
        public override string Description => "Warn when await is used inside a loop";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        public override void Visit(GDAwaitExpression awaitExpr)
        {
            if (Options?.WarnAwaitInLoop != true)
                return;

            if (awaitExpr?.AwaitKeyword == null)
                return;

            // Walk up the parent chain looking for a loop
            foreach (var parent in awaitExpr.Parents)
            {
                if (parent is GDForStatement || parent is GDWhileStatement)
                {
                    ReportIssue(
                        "await inside loop may cause performance issues",
                        awaitExpr.AwaitKeyword,
                        "Consider collecting all coroutines and awaiting them together with await Promise.all() or similar pattern");
                    break;
                }

                // Stop at function boundary - await in inner function is OK
                if (parent is GDMethodDeclaration)
                    break;

                // Also stop at lambda boundary
                if (parent is GDMethodExpression)
                    break;
            }
        }
    }
}
