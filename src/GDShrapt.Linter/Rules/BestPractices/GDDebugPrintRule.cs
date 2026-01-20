using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL238: Detects debug print statements that should be removed.
    /// Catches: print(), print_debug(), print_rich(), print_verbose(), printerr(), printt(), prints()
    /// </summary>
    public class GDDebugPrintRule : GDLintRule
    {
        public override string RuleId => "GDL238";
        public override string Name => "no-debug-print";
        public override string Description => "Debug print statements should be removed";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        private static readonly HashSet<string> DebugPrintFunctions = new HashSet<string>
        {
            "print",
            "print_debug",
            "print_rich",
            "print_verbose",
            "printerr",
            "printt",
            "prints"
        };

        public override void Visit(GDCallExpression callExpr)
        {
            if (!Options?.WarnDebugPrint ?? true)
            {
                base.Visit(callExpr);
                return;
            }

            // Check if this is a direct function call (not a method call)
            if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
            {
                var funcName = identExpr.Identifier?.Sequence;
                if (funcName != null && DebugPrintFunctions.Contains(funcName))
                {
                    ReportIssue(
                        $"Debug print statement '{funcName}()' should be removed",
                        identExpr.Identifier,
                        "Remove debug print statements before committing or use push_warning()/push_error() for logging");
                }
            }

            base.Visit(callExpr);
        }
    }
}
