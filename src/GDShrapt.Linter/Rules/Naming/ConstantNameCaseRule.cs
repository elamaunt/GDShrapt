using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks that constant names use SCREAMING_SNAKE_CASE.
    /// Based on GDScript style guide: "Use CONSTANT_CASE, all caps with underscores, for constants."
    /// </summary>
    public class GDConstantNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL004";
        public override string Name => "constant-name-case";
        public override string Description => "Constant names should use SCREAMING_SNAKE_CASE";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            // Only check constants
            if (variableDeclaration.ConstKeyword == null)
                return;

            var constName = variableDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(constName))
                return;

            var expectedCase = Options?.ConstantNameCase ?? NamingCase.ScreamingSnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(constName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(constName, expectedCase);
                var identifier = variableDeclaration.Identifier;
                var fixes = CreateRenameFixes(identifier, suggestion);

                ReportIssue(
                    $"Constant name '{constName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    variableDeclaration.Identifier,
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
