namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that inner class names use PascalCase.
    /// Based on GDScript style guide: "Use PascalCase for class names."
    /// This is a separate rule from GDL001 to allow different naming conventions
    /// for inner classes (sub-classes) vs top-level class names.
    /// </summary>
    public class GDInnerClassNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL009";
        public override string Name => "inner-class-name-case";
        public override string Description => "Inner class names should use PascalCase";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            var className = innerClass.Identifier?.Sequence;
            if (string.IsNullOrEmpty(className))
            {
                base.Visit(innerClass);
                return;
            }

            // Allow private inner classes with underscore prefix (_PrivateName)
            var nameToCheck = className;
            if (className.StartsWith("_") && className.Length > 1)
            {
                nameToCheck = className.Substring(1);
            }

            var expectedCase = Options?.InnerClassNameCase ?? NamingCase.PascalCase;
            if (expectedCase == NamingCase.Any)
            {
                base.Visit(innerClass);
                return;
            }

            if (!NamingHelper.MatchesCase(nameToCheck, expectedCase))
            {
                var suggestion = className.StartsWith("_")
                    ? "_" + NamingHelper.SuggestCorrectName(nameToCheck, expectedCase)
                    : NamingHelper.SuggestCorrectName(className, expectedCase);

                ReportIssue(
                    $"Inner class name '{className}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    innerClass.Identifier,
                    $"Rename to '{suggestion}'");
            }

            base.Visit(innerClass);
        }
    }
}
