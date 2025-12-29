namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that private members are prefixed with underscore.
    /// Based on GDScript style guide: "Prefix private functions and variables with underscore (_)."
    /// </summary>
    public class GDPrivatePrefixRule : GDLintRule
    {
        public override string RuleId => "GDL008";
        public override string Name => "private-prefix";
        public override string Description => "Private members should be prefixed with underscore";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false; // Disabled by default as it's a suggestion

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.RequireUnderscoreForPrivate != true)
                return;

            var funcName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(funcName))
                return;

            // Skip virtual methods and public methods
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(funcName))
                return;

            // Check if this appears to be a private method that should have underscore
            // This is a heuristic - methods not exposed via export or signal are likely private
            // For now, this rule is informational only
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            if (Options?.RequireUnderscoreForPrivate != true)
                return;

            // Skip constants - they follow different convention
            if (variableDeclaration.ConstKeyword != null)
                return;

            var varName = variableDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // If the variable doesn't start with underscore,
            // it might be a private variable that should have underscore prefix
            // Note: We can't easily detect @export/@onready here, so this is just a hint
            if (!varName.StartsWith("_"))
            {
                // This is just an info hint, not a warning
                ReportIssue(
                    GDLintSeverity.Hint,
                    $"Consider prefixing private variable '{varName}' with underscore",
                    variableDeclaration.Identifier,
                    $"Rename to '_{varName}'");
            }
        }
    }
}
