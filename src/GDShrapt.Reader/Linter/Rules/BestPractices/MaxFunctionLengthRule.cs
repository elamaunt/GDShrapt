using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that functions don't have too many lines.
    /// Long functions are harder to understand, test, and maintain.
    /// </summary>
    public class GDMaxFunctionLengthRule : GDLintRule
    {
        public override string RuleId => "GDL206";
        public override string Name => "max-function-length";
        public override string Description => "Warn when functions are too long";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        /// <summary>
        /// Default maximum number of lines (50).
        /// </summary>
        public const int DefaultMaxLines = 50;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            var maxLines = Options?.MaxFunctionLength ?? DefaultMaxLines;
            if (maxLines <= 0)
                return; // Disabled

            var statements = methodDeclaration.Statements;
            if (statements == null)
                return;

            // Count all statements recursively
            var lineCount = CountStatements(statements);

            if (lineCount > maxLines)
            {
                var methodName = methodDeclaration.Identifier?.Sequence ?? "unknown";
                ReportIssue(
                    $"Function '{methodName}' has approximately {lineCount} statements, which exceeds the maximum of {maxLines}",
                    methodDeclaration.Identifier,
                    "Consider breaking this function into smaller, more focused functions");
            }
        }

        private int CountStatements(GDStatementsList statements)
        {
            if (statements == null)
                return 0;

            int count = 0;
            foreach (var stmt in statements)
            {
                count++;

                // Count nested statements in control flow
                if (stmt is GDIfStatement ifStmt)
                {
                    count += CountIfStatement(ifStmt);
                }
                else if (stmt is GDForStatement forStmt)
                {
                    count += CountStatements(forStmt.Statements);
                }
                else if (stmt is GDWhileStatement whileStmt)
                {
                    count += CountStatements(whileStmt.Statements);
                }
                else if (stmt is GDMatchStatement matchStmt)
                {
                    foreach (var matchCase in matchStmt.Cases)
                    {
                        count++; // The case itself
                        count += CountStatements(matchCase.Statements);
                    }
                }
            }

            return count;
        }

        private int CountIfStatement(GDIfStatement ifStmt)
        {
            int count = 0;

            // Count statements in if branch
            if (ifStmt.IfBranch?.Statements != null)
            {
                count += CountStatements(ifStmt.IfBranch.Statements);
            }

            // Count elif branches
            if (ifStmt.ElifBranchesList != null)
            {
                foreach (var elif in ifStmt.ElifBranchesList)
                {
                    count++; // The elif itself
                    if (elif.Statements != null)
                    {
                        count += CountStatements(elif.Statements);
                    }
                }
            }

            // Count else branch
            if (ifStmt.ElseBranch?.Statements != null)
            {
                count++; // The else itself
                count += CountStatements(ifStmt.ElseBranch.Statements);
            }

            return count;
        }
    }
}
