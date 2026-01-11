using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when a local variable or loop variable shadows a class-level variable.
    /// This can lead to confusion and bugs.
    /// </summary>
    public class GDVariableShadowingRule : GDLintRule
    {
        public override string RuleId => "GDL211";
        public override string Name => "variable-shadowing";
        public override string Description => "Warn when local variables shadow class variables";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        public override void Visit(GDMethodDeclaration method)
        {
            if (Options?.WarnVariableShadowing != true)
                return;

            if (method == null)
                return;

            // Get the owning class
            var classDecl = method.ClassDeclaration;
            if (classDecl == null)
                return;

            // Collect class-level variable names
            var classVarNames = new HashSet<string>();
            foreach (var variable in classDecl.Variables)
            {
                var name = variable.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                    classVarNames.Add(name);
            }

            // Also include method parameters from the class
            foreach (var classMethod in classDecl.Methods)
            {
                // Skip - we don't want to include method names or params in shadowing check
            }

            if (classVarNames.Count == 0)
                return;

            // Check local variable declarations
            foreach (var localVar in method.AllNodes.OfType<GDVariableDeclarationStatement>())
            {
                var name = localVar.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name) && classVarNames.Contains(name))
                {
                    ReportIssue(
                        $"Local variable '{name}' shadows a class-level variable",
                        localVar.Identifier,
                        "Consider renaming the local variable to avoid confusion");
                }
            }

            // Check for-loop iterators
            foreach (var forStmt in method.AllNodes.OfType<GDForStatement>())
            {
                var name = forStmt.Variable?.Sequence;
                if (!string.IsNullOrEmpty(name) && classVarNames.Contains(name))
                {
                    ReportIssue(
                        $"Loop variable '{name}' shadows a class-level variable",
                        forStmt.Variable,
                        "Consider renaming the loop variable to avoid confusion");
                }
            }
        }
    }
}
