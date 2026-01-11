using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks that class names use PascalCase.
    /// Based on GDScript style guide: "Use PascalCase for class names."
    /// </summary>
    public class GDClassNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL001";
        public override string Name => "class-name-case";
        public override string Description => "Class names should use PascalCase";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            var className = classDeclaration.ClassName?.Identifier?.Sequence;
            if (string.IsNullOrEmpty(className))
                return;

            var expectedCase = Options?.ClassNameCase ?? NamingCase.PascalCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(className, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(className, expectedCase);
                ReportIssue(
                    $"Class name '{className}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    classDeclaration.ClassName,
                    $"Rename to '{suggestion}'");
            }
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            var className = innerClass.Identifier?.Sequence;
            if (string.IsNullOrEmpty(className))
                return;

            var expectedCase = Options?.ClassNameCase ?? NamingCase.PascalCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(className, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(className, expectedCase);
                ReportIssue(
                    $"Inner class name '{className}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    innerClass.Identifier,
                    $"Rename to '{suggestion}'");
            }
        }
    }
}
