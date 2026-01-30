using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when code has too many levels of nesting (if/for/while/match).
    /// Deep nesting makes code harder to read and understand.
    /// </summary>
    public class GDMaxNestingDepthRule : GDLintRule
    {
        public override string RuleId => "GDL225";
        public override string Name => "max-nesting-depth";
        public override string Description => "Warn when nesting depth is too high";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxNestingDepth = 4;

        public override void Visit(GDMethodDeclaration method)
        {
            var maxDepth = Options?.MaxNestingDepth ?? DefaultMaxNestingDepth;
            if (maxDepth <= 0)
                return; // Disabled

            if (method == null)
                return;

            int maxFoundDepth = 0;
            GDSyntaxToken deepestToken = null;

            // Track parents to calculate depth
            foreach (var node in method.AllNodes)
            {
                if (IsNestingNode(node))
                {
                    int depth = CalculateNestingDepth(node, method);
                    if (depth > maxFoundDepth)
                    {
                        maxFoundDepth = depth;
                        deepestToken = GetNestingToken(node);
                    }
                }
            }

            if (maxFoundDepth > maxDepth)
            {
                var methodName = method.Identifier?.Sequence ?? "Function";
                ReportIssue(
                    $"'{methodName}' has nesting depth of {maxFoundDepth} (max {maxDepth})",
                    // Report on method identifier for consistency with other complexity rules
                    // This allows # gdlint:ignore on the line before func to work correctly
                    (GDSyntaxToken)method.Identifier ?? method.FuncKeyword,
                    "Consider extracting nested logic into separate functions");
            }

            base.Visit(method);
        }

        private bool IsNestingNode(GDNode node)
        {
            return node is GDIfBranch ||
                   node is GDForStatement ||
                   node is GDWhileStatement ||
                   node is GDMatchStatement;
        }

        private GDSyntaxToken GetNestingToken(GDNode node)
        {
            switch (node)
            {
                case GDIfBranch ifBranch:
                    return ifBranch.IfKeyword;
                case GDForStatement forStmt:
                    return forStmt.ForKeyword;
                case GDWhileStatement whileStmt:
                    return whileStmt.WhileKeyword;
                case GDMatchStatement matchStmt:
                    return matchStmt.MatchKeyword;
                default:
                    return null;
            }
        }

        private int CalculateNestingDepth(GDNode node, GDMethodDeclaration method)
        {
            int depth = 0;

            // Walk up through parents, counting nesting nodes
            var current = node.Parent;
            while (current != null && current != method)
            {
                if (IsNestingNode(current))
                {
                    depth++;
                }
                current = current.Parent;
            }

            // Add 1 for the current node itself
            return depth + 1;
        }
    }
}
