using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when a class has too many member variables.
    /// Too many variables indicate a class that may have too many responsibilities.
    /// </summary>
    public class GDMaxClassVariablesRule : GDLintRule
    {
        public override string RuleId => "GDL227";
        public override string Name => "max-class-variables";
        public override string Description => "Warn when class has too many member variables";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxClassVariables = 20;

        public override void Visit(GDClassDeclaration classDecl)
        {
            CheckClassVariables(classDecl.Variables, classDecl.ClassName?.Identifier);
            base.Visit(classDecl);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            CheckClassVariables(innerClass.Variables, innerClass.Identifier);
            base.Visit(innerClass);
        }

        private void CheckClassVariables(System.Collections.Generic.IEnumerable<GDVariableDeclaration> variables, GDIdentifier classIdentifier)
        {
            var maxVars = Options?.MaxClassVariables ?? DefaultMaxClassVariables;
            if (maxVars <= 0)
                return; // Disabled

            if (variables == null)
                return;

            // Exclude constants (they don't count as variables)
            var varCount = variables.Count(v => v.ConstKeyword == null);

            if (varCount > maxVars)
            {
                var className = classIdentifier?.Sequence ?? "Class";
                ReportIssue(
                    $"'{className}' has {varCount} member variables (max {maxVars})",
                    classIdentifier,
                    "Consider splitting this class or grouping related variables into a separate resource");
            }
        }
    }
}
