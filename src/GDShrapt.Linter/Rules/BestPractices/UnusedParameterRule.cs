using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks for unused function parameters.
    /// Uses token iteration to find all identifier usages.
    /// </summary>
    public class GDUnusedParameterRule : GDLintRule
    {
        public override string RuleId => "GDL202";
        public override string Name => "unused-parameter";
        public override string Description => "Warn about unused function parameters";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.WarnUnusedParameters != true)
                return;

            // Skip virtual methods - parameters may be required by interface
            var methodName = methodDeclaration.Identifier?.Sequence;
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(methodName))
                return;

            var parameters = methodDeclaration.Parameters;
            if (parameters == null || !parameters.Any())
                return;

            // Collect all used identifiers in the method body
            var usedIdentifiers = new HashSet<string>();
            CollectUsedIdentifiers(methodDeclaration, usedIdentifiers);

            // Check each parameter
            foreach (var param in parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                // Skip parameters starting with underscore (intentionally unused)
                if (paramName.StartsWith("_"))
                    continue;

                if (!usedIdentifiers.Contains(paramName))
                {
                    ReportIssue(
                        $"Parameter '{paramName}' is declared but never used",
                        param.Identifier,
                        "Remove the parameter or prefix with underscore if intentionally unused");
                }
            }
        }

        private void CollectUsedIdentifiers(GDMethodDeclaration method, HashSet<string> result)
        {
            // Collect all identifier usages in the method body
            foreach (var node in method.AllNodes)
            {
                if (node is GDIdentifierExpression identExpr)
                {
                    var name = identExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Add(name);
                    }
                }
            }
        }
    }
}
