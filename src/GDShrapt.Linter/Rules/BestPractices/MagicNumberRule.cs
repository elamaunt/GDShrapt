using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about "magic numbers" - numeric literals that should be named constants.
    /// </summary>
    public class GDMagicNumberRule : GDLintRule
    {
        public override string RuleId => "GDL209";
        public override string Name => "magic-number";
        public override string Description => "Warn about magic numbers that should be named constants";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false;

        // Default allowed magic numbers
        private static readonly HashSet<string> DefaultAllowedNumbers = new HashSet<string>
        {
            "0", "1", "-1", "2",
            "0.0", "1.0", "0.5", "-1.0",
            "0x0", "0x1", "0b0", "0b1"
        };

        public override void Visit(GDNumberExpression numberExpr)
        {
            if (Options?.WarnMagicNumbers != true)
                return;

            if (numberExpr?.Number == null)
                return;

            var numberStr = numberExpr.Number.ToString();
            if (string.IsNullOrEmpty(numberStr))
                return;

            // Check if this number is in the allowed list
            var allowedNumbers = Options.AllowedMagicNumbers ?? DefaultAllowedNumbers;
            if (allowedNumbers.Contains(numberStr))
                return;

            // Check if inside a constant declaration
            if (IsInsideConstant(numberExpr))
                return;

            // Check if this is an array index (common pattern)
            if (IsArrayIndex(numberExpr))
                return;

            // Check if inside enum value
            if (IsInsideEnum(numberExpr))
                return;

            ReportIssue(
                $"Magic number '{numberStr}' should be a named constant",
                numberExpr.Number,
                "Extract this number into a const with a descriptive name");
        }

        private bool IsInsideConstant(GDNumberExpression numberExpr)
        {
            foreach (var parent in numberExpr.Parents)
            {
                if (parent is GDVariableDeclaration varDecl && varDecl.ConstKeyword != null)
                    return true;

                // Stop at statement/declaration boundary
                if (parent is GDStatement || parent is GDMethodDeclaration)
                    break;
            }
            return false;
        }

        private bool IsArrayIndex(GDNumberExpression numberExpr)
        {
            // Check if immediate parent is indexer expression
            var parent = numberExpr.Parent;
            if (parent is GDIndexerExpression)
                return true;

            return false;
        }

        private bool IsInsideEnum(GDNumberExpression numberExpr)
        {
            foreach (var parent in numberExpr.Parents)
            {
                if (parent is GDEnumValueDeclaration || parent is GDEnumDeclaration)
                    return true;

                if (parent is GDMethodDeclaration)
                    break;
            }
            return false;
        }
    }
}
