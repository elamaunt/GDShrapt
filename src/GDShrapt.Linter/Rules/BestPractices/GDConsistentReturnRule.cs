using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a function has inconsistent return statements -
    /// some return values and some don't.
    /// </summary>
    public class GDConsistentReturnRule : GDLintRule
    {
        public override string RuleId => "GDL234";
        public override string Name => "consistent-return";
        public override string Description => "Warn when function has inconsistent return statements";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDMethodDeclaration method)
        {
            if (!Options?.WarnInconsistentReturn ?? false)
            {
                base.Visit(method);
                return;
            }

            if (method == null)
            {
                base.Visit(method);
                return;
            }

            // Skip functions with explicit void return type
            var returnType = method.ReturnType?.BuildName();
            if (returnType == "void")
            {
                base.Visit(method);
                return;
            }

            var returns = method.AllNodes.OfType<GDReturnExpression>().ToList();

            if (returns.Count == 0)
            {
                base.Visit(method);
                return;
            }

            // Check if all returns are consistent
            bool hasValueReturn = false;
            bool hasEmptyReturn = false;
            GDReturnExpression firstValueReturn = null;
            GDReturnExpression firstEmptyReturn = null;

            foreach (var ret in returns)
            {
                if (ret.Expression != null)
                {
                    hasValueReturn = true;
                    if (firstValueReturn == null)
                        firstValueReturn = ret;
                }
                else
                {
                    hasEmptyReturn = true;
                    if (firstEmptyReturn == null)
                        firstEmptyReturn = ret;
                }
            }

            // If we have both value and empty returns, that's inconsistent
            if (hasValueReturn && hasEmptyReturn)
            {
                var methodName = method.Identifier?.Sequence ?? "Function";

                // Report on the empty returns (they're usually the problem)
                foreach (var ret in returns.Where(r => r.Expression == null))
                {
                    ReportIssue(
                        $"'{methodName}' has inconsistent return statements (some return values, some don't)",
                        ret.ReturnKeyword,
                        "Either all return statements should return a value, or none should");
                }
            }

            base.Visit(method);
        }
    }
}
