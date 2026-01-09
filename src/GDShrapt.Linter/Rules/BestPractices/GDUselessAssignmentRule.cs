using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a variable is assigned but never read before being reassigned or going out of scope.
    /// </summary>
    public class GDUselessAssignmentRule : GDLintRule
    {
        public override string RuleId => "GDL231";
        public override string Name => "useless-assignment";
        public override string Description => "Warn when assigned value is never read";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDMethodDeclaration method)
        {
            if (!Options?.WarnUselessAssignment ?? false)
            {
                base.Visit(method);
                return;
            }

            if (method?.Statements == null)
            {
                base.Visit(method);
                return;
            }

            // Track assignments and usages
            var assignments = new Dictionary<string, List<AssignmentInfo>>();
            var usages = new HashSet<string>();

            AnalyzeStatements(method.Statements, assignments, usages);

            // Report assignments that are never read
            foreach (var kvp in assignments)
            {
                var varName = kvp.Key;
                var assignList = kvp.Value;

                // If the variable is never used at all, it's already handled by unused-variable rule
                if (!usages.Contains(varName))
                    continue;

                // Check for reassignments without reading
                for (int i = 0; i < assignList.Count - 1; i++)
                {
                    var current = assignList[i];

                    // If no usage between these assignments, the first one is useless
                    if (!current.WasRead)
                    {
                        ReportIssue(
                            $"Value assigned to '{varName}' is never read before being overwritten",
                            current.Token,
                            "Remove this assignment or use the value before reassigning");
                    }
                }
            }

            base.Visit(method);
        }

        private void AnalyzeStatements(GDStatementsList statements, Dictionary<string, List<AssignmentInfo>> assignments, HashSet<string> usages)
        {
            if (statements == null)
                return;

            foreach (var stmt in statements)
            {
                AnalyzeStatement(stmt, assignments, usages);
            }
        }

        private void AnalyzeStatement(GDStatement stmt, Dictionary<string, List<AssignmentInfo>> assignments, HashSet<string> usages)
        {
            if (stmt == null)
                return;

            // Handle variable declaration with initialization
            if (stmt is GDVariableDeclarationStatement varDecl)
            {
                var varName = varDecl.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName) && varDecl.Initializer != null)
                {
                    // First, record usages in the initializer
                    RecordUsages(varDecl.Initializer, assignments, usages);

                    // Then record the assignment
                    if (!assignments.ContainsKey(varName))
                        assignments[varName] = new List<AssignmentInfo>();

                    assignments[varName].Add(new AssignmentInfo
                    {
                        Token = varDecl.Identifier,
                        WasRead = false
                    });
                }
            }
            // Handle assignment expressions (GDDualOperatorExpression with Assignment operator)
            else if (stmt is GDExpressionStatement exprStmt &&
                     exprStmt.Expression is GDDualOperatorExpression dualOp &&
                     IsAssignmentOperator(dualOp.OperatorType))
            {
                // First, record usages in the right-hand side
                RecordUsages(dualOp.RightExpression, assignments, usages);

                // Get the variable being assigned
                var varName = GetAssignmentTarget(dualOp.LeftExpression);
                if (!string.IsNullOrEmpty(varName))
                {
                    if (!assignments.ContainsKey(varName))
                        assignments[varName] = new List<AssignmentInfo>();

                    // For compound assignments, the variable is also being read
                    if (dualOp.OperatorType != GDDualOperatorType.Assignment)
                    {
                        MarkAsRead(varName, assignments);
                    }

                    assignments[varName].Add(new AssignmentInfo
                    {
                        Token = dualOp.LeftExpression?.FirstChildToken,
                        WasRead = false
                    });
                }
            }
            else
            {
                // Record all usages in the statement
                RecordUsagesInNode(stmt, assignments, usages);
            }
        }

        private void RecordUsages(GDExpression expr, Dictionary<string, List<AssignmentInfo>> assignments, HashSet<string> usages)
        {
            if (expr == null)
                return;

            RecordUsagesInNode(expr, assignments, usages);
        }

        private void RecordUsagesInNode(GDNode node, Dictionary<string, List<AssignmentInfo>> assignments, HashSet<string> usages)
        {
            foreach (var child in node.AllNodes)
            {
                if (child is GDIdentifierExpression identExpr)
                {
                    var varName = identExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(varName))
                    {
                        usages.Add(varName);
                        MarkAsRead(varName, assignments);
                    }
                }
            }
        }

        private void MarkAsRead(string varName, Dictionary<string, List<AssignmentInfo>> assignments)
        {
            if (assignments.TryGetValue(varName, out var list) && list.Count > 0)
            {
                list[list.Count - 1].WasRead = true;
            }
        }

        private string GetAssignmentTarget(GDExpression left)
        {
            // Simple identifier
            if (left is GDIdentifierExpression identExpr)
                return identExpr.Identifier?.Sequence;

            // For member access (self.x) or indexer (arr[0]), return null
            // as tracking those is more complex
            return null;
        }

        private bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            switch (opType)
            {
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.XorAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                    return true;
                default:
                    return false;
            }
        }

        private class AssignmentInfo
        {
            public GDSyntaxToken Token { get; set; }
            public bool WasRead { get; set; }
        }
    }
}
