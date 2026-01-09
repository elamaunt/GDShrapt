using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a function has too many return statements.
    /// Multiple returns can make code harder to follow and debug.
    /// </summary>
    public class GDMaxReturnsRule : GDLintRule
    {
        public override string RuleId => "GDL223";
        public override string Name => "max-returns";
        public override string Description => "Warn when function has too many return statements";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxReturns = 6;

        public override void Visit(GDMethodDeclaration method)
        {
            var maxReturns = Options?.MaxReturns ?? DefaultMaxReturns;
            if (maxReturns <= 0)
                return; // Disabled

            if (method == null)
                return;

            var returnCount = method.AllNodes.OfType<GDReturnExpression>().Count();

            if (returnCount > maxReturns)
            {
                var methodName = method.Identifier?.Sequence ?? "Function";
                ReportIssue(
                    $"'{methodName}' has {returnCount} return statements (max {maxReturns})",
                    (GDSyntaxToken)method.Identifier ?? method.FuncKeyword,
                    "Consider restructuring to reduce the number of exit points");
            }

            base.Visit(method);
        }
    }
}
