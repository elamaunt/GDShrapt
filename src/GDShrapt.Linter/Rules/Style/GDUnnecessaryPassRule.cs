using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about unnecessary 'pass' statements in non-empty blocks.
    /// A 'pass' statement is only needed when a block would otherwise be empty.
    /// </summary>
    public class GDUnnecessaryPassRule : GDLintRule
    {
        public override string RuleId => "GDL251";
        public override string Name => "unnecessary-pass";
        public override string Description => "Warn about unnecessary pass statements";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDMethodDeclaration method)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(method.Statements);
        }

        public override void Visit(GDIfBranch branch)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(branch.Statements);
        }

        public override void Visit(GDElseBranch branch)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(branch.Statements);
        }

        public override void Visit(GDElifBranch branch)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(branch.Statements);
        }

        public override void Visit(GDForStatement forStmt)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(forStmt.Statements);
        }

        public override void Visit(GDWhileStatement whileStmt)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(whileStmt.Statements);
        }

        public override void Visit(GDMatchCaseDeclaration caseDecl)
        {
            if (Options?.WarnUnnecessaryPass != true)
                return;

            CheckStatements(caseDecl.Statements);
        }

        private void CheckStatements(IEnumerable<GDStatement> statements)
        {
            if (statements == null)
                return;

            var list = statements.ToList();

            // pass is necessary if it's the only statement
            if (list.Count <= 1)
                return;

            // Find pass statements in non-empty blocks
            // pass is wrapped in GDExpressionStatement containing GDPassExpression
            foreach (var stmt in list)
            {
                if (stmt is GDExpressionStatement exprStmt &&
                    exprStmt.Expression is GDPassExpression passExpr)
                {
                    ReportIssue(
                        "Unnecessary 'pass' statement in non-empty block",
                        passExpr,
                        "Remove the pass statement");
                }
            }
        }
    }
}
