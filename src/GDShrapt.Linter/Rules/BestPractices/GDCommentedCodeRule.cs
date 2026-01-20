using System.Text.RegularExpressions;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL237: Detects commented-out code that should be removed.
    /// Uses heuristics to identify code patterns in comments.
    /// </summary>
    public class GDCommentedCodeRule : GDLintRule
    {
        public override string RuleId => "GDL237";
        public override string Name => "commented-code";
        public override string Description => "Commented-out code should be removed";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false;

        // Patterns that indicate commented-out code
        private static readonly Regex KeywordPattern = new Regex(
            @"^\s*(func\s|var\s|const\s|signal\s|class\s|extends\s|class_name\s|enum\s|static\s|@onready|@export)",
            RegexOptions.Compiled);

        private static readonly Regex ControlFlowPattern = new Regex(
            @"^\s*(if\s|elif\s|else:|for\s|while\s|match\s|return\s|break|continue|pass)",
            RegexOptions.Compiled);

        private static readonly Regex AssignmentPattern = new Regex(
            @"^\s*\w+\s*(=|\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<=|>>=)\s*",
            RegexOptions.Compiled);

        private static readonly Regex FunctionCallPattern = new Regex(
            @"^\s*\w+\s*\([^)]*\)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex MethodCallPattern = new Regex(
            @"^\s*\w+\.\w+\s*\(",
            RegexOptions.Compiled);

        // Skip doc comments (## comments)
        private static readonly Regex DocCommentPattern = new Regex(
            @"^##",
            RegexOptions.Compiled);

        // Skip suppression comments
        private static readonly Regex SuppressionPattern = new Regex(
            @"^\s*#\s*gdlint:",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void Visit(GDClassDeclaration classDecl)
        {
            if (!Options?.WarnCommentedCode ?? true)
            {
                base.Visit(classDecl);
                return;
            }

            CheckComments(classDecl);
            base.Visit(classDecl);
        }

        private void CheckComments(GDNode node)
        {
            foreach (var token in node.AllTokens)
            {
                if (token is GDComment comment)
                {
                    CheckComment(comment);
                }
            }
        }

        private void CheckComment(GDComment comment)
        {
            var text = comment.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Skip doc comments
            if (DocCommentPattern.IsMatch(text))
                return;

            // Skip suppression comments
            if (SuppressionPattern.IsMatch(text))
                return;

            // Remove the # prefix
            var content = text.TrimStart('#').Trim();
            if (string.IsNullOrWhiteSpace(content))
                return;

            // Check for code patterns
            if (LooksLikeCode(content))
            {
                ReportIssue(
                    "Commented-out code should be removed",
                    comment,
                    "Remove commented code or convert to a proper TODO comment");
            }
        }

        private bool LooksLikeCode(string content)
        {
            // Check for GDScript keywords
            if (KeywordPattern.IsMatch(content))
                return true;

            // Check for control flow statements
            if (ControlFlowPattern.IsMatch(content))
                return true;

            // Check for assignments
            if (AssignmentPattern.IsMatch(content))
                return true;

            // Check for standalone function calls
            if (FunctionCallPattern.IsMatch(content))
                return true;

            // Check for method calls
            if (MethodCallPattern.IsMatch(content))
                return true;

            return false;
        }
    }
}
