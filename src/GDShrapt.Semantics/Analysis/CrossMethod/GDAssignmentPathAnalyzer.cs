using GDShrapt.Reader;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Analyzes assignment paths through a method to detect
    /// unconditional vs conditional variable initialization.
    /// </summary>
    internal class GDAssignmentPathAnalyzer : GDVisitor
    {
        private readonly HashSet<string> _classVariables;
        private readonly Dictionary<string, AssignmentInfo> _assignments = new();

        // Branch tracking
        private readonly Stack<BranchContext> _branchStack = new();
        private int _branchDepth = 0;
        private bool _hasElseBranch = false;
        private readonly Stack<bool> _elsePresenceStack = new();

        public GDAssignmentPathAnalyzer(IEnumerable<string> classVariables)
        {
            _classVariables = new HashSet<string>(classVariables);
        }

        public AssignmentAnalysisResult Analyze(GDMethodDeclaration method)
        {
            if (method?.Statements == null)
                return new AssignmentAnalysisResult();

            method.WalkIn(this);

            var result = new AssignmentAnalysisResult();

            foreach (var kvp in _assignments)
            {
                var varName = kvp.Key;
                var info = kvp.Value;

                if (info.IsUnconditional())
                {
                    result.Unconditional.Add(varName);
                }
                else if (info.IsConditional())
                {
                    result.Conditional.Add(varName);
                }
            }

            return result;
        }

        #region If Statement Handling

        public override void Visit(GDIfStatement ifStmt)
        {
            _branchDepth++;
            _elsePresenceStack.Push(false);
            _branchStack.Push(new BranchContext
            {
                Depth = _branchDepth,
                BranchAssignments = new Dictionary<string, HashSet<int>>()
            });
        }

        public override void Visit(GDIfBranch ifBranch)
        {
            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Peek();
                ctx.CurrentBranchIndex = 0;
            }
        }

        public override void Left(GDIfBranch ifBranch)
        {
            // Branch assignments are tracked per-branch
        }

        public override void Visit(GDElifBranch elifBranch)
        {
            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Peek();
                ctx.CurrentBranchIndex++;
            }
        }

        public override void Left(GDElifBranch elifBranch)
        {
            // Branch assignments are tracked per-branch
        }

        public override void Visit(GDElseBranch elseBranch)
        {
            if (_elsePresenceStack.Count > 0)
            {
                _elsePresenceStack.Pop();
                _elsePresenceStack.Push(true);
            }

            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Peek();
                ctx.CurrentBranchIndex++;
                ctx.HasElse = true;
            }
        }

        public override void Left(GDElseBranch elseBranch)
        {
            // Branch assignments are tracked per-branch
        }

        public override void Left(GDIfStatement ifStmt)
        {
            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Pop();
                var hasElse = _elsePresenceStack.Count > 0 && _elsePresenceStack.Pop();

                // Check if any variables are assigned in ALL branches
                foreach (var kvp in ctx.BranchAssignments)
                {
                    var varName = kvp.Key;
                    var branchesWithAssignment = kvp.Value;

                    // Count expected branches: if + elifs + else (if present)
                    var expectedBranches = ctx.CurrentBranchIndex + 1;

                    if (!_assignments.TryGetValue(varName, out var info))
                    {
                        info = new AssignmentInfo();
                        _assignments[varName] = info;
                    }

                    if (hasElse && branchesWithAssignment.Count == expectedBranches)
                    {
                        // Assigned in ALL branches (including else) - unconditional at this if level
                        info.AllBranchAssignments.Add(_branchDepth);
                    }
                    else
                    {
                        // Only in some branches - conditional
                        info.PartialBranchAssignments.Add(_branchDepth);
                    }
                }
            }

            _branchDepth--;
        }

        #endregion

        #region Assignment Detection

        public override void Visit(GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;
            if (opType == null)
                return;

            if (!IsAssignmentOperator(opType.Value))
                return;

            var varName = GetTargetVariable(dualOp.LeftExpression);
            if (string.IsNullOrEmpty(varName) || !_classVariables.Contains(varName))
                return;

            RecordAssignment(varName);
        }

        private static string? GetTargetVariable(GDExpression? expr)
        {
            if (expr is GDIdentifierExpression ident)
                return ident.Identifier?.Sequence;

            // Handle self.var
            if (expr is GDMemberOperatorExpression member &&
                member.CallerExpression is GDIdentifierExpression selfIdent &&
                selfIdent.Identifier?.Sequence == "self")
            {
                return member.Identifier?.Sequence;
            }

            return null;
        }

        private void RecordAssignment(string varName)
        {
            if (!_assignments.TryGetValue(varName, out var info))
            {
                info = new AssignmentInfo();
                _assignments[varName] = info;
            }

            if (_branchDepth == 0)
            {
                // Top-level assignment - unconditional
                info.TopLevelAssignment = true;
            }
            else if (_branchStack.Count > 0)
            {
                // Inside a branch - track which branch
                var ctx = _branchStack.Peek();
                if (!ctx.BranchAssignments.TryGetValue(varName, out var branches))
                {
                    branches = new HashSet<int>();
                    ctx.BranchAssignments[varName] = branches;
                }
                branches.Add(ctx.CurrentBranchIndex);
            }
        }

        private static bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            return opType switch
            {
                GDDualOperatorType.Assignment => true,
                GDDualOperatorType.AddAndAssign => true,
                GDDualOperatorType.SubtractAndAssign => true,
                GDDualOperatorType.MultiplyAndAssign => true,
                GDDualOperatorType.DivideAndAssign => true,
                GDDualOperatorType.ModAndAssign => true,
                GDDualOperatorType.BitwiseAndAndAssign => true,
                GDDualOperatorType.BitwiseOrAndAssign => true,
                GDDualOperatorType.PowerAndAssign => true,
                GDDualOperatorType.BitShiftLeftAndAssign => true,
                GDDualOperatorType.BitShiftRightAndAssign => true,
                GDDualOperatorType.XorAndAssign => true,
                _ => false
            };
        }

        #endregion

        #region Loop Handling

        public override void Visit(GDForStatement forStmt)
        {
            // Entering a loop - assignments inside are conditional
            // (loop may not execute)
            _branchDepth++;
            _branchStack.Push(new BranchContext
            {
                Depth = _branchDepth,
                IsLoop = true,
                BranchAssignments = new Dictionary<string, HashSet<int>>()
            });
        }

        public override void Left(GDForStatement forStmt)
        {
            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Pop();
                // Loop assignments are always conditional (loop may not execute)
                foreach (var varName in ctx.BranchAssignments.Keys)
                {
                    if (!_assignments.TryGetValue(varName, out var info))
                    {
                        info = new AssignmentInfo();
                        _assignments[varName] = info;
                    }
                    info.PartialBranchAssignments.Add(_branchDepth);
                }
            }
            _branchDepth--;
        }

        public override void Visit(GDWhileStatement whileStmt)
        {
            _branchDepth++;
            _branchStack.Push(new BranchContext
            {
                Depth = _branchDepth,
                IsLoop = true,
                BranchAssignments = new Dictionary<string, HashSet<int>>()
            });
        }

        public override void Left(GDWhileStatement whileStmt)
        {
            if (_branchStack.Count > 0)
            {
                var ctx = _branchStack.Pop();
                // Loop assignments are always conditional
                foreach (var varName in ctx.BranchAssignments.Keys)
                {
                    if (!_assignments.TryGetValue(varName, out var info))
                    {
                        info = new AssignmentInfo();
                        _assignments[varName] = info;
                    }
                    info.PartialBranchAssignments.Add(_branchDepth);
                }
            }
            _branchDepth--;
        }

        #endregion

        #region Helper Classes

        private class BranchContext
        {
            public int Depth { get; set; }
            public int CurrentBranchIndex { get; set; }
            public bool HasElse { get; set; }
            public bool IsLoop { get; set; }
            public Dictionary<string, HashSet<int>> BranchAssignments { get; set; } = new();
        }

        private class AssignmentInfo
        {
            public bool TopLevelAssignment { get; set; }
            public HashSet<int> AllBranchAssignments { get; } = new();
            public HashSet<int> PartialBranchAssignments { get; } = new();

            public bool IsUnconditional()
            {
                // Unconditional if:
                // 1. Assigned at top level, OR
                // 2. Assigned in ALL branches of an if/else at some level (and no partial assignments)
                return TopLevelAssignment ||
                       (AllBranchAssignments.Count > 0 && PartialBranchAssignments.Count == 0);
            }

            public bool IsConditional()
            {
                // Conditional if:
                // 1. Any partial branch assignments, OR
                // 2. All-branch assignments without top-level (could have failed condition)
                return !TopLevelAssignment && PartialBranchAssignments.Count > 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of assignment path analysis.
    /// </summary>
    internal class AssignmentAnalysisResult
    {
        /// <summary>
        /// Variables unconditionally initialized (on ALL paths).
        /// </summary>
        public HashSet<string> Unconditional { get; } = new();

        /// <summary>
        /// Variables conditionally initialized (on SOME paths only).
        /// </summary>
        public HashSet<string> Conditional { get; } = new();
    }
}
