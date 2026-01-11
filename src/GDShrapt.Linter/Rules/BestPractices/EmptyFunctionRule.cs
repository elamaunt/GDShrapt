using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks for empty functions (functions with only 'pass' or no statements).
    /// Uses node iteration to analyze function body.
    /// </summary>
    public class GDEmptyFunctionRule : GDLintRule
    {
        public override string RuleId => "GDL203";
        public override string Name => "empty-function";
        public override string Description => "Warn about empty or pass-only functions";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.WarnEmptyFunctions != true)
                return;

            var methodName = methodDeclaration.Identifier?.Sequence;

            // Skip virtual methods that might be intentionally empty
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(methodName))
                return;

            // Check if there's an inline expression (single-line function)
            if (methodDeclaration.Expression != null)
                return;

            var statements = methodDeclaration.Statements;
            if (statements == null)
            {
                ReportIssue(
                    $"Function '{methodName}' is empty",
                    methodDeclaration.Identifier,
                    "Add implementation or remove the function");
                return;
            }

            // Use direct enumeration of statements
            var stmtCount = 0;
            GDStatement firstStmt = null;

            foreach (var stmt in statements)
            {
                if (firstStmt == null)
                    firstStmt = stmt;
                stmtCount++;
                if (stmtCount > 1)
                    break; // More than one statement - not empty
            }

            if (stmtCount == 0)
            {
                ReportIssue(
                    $"Function '{methodName}' is empty",
                    methodDeclaration.Identifier,
                    "Add implementation or remove the function");
                return;
            }

            // Check if the only statement is 'pass'
            if (stmtCount == 1 && firstStmt is GDExpressionStatement exprStmt)
            {
                if (exprStmt.Expression is GDPassExpression)
                {
                    ReportIssue(
                        $"Function '{methodName}' only contains 'pass'",
                        methodDeclaration.Identifier,
                        "Add implementation or remove the function");
                }
            }
        }
    }
}
