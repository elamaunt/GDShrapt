namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that enum values use SCREAMING_SNAKE_CASE.
    /// Based on GDScript style guide: "Use CONSTANT_CASE for enum members/values."
    /// </summary>
    public class GDEnumValueCaseRule : GDLintRule
    {
        public override string RuleId => "GDL007";
        public override string Name => "enum-value-case";
        public override string Description => "Enum values should use SCREAMING_SNAKE_CASE";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDEnumValueDeclaration enumValue)
        {
            var valueName = enumValue.Identifier?.Sequence;
            if (string.IsNullOrEmpty(valueName))
                return;

            var expectedCase = Options?.EnumValueCase ?? NamingCase.ScreamingSnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(valueName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(valueName, expectedCase);
                ReportIssue(
                    $"Enum value '{valueName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    enumValue.Identifier,
                    $"Rename to '{suggestion}'");
            }
        }
    }
}
