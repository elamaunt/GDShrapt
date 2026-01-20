using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDL236: Detects god classes with too many responsibilities.
    /// Combines multiple metrics: variables, methods, lines, dependencies.
    /// A god class typically has too many variables, too many methods, or too many lines.
    /// </summary>
    public class GDGodClassRule : GDLintRule
    {
        public override string RuleId => "GDL236";
        public override string Name => "god-class";
        public override string Description => "Class has too many responsibilities (god class)";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        // Default thresholds
        public const int DefaultMaxVariables = 15;
        public const int DefaultMaxMethods = 20;
        public const int DefaultMaxLines = 500;
        public const int DefaultMaxPublicMethods = 15;
        public const int DefaultCombinedVariablesThreshold = 10;

        public override void Visit(GDClassDeclaration classDecl)
        {
            CheckGodClass(classDecl, classDecl.ClassName?.Identifier);
            base.Visit(classDecl);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            CheckGodClass(innerClass, innerClass.Identifier);
            base.Visit(innerClass);
        }

        private void CheckGodClass(GDClassDeclaration classDecl, GDIdentifier classIdentifier)
        {
            if (!Options?.WarnGodClass ?? true)
                return;

            var maxVars = Options?.GodClassMaxVariables ?? DefaultMaxVariables;
            var maxMethods = Options?.GodClassMaxMethods ?? DefaultMaxMethods;
            var maxLines = Options?.GodClassMaxLines ?? DefaultMaxLines;
            var maxPublicMethods = DefaultMaxPublicMethods;
            var combinedVarsThreshold = DefaultCombinedVariablesThreshold;

            // Count metrics
            int variableCount = 0;
            int methodCount = 0;
            int publicMethodCount = 0;

            if (classDecl.Variables != null)
            {
                // Exclude constants
                variableCount = classDecl.Variables.Count(v => v.ConstKeyword == null);
            }

            if (classDecl.Methods != null)
            {
                methodCount = classDecl.Methods.Count();
                // Public methods don't start with _
                publicMethodCount = classDecl.Methods.Count(m =>
                    m.Identifier?.Sequence != null &&
                    !m.Identifier.Sequence.StartsWith("_"));
            }

            // Estimate line count from node positions
            int lineCount = EstimateLineCount(classDecl);

            // Check god class criteria
            string violation = null;

            if (variableCount > maxVars)
            {
                violation = $"too many member variables ({variableCount} > {maxVars})";
            }
            else if (methodCount > maxMethods)
            {
                violation = $"too many methods ({methodCount} > {maxMethods})";
            }
            else if (lineCount > maxLines)
            {
                violation = $"too many lines ({lineCount} > {maxLines})";
            }
            else if (publicMethodCount > maxPublicMethods && variableCount > combinedVarsThreshold)
            {
                violation = $"too many public methods ({publicMethodCount}) combined with many variables ({variableCount})";
            }

            if (violation != null)
            {
                var className = classIdentifier?.Sequence ?? "Class";
                ReportIssue(
                    $"'{className}' is a god class: {violation}",
                    classIdentifier,
                    "Consider splitting this class into smaller, more focused classes");
            }
        }

        private void CheckGodClass(GDInnerClassDeclaration innerClass, GDIdentifier classIdentifier)
        {
            if (!Options?.WarnGodClass ?? true)
                return;

            var maxVars = Options?.GodClassMaxVariables ?? DefaultMaxVariables;
            var maxMethods = Options?.GodClassMaxMethods ?? DefaultMaxMethods;
            var maxLines = Options?.GodClassMaxLines ?? DefaultMaxLines;
            var maxPublicMethods = DefaultMaxPublicMethods;
            var combinedVarsThreshold = DefaultCombinedVariablesThreshold;

            // Count metrics
            int variableCount = 0;
            int methodCount = 0;
            int publicMethodCount = 0;

            if (innerClass.Variables != null)
            {
                variableCount = innerClass.Variables.Count(v => v.ConstKeyword == null);
            }

            if (innerClass.Methods != null)
            {
                methodCount = innerClass.Methods.Count();
                publicMethodCount = innerClass.Methods.Count(m =>
                    m.Identifier?.Sequence != null &&
                    !m.Identifier.Sequence.StartsWith("_"));
            }

            int lineCount = EstimateLineCount(innerClass);

            // Check god class criteria
            string violation = null;

            if (variableCount > maxVars)
            {
                violation = $"too many member variables ({variableCount} > {maxVars})";
            }
            else if (methodCount > maxMethods)
            {
                violation = $"too many methods ({methodCount} > {maxMethods})";
            }
            else if (lineCount > maxLines)
            {
                violation = $"too many lines ({lineCount} > {maxLines})";
            }
            else if (publicMethodCount > maxPublicMethods && variableCount > combinedVarsThreshold)
            {
                violation = $"too many public methods ({publicMethodCount}) combined with many variables ({variableCount})";
            }

            if (violation != null)
            {
                var className = classIdentifier?.Sequence ?? "InnerClass";
                ReportIssue(
                    $"'{className}' is a god class: {violation}",
                    classIdentifier,
                    "Consider splitting this class into smaller, more focused classes");
            }
        }

        private int EstimateLineCount(GDNode node)
        {
            // Get all tokens and find the line range
            var allTokens = node.AllTokens.ToList();
            if (allTokens.Count == 0)
                return 0;

            int minLine = int.MaxValue;
            int maxLine = 0;

            foreach (var token in allTokens)
            {
                if (token.StartLine > 0 && token.StartLine < minLine)
                    minLine = token.StartLine;
                if (token.EndLine > maxLine)
                    maxLine = token.EndLine;
            }

            if (minLine == int.MaxValue)
                return 0;

            return maxLine - minLine + 1;
        }
    }
}
