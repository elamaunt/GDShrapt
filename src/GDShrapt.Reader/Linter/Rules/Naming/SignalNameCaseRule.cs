namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that signal names use snake_case.
    /// Based on GDScript style guide: "Use snake_case for signal names."
    /// Signals should use past tense verb (e.g., "door_opened", "game_started").
    /// </summary>
    public class GDSignalNameCaseRule : GDLintRule
    {
        public override string RuleId => "GDL005";
        public override string Name => "signal-name-case";
        public override string Description => "Signal names should use snake_case (preferably past tense)";
        public override GDLintCategory Category => GDLintCategory.Naming;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            var signalName = signalDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(signalName))
                return;

            var expectedCase = Options?.SignalNameCase ?? NamingCase.SnakeCase;
            if (expectedCase == NamingCase.Any)
                return;

            if (!NamingHelper.MatchesCase(signalName, expectedCase))
            {
                var suggestion = NamingHelper.SuggestCorrectName(signalName, expectedCase);
                ReportIssue(
                    $"Signal name '{signalName}' should use {NamingHelper.GetCaseName(expectedCase)}",
                    signalDeclaration.Identifier,
                    $"Rename to '{suggestion}'");
            }
        }
    }
}
