using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when an if statement is the only statement inside an else block.
    /// This can usually be rewritten as elif for better readability.
    /// </summary>
    public class GDNoLonelyIfRule : GDLintRule
    {
        public override string RuleId => "GDL233";
        public override string Name => "no-lonely-if";
        public override string Description => "Warn when if is the only statement in else block";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false;

        public override void Visit(GDElseBranch elseBranch)
        {
            if (!Options?.WarnNoLonelyIf ?? false)
            {
                base.Visit(elseBranch);
                return;
            }

            if (elseBranch?.Statements == null)
            {
                base.Visit(elseBranch);
                return;
            }

            // Get all non-trivial statements
            var statements = elseBranch.Statements
                .Where(s => s != null)
                .ToList();

            // If there's exactly one statement and it's an if statement
            if (statements.Count == 1 && statements[0] is GDIfStatement lonelyIf)
            {
                ReportIssue(
                    "Lonely if statement in else block",
                    lonelyIf.IfBranch?.IfKeyword,
                    "Consider using 'elif' instead of 'else: if'");
            }

            base.Visit(elseBranch);
        }
    }
}
