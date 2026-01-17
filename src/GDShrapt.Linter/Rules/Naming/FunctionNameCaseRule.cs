using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks that function names use snake_case.
    /// Based on GDScript style guide: "Use snake_case for function names."
    /// </summary>
    public class GDFunctionNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL002";
        public override string Name => "function-name-case";
        public override string Description => "Function names should use snake_case";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            var funcName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(funcName))
                return;

            // Skip virtual methods that start with underscore (they follow Godot convention)
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(funcName))
                return;

            var expectedCase = Options?.FunctionNameCase ?? NamingCase.SnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            // For snake_case, we need to strip the leading underscore for private methods
            var nameToCheck = funcName;
            if (funcName.StartsWith("_") && !GDSpecialMethodHelper.IsKnownVirtualMethod(funcName))
            {
                // Private method with underscore prefix - check the rest
                nameToCheck = funcName.Substring(1);
            }

            if (!string.IsNullOrEmpty(nameToCheck) && !NamingHelper.MatchesCase(nameToCheck, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(funcName, expectedCase);
                var identifier = methodDeclaration.Identifier;
                var fixes = CreateRenameFixes(identifier, suggestion);

                ReportIssue(
                    $"Function name '{funcName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    methodDeclaration.Identifier,
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
