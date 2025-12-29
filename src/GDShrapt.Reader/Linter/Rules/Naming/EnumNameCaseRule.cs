namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that enum names use PascalCase.
    /// Based on GDScript style guide: "Use PascalCase for enum names."
    /// </summary>
    public class GDEnumNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL006";
        public override string Name => "enum-name-case";
        public override string Description => "Enum names should use PascalCase";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            var enumName = enumDeclaration.Identifier?.Sequence;

            // Anonymous enums don't have names
            if (string.IsNullOrEmpty(enumName))
                return;

            var expectedCase = Options?.EnumNameCase ?? NamingCase.PascalCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(enumName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(enumName, expectedCase);
                ReportIssue(
                    $"Enum name '{enumName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    enumDeclaration.Identifier,
                    $"Rename to '{suggestion}'");
            }
        }
    }
}
