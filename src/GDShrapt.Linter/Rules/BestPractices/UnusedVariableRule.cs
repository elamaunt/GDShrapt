using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks for unused local variables.
    /// Uses token iteration to find all identifier usages.
    /// </summary>
    public class GDUnusedVariableRule : GDLintRule
    {
        public override string RuleId => "GDL201";
        public override string Name => "unused-variable";
        public override string Description => "Warn about unused local variables";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.WarnUnusedVariables != true)
                return;

            // Collect all local variable declarations
            var localVariables = new List<GDVariableDeclarationStatement>();
            CollectLocalVariables(methodDeclaration, localVariables);

            if (localVariables.Count == 0)
                return;

            // Collect all used identifiers by iterating through all tokens
            var usedIdentifiers = new HashSet<string>();
            CollectUsedIdentifiers(methodDeclaration, usedIdentifiers);

            // Check which declared variables were not used
            foreach (var localVar in localVariables)
            {
                var varName = localVar.Identifier?.Sequence;
                if (string.IsNullOrEmpty(varName))
                    continue;

                // Skip variables starting with underscore (intentionally unused)
                if (varName.StartsWith("_"))
                    continue;

                // Variable is used if its name appears in any GDIdentifierExpression
                // But we need to exclude the declaration itself
                if (!usedIdentifiers.Contains(varName))
                {
                    ReportIssue(
                        $"Local variable '{varName}' is declared but never used",
                        localVar.Identifier,
                        "Remove the variable or prefix with underscore if intentionally unused");
                }
            }
        }

        private void CollectLocalVariables(GDNode node, List<GDVariableDeclarationStatement> result)
        {
            foreach (var childNode in node.AllNodes)
            {
                if (childNode is GDVariableDeclarationStatement varDecl)
                {
                    result.Add(varDecl);
                }
            }
        }

        private void CollectUsedIdentifiers(GDMethodDeclaration method, HashSet<string> result)
        {
            // Get declared variable names to exclude them from usage detection
            var declaredNames = new HashSet<string>();
            foreach (var node in method.AllNodes)
            {
                if (node is GDVariableDeclarationStatement varDecl)
                {
                    var name = varDecl.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                        declaredNames.Add(name);
                }
            }

            // Find all identifier expressions that are not the declaration itself
            foreach (var node in method.AllNodes)
            {
                if (node is GDIdentifierExpression identExpr)
                {
                    var name = identExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name) && declaredNames.Contains(name))
                    {
                        // Check if this identifier is part of a variable declaration
                        // If the parent is a variable declaration and this is its identifier, skip it
                        var parent = identExpr.Parent;
                        if (parent is GDVariableDeclarationStatement varDecl &&
                            varDecl.Identifier?.Sequence == name)
                        {
                            continue; // This is the declaration, not a usage
                        }

                        result.Add(name);
                    }
                }
            }
        }
    }
}
