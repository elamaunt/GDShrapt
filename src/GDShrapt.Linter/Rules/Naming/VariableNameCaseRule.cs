namespace GDShrapt.Reader
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

        private void CheckVariableName(string varName, GDSyntaxToken token)
        {
            if (string.IsNullOrEmpty(varName))
                return;

            var expectedCase = Options?.VariableNameCase ?? NamingCase.SnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(varName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(varName, expectedCase);
                ReportIssue(
                    $"Variable name '{varName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    token,
                    $"Rename to '{suggestion}'");
            }
        }
    }
}
