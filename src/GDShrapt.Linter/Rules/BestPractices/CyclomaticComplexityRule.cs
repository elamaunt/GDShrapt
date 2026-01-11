using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Measures the cyclomatic complexity of functions and warns when it exceeds a threshold.
    /// High complexity indicates code that is hard to understand, test, and maintain.
    /// </summary>
    public class GDCyclomaticComplexityRule : GDLintRule
    {
        public override string RuleId => "GDL208";
        public override string Name => "cyclomatic-complexity";
        public override string Description => "Warn when function complexity is too high";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxComplexity = 10;

        public override void Visit(GDMethodDeclaration method)
        {
            var maxComplexity = Options?.MaxCyclomaticComplexity ?? DefaultMaxComplexity;
            if (maxComplexity <= 0)
                return; // Disabled

            if (method == null)
                return;

            // Start with complexity of 1 (the function itself)
            int complexity = 1;

            // Count decision points
            foreach (var node in method.AllNodes)
            {
                // Control flow statements
                if (node is GDIfStatement)
                {
                    complexity++; // The if itself
                }
                else if (node is GDElifBranch)
                {
                    complexity++; // Each elif is a decision point
                }
                else if (node is GDWhileStatement)
                {
                    complexity++;
                }
                else if (node is GDForStatement)
                {
                    complexity++;
                }
                else if (node is GDMatchCaseDeclaration)
                {
                    complexity++; // Each case in match
                }
                // Ternary operator (conditional expression)
                else if (node is GDIfExpression)
                {
                    complexity++;
                }
                // Logical operators in conditions
                else if (node is GDDualOperatorExpression dualOp)
                {
                    var opType = dualOp.OperatorType;
                    if (opType == GDDualOperatorType.And ||
                        opType == GDDualOperatorType.And2 ||
                        opType == GDDualOperatorType.Or ||
                        opType == GDDualOperatorType.Or2)
                    {
                        complexity++;
                    }
                }
            }

            if (complexity > maxComplexity)
            {
                var methodName = method.Identifier?.Sequence ?? "unknown";
                ReportIssue(
                    $"Function '{methodName}' has cyclomatic complexity of {complexity}, which exceeds the maximum of {maxComplexity}",
                    (GDSyntaxToken)method.Identifier ?? method.FuncKeyword,
                    "Consider breaking this function into smaller, more focused functions");
            }
        }
    }
}
