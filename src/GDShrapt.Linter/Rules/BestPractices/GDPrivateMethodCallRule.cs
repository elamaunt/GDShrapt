namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when calling private methods (prefixed with _) from outside the class.
    /// Private methods starting with underscore are a convention in GDScript.
    /// Compatible with gdtoolkit's private-method-call rule.
    /// </summary>
    public class GDPrivateMethodCallRule : GDLintRule
    {
        public override string RuleId => "GDL218";
        public override string Name => "private-method-call";
        public override string Description => "Calling private methods (prefixed with _) from outside the class";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDCallExpression call)
        {
            // Check if this is a method call on an object (obj._method())
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;

                // Check if it's a private method (starts with _)
                if (!string.IsNullOrEmpty(methodName) && methodName.StartsWith("_"))
                {
                    // Allow calls on 'self'
                    if (!IsSelfCall(memberOp))
                    {
                        ReportIssue(
                            $"Calling private method '{methodName}' from outside the class",
                            memberOp.Identifier,
                            $"Consider using a public method instead, or ensure this is intentional");
                    }
                }
            }

            base.Visit(call);
        }

        private bool IsSelfCall(GDMemberOperatorExpression memberOp)
        {
            // Check if the caller is 'self'
            if (memberOp.CallerExpression is GDIdentifierExpression idExpr)
            {
                var callerName = idExpr.Identifier?.Sequence;
                return callerName == "self";
            }

            return false;
        }
    }
}
