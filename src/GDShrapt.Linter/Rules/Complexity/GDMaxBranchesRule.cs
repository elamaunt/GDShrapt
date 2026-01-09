using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a function has too many branches (if/elif/else/match cases).
    /// Too many branches indicate complex decision logic that may need refactoring.
    /// </summary>
    public class GDMaxBranchesRule : GDLintRule
    {
        public override string RuleId => "GDL228";
        public override string Name => "max-branches";
        public override string Description => "Warn when function has too many branches";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxBranches = 12;

        public override void Visit(GDMethodDeclaration method)
        {
            var maxBranches = Options?.MaxBranches ?? DefaultMaxBranches;
            if (maxBranches <= 0)
                return; // Disabled

            if (method == null)
                return;

            int branchCount = 0;

            foreach (var node in method.AllNodes)
            {
                // Count if statements
                if (node is GDIfStatement)
                    branchCount++;
                // Count elif branches
                else if (node is GDElifBranch)
                    branchCount++;
                // Count else branches
                else if (node is GDElseBranch)
                    branchCount++;
                // Count match cases
                else if (node is GDMatchCaseDeclaration)
                    branchCount++;
            }

            if (branchCount > maxBranches)
            {
                var methodName = method.Identifier?.Sequence ?? "Function";
                ReportIssue(
                    $"'{methodName}' has {branchCount} branches (max {maxBranches})",
                    method.Identifier ?? (GDSyntaxToken)method.FuncKeyword,
                    "Consider using a lookup table, state pattern, or extracting logic into separate functions");
            }

            base.Visit(method);
        }
    }
}
