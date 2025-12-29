using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that functions don't have too many parameters.
    /// Functions with many parameters are harder to understand and maintain.
    /// </summary>
    public class GDMaxParametersRule : GDLintRule
    {
        public override string RuleId => "GDL205";
        public override string Name => "max-parameters";
        public override string Description => "Warn when functions have too many parameters";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        /// <summary>
        /// Default maximum number of parameters (5).
        /// </summary>
        public const int DefaultMaxParameters = 5;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            var maxParams = Options?.MaxParameters ?? DefaultMaxParameters;
            if (maxParams <= 0)
                return; // Disabled

            var parameters = methodDeclaration.Parameters;
            if (parameters == null)
                return;

            var paramCount = parameters.Count();
            if (paramCount > maxParams)
            {
                var methodName = methodDeclaration.Identifier?.Sequence ?? "unknown";
                ReportIssue(
                    $"Function '{methodName}' has {paramCount} parameters, which exceeds the maximum of {maxParams}",
                    methodDeclaration.Identifier,
                    "Consider refactoring to use a configuration object or splitting the function");
            }
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            // Also check inner class methods by walking into them
            // The visitor will automatically visit methods inside
        }
    }
}
