using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL235: Warns when a function with explicit return type does not return
    /// a value in all code paths.
    /// Similar to C# compiler error CS0161 "not all code paths return a value".
    /// </summary>
    public class GDMissingReturnRule : GDLintRule
    {
        public override string RuleId => "GDL235";
        public override string Name => "missing-return";
        public override string Description => "Function with explicit return type must return a value in all code paths";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDMethodDeclaration method)
        {
            if (!Options?.WarnMissingReturn ?? false)
            {
                base.Visit(method);
                return;
            }

            if (method == null)
            {
                base.Visit(method);
                return;
            }

            // Skip functions without explicit return type - they implicitly return null
            var returnType = method.ReturnType?.BuildName();
            if (string.IsNullOrEmpty(returnType) || returnType == "void")
            {
                base.Visit(method);
                return;
            }

            // Check if function has any statements
            if (method.Statements == null || !method.Statements.Any())
            {
                // Empty function with return type - report
                ReportMissingReturn(method);
                base.Visit(method);
                return;
            }

            // Analyze if all code paths return a value
            if (!AllPathsReturn(method.Statements))
            {
                ReportMissingReturn(method);
            }

            base.Visit(method);
        }

        private void ReportMissingReturn(GDMethodDeclaration method)
        {
            var methodName = method.Identifier?.Sequence ?? "Function";
            var returnType = method.ReturnType?.BuildName() ?? "unknown";

            // Find the function keyword or identifier to report on
            GDSyntaxToken reportToken = method.FuncKeyword;
            if (reportToken == null)
                reportToken = method.Identifier;
            if (reportToken == null)
                return;

            ReportIssue(
                $"'{methodName}' declares return type '{returnType}' but not all code paths return a value",
                reportToken,
                "Ensure all code paths return a value or change return type to void");
        }

        /// <summary>
        /// Analyzes if all code paths in a statement list return a value.
        /// </summary>
        private bool AllPathsReturn(IEnumerable<GDNode> statements)
        {
            if (statements == null)
                return false;

            var stmtList = statements.ToList();
            if (stmtList.Count == 0)
                return false;

            // Check the last statement
            var lastStmt = stmtList.Last();

            // If last statement is a return with expression, we're good
            if (lastStmt is GDExpressionStatement exprStmt)
            {
                if (exprStmt.Expression is GDReturnExpression returnExpr)
                {
                    // Return with expression means path returns a value
                    return returnExpr.Expression != null;
                }
            }

            // If last statement is an if with all branches returning
            if (lastStmt is GDIfStatement ifStmt)
            {
                return IfStatementAllPathsReturn(ifStmt);
            }

            // If last statement is a match statement with all cases returning
            if (lastStmt is GDMatchStatement matchStmt)
            {
                return MatchStatementAllPathsReturn(matchStmt);
            }

            // Check if any statement in the sequence unconditionally returns
            // (statements after return are unreachable but we don't report that here)
            foreach (var stmt in stmtList)
            {
                if (stmt is GDExpressionStatement es && es.Expression is GDReturnExpression ret && ret.Expression != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if an if statement has all paths returning.
        /// For this to be true, all branches must exist and return.
        /// </summary>
        private bool IfStatementAllPathsReturn(GDIfStatement ifStmt)
        {
            // Must have an else branch for all paths to return
            if (ifStmt.ElseBranch == null)
                return false;

            // Check if branch returns
            if (!IfBranchReturns(ifStmt.IfBranch))
                return false;

            // Check all elif branches
            if (ifStmt.ElifBranchesList != null)
            {
                foreach (var elif in ifStmt.ElifBranchesList.OfType<GDElifBranch>())
                {
                    if (!ElifBranchReturns(elif))
                        return false;
                }
            }

            // Check else branch
            if (!ElseBranchReturns(ifStmt.ElseBranch))
                return false;

            return true;
        }

        private bool IfBranchReturns(GDIfBranch branch)
        {
            if (branch == null)
                return false;

            // Check single-line expression
            if (branch.Expression is GDReturnExpression retExpr)
                return retExpr.Expression != null;

            // Check statements
            return AllPathsReturn(branch.Statements);
        }

        private bool ElifBranchReturns(GDElifBranch branch)
        {
            if (branch == null)
                return false;

            // Check single-line expression
            if (branch.Expression is GDReturnExpression retExpr)
                return retExpr.Expression != null;

            // Check statements
            return AllPathsReturn(branch.Statements);
        }

        private bool ElseBranchReturns(GDElseBranch branch)
        {
            if (branch == null)
                return false;

            // Check single-line expression
            if (branch.Expression is GDReturnExpression retExpr)
                return retExpr.Expression != null;

            // Check statements
            return AllPathsReturn(branch.Statements);
        }

        /// <summary>
        /// Checks if a match statement has all paths returning.
        /// For this to be true, there must be a default/wildcard case and all cases must return.
        /// </summary>
        private bool MatchStatementAllPathsReturn(GDMatchStatement matchStmt)
        {
            if (matchStmt.Cases == null || !matchStmt.Cases.Any())
                return false;

            bool hasDefaultCase = false;

            foreach (var matchCase in matchStmt.Cases.OfType<GDMatchCaseDeclaration>())
            {
                // Check if this is a default case (using _ pattern)
                if (IsDefaultMatchCase(matchCase))
                    hasDefaultCase = true;

                // Check if this case returns
                if (!MatchCaseReturns(matchCase))
                    return false;
            }

            // All cases must exist AND have a default case for exhaustive matching
            return hasDefaultCase;
        }

        private bool IsDefaultMatchCase(GDMatchCaseDeclaration matchCase)
        {
            // Check for wildcard pattern "_" in conditions
            if (matchCase.Conditions == null)
                return false;

            foreach (var condition in matchCase.Conditions)
            {
                // Check if this is an identifier "_" (wildcard pattern)
                if (condition is GDIdentifierExpression identExpr && identExpr.Identifier?.Sequence == "_")
                    return true;
            }

            return false;
        }

        private bool MatchCaseReturns(GDMatchCaseDeclaration matchCase)
        {
            if (matchCase == null)
                return false;

            // Check statements
            return AllPathsReturn(matchCase.Statements);
        }
    }
}
