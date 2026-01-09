using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a function has too many local variables.
    /// Too many local variables indicate a function that may be doing too much.
    /// </summary>
    public class GDMaxLocalVariablesRule : GDLintRule
    {
        public override string RuleId => "GDL226";
        public override string Name => "max-local-variables";
        public override string Description => "Warn when function has too many local variables";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxLocalVariables = 15;

        public override void Visit(GDMethodDeclaration method)
        {
            var maxVars = Options?.MaxLocalVariables ?? DefaultMaxLocalVariables;
            if (maxVars <= 0)
                return; // Disabled

            if (method == null)
                return;

            var localVarCount = method.AllNodes.OfType<GDVariableDeclarationStatement>().Count();

            if (localVarCount > maxVars)
            {
                var methodName = method.Identifier?.Sequence ?? "Function";
                ReportIssue(
                    $"'{methodName}' has {localVarCount} local variables (max {maxVars})",
                    method.Identifier ?? (GDSyntaxToken)method.FuncKeyword,
                    "Consider breaking this function into smaller functions or using a data structure");
            }

            base.Visit(method);
        }
    }
}
