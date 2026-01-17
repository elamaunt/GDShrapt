using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks that variable names use snake_case.
    /// Based on GDScript style guide: "Use snake_case for variable names."
    /// </summary>
    public class GDVariableNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL003";
        public override string Name => "variable-name-case";
        public override string Description => "Variable names should use snake_case";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            // Skip constants - they have their own rule
            if (variableDeclaration.ConstKeyword != null)
                return;

            var varName = variableDeclaration.Identifier?.Sequence;
            CheckVariableName(varName, variableDeclaration.Identifier);
        }

        public override void Visit(GDVariableDeclarationStatement localVariable)
        {
            var varName = localVariable.Identifier?.Sequence;
            CheckVariableName(varName, localVariable.Identifier);
        }

        private void CheckVariableName(string varName, GDIdentifier identifier)
        {
            if (string.IsNullOrEmpty(varName) || identifier == null)
                return;

            var expectedCase = Options?.VariableNameCase ?? NamingCase.SnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(varName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(varName, expectedCase);
                var fixes = CreateRenameFixes(identifier, suggestion);

                ReportIssue(
                    $"Variable name '{varName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    identifier,
                    $"Rename to '{suggestion}'",
                    fixes);
            }
        }

        private IEnumerable<GDFixDescriptor> CreateRenameFixes(GDIdentifier identifier, string suggestion)
        {
            if (identifier == null || string.IsNullOrEmpty(suggestion))
                yield break;

            yield return GDTextEditFixDescriptor.Replace(
                $"Rename to '{suggestion}'",
                identifier.StartLine,
                identifier.StartColumn,
                identifier.EndColumn,
                suggestion);
        }
    }
}
