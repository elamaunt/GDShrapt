using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Checks for consistent trailing comma usage in multiline arrays and dictionaries.
    /// Trailing commas make diffs cleaner when adding new elements.
    /// </summary>
    public class GDTrailingCommaRule : GDLintRule
    {
        public override string RuleId => "GDL302";
        public override string Name => "trailing-comma";
        public override string Description => "Enforce trailing comma in multiline collections";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false;

        public override void Visit(GDArrayInitializerExpression array)
        {
            if (Options?.RequireTrailingComma != true)
                return;

            if (array?.Values == null)
                return;

            // Only check multiline arrays
            if (array.NewLinesCount == 0)
                return;

            CheckTrailingComma(array.Values, array.SquareCloseBracket);
        }

        public override void Visit(GDDictionaryInitializerExpression dict)
        {
            if (Options?.RequireTrailingComma != true)
                return;

            if (dict?.KeyValues == null)
                return;

            // Only check multiline dictionaries
            if (dict.NewLinesCount == 0)
                return;

            CheckDictionaryTrailingComma(dict);
        }

        private void CheckTrailingComma(GDExpressionsList values, GDSquareCloseBracket closeBracket)
        {
            var lastExpr = values.LastOrDefault();
            if (lastExpr == null)
                return;

            // Check if there's a comma after the last element
            var tokenAfterLast = lastExpr.NextToken;

            // Skip spaces and newlines to find if there's a comma
            while (tokenAfterLast != null &&
                   (tokenAfterLast is GDSpace || tokenAfterLast is GDNewLine || tokenAfterLast is GDIntendation))
            {
                tokenAfterLast = tokenAfterLast.NextToken;
            }

            if (!(tokenAfterLast is GDComma))
            {
                var reportToken = lastExpr.AllTokens.LastOrDefault() ?? closeBracket;
                ReportIssue(
                    "Missing trailing comma in multiline array",
                    reportToken,
                    "Add a trailing comma after the last element for cleaner diffs");
            }
        }

        private void CheckDictionaryTrailingComma(GDDictionaryInitializerExpression dict)
        {
            var lastKvp = dict.KeyValues?.LastOrDefault();
            if (lastKvp == null)
                return;

            // Check if there's a comma after the last key-value pair
            var tokenAfterLast = lastKvp.NextToken;

            // Skip spaces and newlines
            while (tokenAfterLast != null &&
                   (tokenAfterLast is GDSpace || tokenAfterLast is GDNewLine || tokenAfterLast is GDIntendation))
            {
                tokenAfterLast = tokenAfterLast.NextToken;
            }

            if (!(tokenAfterLast is GDComma))
            {
                var reportToken = lastKvp.AllTokens.LastOrDefault() ?? dict.FigureCloseBracket;
                ReportIssue(
                    "Missing trailing comma in multiline dictionary",
                    reportToken,
                    "Add a trailing comma after the last entry for cleaner diffs");
            }
        }
    }
}
