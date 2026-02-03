using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when a variable is used before being initialized.
    /// This helps catch potential null reference errors and logic bugs.
    /// </summary>
    public class GDUninitializedVariableRule : GDLintRule
    {
        public override string RuleId => "GDL250";
        public override string Name => "uninitialized-variable";
        public override string Description => "Warn when variable may be used before initialization";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        private HashSet<string> _uninitializedVars = new HashSet<string>();
        private HashSet<string> _initializedVars = new HashSet<string>();
        private HashSet<string> _reportedVars = new HashSet<string>();

        public override void Visit(GDMethodDeclaration method)
        {
            if (Options?.WarnUninitializedVariable != true)
                return;

            _uninitializedVars.Clear();
            _initializedVars.Clear();
            _reportedVars.Clear();
        }

        public override void Visit(GDVariableDeclarationStatement varDecl)
        {
            if (Options?.WarnUninitializedVariable != true)
                return;

            var varName = varDecl.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            if (varDecl.Initializer == null)
            {
                // Variable declared without initializer
                _uninitializedVars.Add(varName);
                _initializedVars.Remove(varName);
            }
            else
            {
                // Variable declared with initializer - check if initializer uses the same variable
                // (e.g., var x = x + 1 is a separate error, but we should mark x as initialized)
                _initializedVars.Add(varName);
                _uninitializedVars.Remove(varName);
            }
        }

        public override void Visit(GDIdentifierExpression idExpr)
        {
            if (Options?.WarnUninitializedVariable != true)
                return;

            var varName = idExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Skip if already reported
            if (_reportedVars.Contains(varName))
                return;

            // Check if this is a read access to an uninitialized variable
            if (_uninitializedVars.Contains(varName) && !_initializedVars.Contains(varName))
            {
                // Check if this is in a write context (left side of assignment)
                if (!IsWriteContext(idExpr))
                {
                    ReportIssue(
                        $"Variable '{varName}' may be used before initialization",
                        idExpr,
                        "Initialize the variable before use or assign a default value");
                    _reportedVars.Add(varName);
                }
            }
        }

        public override void Visit(GDDualOperatorExpression dual)
        {
            if (Options?.WarnUninitializedVariable != true)
                return;

            // Handle assignments: mark variable as initialized
            if (IsAssignmentOperator(dual.OperatorType))
            {
                if (dual.LeftExpression is GDIdentifierExpression leftId)
                {
                    var varName = leftId.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(varName))
                    {
                        // Mark as initialized (will be processed after right side is visited)
                        _initializedVars.Add(varName);
                        _uninitializedVars.Remove(varName);
                    }
                }
            }
        }

        private bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            return opType == GDDualOperatorType.Assignment ||
                   opType == GDDualOperatorType.AddAndAssign ||
                   opType == GDDualOperatorType.SubtractAndAssign ||
                   opType == GDDualOperatorType.MultiplyAndAssign ||
                   opType == GDDualOperatorType.DivideAndAssign ||
                   opType == GDDualOperatorType.ModAndAssign ||
                   opType == GDDualOperatorType.PowerAndAssign ||
                   opType == GDDualOperatorType.BitwiseAndAndAssign ||
                   opType == GDDualOperatorType.BitwiseOrAndAssign ||
                   opType == GDDualOperatorType.XorAndAssign ||
                   opType == GDDualOperatorType.BitShiftLeftAndAssign ||
                   opType == GDDualOperatorType.BitShiftRightAndAssign;
        }

        private bool IsWriteContext(GDIdentifierExpression idExpr)
        {
            // Check if this identifier is on the left side of an assignment
            var parent = idExpr.Parent;
            if (parent is GDDualOperatorExpression dual &&
                IsAssignmentOperator(dual.OperatorType))
            {
                return dual.LeftExpression == idExpr;
            }

            return false;
        }
    }
}
