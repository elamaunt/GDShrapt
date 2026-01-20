using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Performs flow-sensitive type analysis on a method body.
/// Tracks variable types through assignments and control flow (if/elif/else).
/// Supports type narrowing from 'is' checks.
/// </summary>
internal class GDFlowAnalyzer : GDVisitor
{
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly Stack<GDFlowState> _stateStack = new();
    private readonly Stack<List<GDFlowState>> _branchStatesStack = new();
    private GDFlowState _currentState;

    // Results: maps AST nodes to their flow state at that point
    private readonly Dictionary<GDNode, GDFlowState> _nodeStates = new();

    // Track statement order for linear flow
    private readonly Dictionary<GDNode, int> _statementOrder = new();
    private int _currentOrder;

    public GDFlowAnalyzer(GDTypeInferenceEngine? typeEngine)
    {
        _typeEngine = typeEngine;
        _currentState = new GDFlowState();
    }

    /// <summary>
    /// Gets the computed flow states for AST nodes.
    /// </summary>
    public IReadOnlyDictionary<GDNode, GDFlowState> NodeStates => _nodeStates;

    /// <summary>
    /// Gets the final flow state after analysis.
    /// </summary>
    public GDFlowState FinalState => _currentState;

    /// <summary>
    /// Analyzes a method declaration for flow-sensitive types.
    /// </summary>
    public void Analyze(GDMethodDeclaration method)
    {
        if (method == null)
            return;

        // Initialize parameters
        if (method.Parameters != null)
        {
            foreach (var param in method.Parameters)
            {
                var name = param.Identifier?.Sequence;
                var declType = param.Type?.BuildName();
                if (!string.IsNullOrEmpty(name))
                    _currentState.DeclareVariable(name, declType);
            }
        }

        // Walk the method body
        method.WalkIn(this);
    }

    #region Variable Declarations

    public override void Visit(GDVariableDeclarationStatement varDecl)
    {
        var name = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var declType = varDecl.Type?.BuildName();
        string? initType = null;

        if (varDecl.Initializer != null)
        {
            initType = _typeEngine?.InferType(varDecl.Initializer);
        }

        _currentState.DeclareVariable(name, declType, initType);
        RecordState(varDecl);
    }

    #endregion

    #region Assignments

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        if (opType == null)
            return;

        // Handle assignment operators
        if (IsAssignmentOperator(opType.Value))
        {
            if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    var rhsType = _typeEngine?.InferType(dualOp.RightExpression);
                    if (!string.IsNullOrEmpty(rhsType))
                    {
                        _currentState.SetVariableType(name, rhsType, dualOp);
                    }
                }
            }
            RecordState(dualOp);
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

    #region If Statements

    public override void Visit(GDIfStatement ifStmt)
    {
        // Save current state before branching
        _stateStack.Push(_currentState);
        // Create list to collect branch end states
        _branchStatesStack.Push(new List<GDFlowState>());
    }

    public override void Visit(GDIfBranch ifBranch)
    {
        // Create child state for if branch
        var branchState = _currentState.CreateChild();

        // Apply narrowing from condition
        ApplyNarrowingFromCondition(ifBranch.Condition, branchState);

        _currentState = branchState;
        RecordState(ifBranch);
    }

    public override void Left(GDIfBranch ifBranch)
    {
        // Save branch end state for merging
        RecordState(ifBranch);
        if (_branchStatesStack.Count > 0)
        {
            _branchStatesStack.Peek().Add(_currentState);
        }
    }

    public override void Visit(GDElifBranch elifBranch)
    {
        // Create child state from parent (not from previous branch)
        var parentState = _stateStack.Count > 0 ? _stateStack.Peek() : _currentState;
        var branchState = parentState.CreateChild();

        // Apply narrowing from condition
        ApplyNarrowingFromCondition(elifBranch.Condition, branchState);

        _currentState = branchState;
        RecordState(elifBranch);
    }

    public override void Left(GDElifBranch elifBranch)
    {
        // Save branch end state for merging
        RecordState(elifBranch);
        if (_branchStatesStack.Count > 0)
        {
            _branchStatesStack.Peek().Add(_currentState);
        }
    }

    public override void Visit(GDElseBranch elseBranch)
    {
        // Create child state from parent (not from previous branch)
        var parentState = _stateStack.Count > 0 ? _stateStack.Peek() : _currentState;
        _currentState = parentState.CreateChild();
        RecordState(elseBranch);
    }

    public override void Left(GDElseBranch elseBranch)
    {
        // Save branch end state for merging
        RecordState(elseBranch);
        if (_branchStatesStack.Count > 0)
        {
            _branchStatesStack.Peek().Add(_currentState);
        }
    }

    public override void Left(GDIfStatement ifStmt)
    {
        var parentState = _stateStack.Count > 0 ? _stateStack.Pop() : _currentState;
        var branchStates = _branchStatesStack.Count > 0 ? _branchStatesStack.Pop() : new List<GDFlowState>();

        if (branchStates.Count == 0)
        {
            // No branches - keep parent state
            _currentState = parentState;
        }
        else if (branchStates.Count == 1)
        {
            // Single branch (if without else) - merge with parent (else path = parent)
            _currentState = GDFlowState.MergeBranches(branchStates[0], parentState, parentState);
        }
        else
        {
            // Multiple branches - merge all
            _currentState = MergeMultipleBranches(branchStates, parentState);
        }

        RecordState(ifStmt);
    }

    private GDFlowState MergeMultipleBranches(List<GDFlowState> branches, GDFlowState parent)
    {
        if (branches.Count == 0)
            return parent;
        if (branches.Count == 1)
            return GDFlowState.MergeBranches(branches[0], parent, parent);

        // Merge first two
        var merged = GDFlowState.MergeBranches(branches[0], branches[1], parent);

        // Merge remaining
        for (int i = 2; i < branches.Count; i++)
        {
            merged = GDFlowState.MergeBranches(merged, branches[i], parent);
        }

        return merged;
    }

    #endregion

    #region For/While Loops

    // Maximum iterations for fixed-point analysis to prevent infinite loops
    private const int MaxFixedPointIterations = 10;

    // Track loop body states for fixed-point iteration
    private readonly Stack<LoopAnalysisContext> _loopContextStack = new();

    private class LoopAnalysisContext
    {
        public GDFlowState PreLoopState { get; set; } = GDFlowState.Empty;
        public GDFlowState? CurrentIterationState { get; set; }
        public int IterationCount { get; set; }
        public Dictionary<string, HashSet<string>>? PreviousSnapshot { get; set; }
        public string? IteratorName { get; set; }
        public string? IteratorType { get; set; }
    }

    public override void Visit(GDForStatement forStmt)
    {
        // Save current state before loop
        var preLoopState = _currentState;
        _stateStack.Push(preLoopState);

        // Create context for fixed-point iteration
        var context = new LoopAnalysisContext
        {
            PreLoopState = preLoopState,
            IterationCount = 0
        };

        // Declare iterator variable with inferred type from collection
        var iteratorName = forStmt.Variable?.Sequence;
        if (!string.IsNullOrEmpty(iteratorName))
        {
            var collectionType = _typeEngine?.InferType(forStmt.Collection);
            var elementType = InferIteratorElementType(collectionType);
            context.IteratorName = iteratorName;
            context.IteratorType = elementType;
        }

        _loopContextStack.Push(context);

        // Create initial loop body state
        var loopState = preLoopState.CreateChild();
        if (!string.IsNullOrEmpty(context.IteratorName))
        {
            loopState.DeclareVariable(context.IteratorName, null, context.IteratorType);
        }

        _currentState = loopState;
        RecordState(forStmt);
    }

    public override void Left(GDForStatement forStmt)
    {
        var parentState = _stateStack.Count > 0 ? _stateStack.Pop() : _currentState;
        var context = _loopContextStack.Count > 0 ? _loopContextStack.Pop() : null;

        if (context != null)
        {
            // Perform fixed-point iteration to stabilize loop types
            _currentState = ComputeLoopFixedPoint(
                context.PreLoopState,
                _currentState,
                context.IteratorName,
                context.IteratorType,
                forStmt.Statements);
        }
        else
        {
            // Fallback: simple merge
            _currentState = GDFlowState.MergeBranches(_currentState, parentState, parentState);
        }

        RecordState(forStmt);
    }

    public override void Visit(GDWhileStatement whileStmt)
    {
        // Save current state before loop
        var preLoopState = _currentState;
        _stateStack.Push(preLoopState);

        // Create context for fixed-point iteration
        var context = new LoopAnalysisContext
        {
            PreLoopState = preLoopState,
            IterationCount = 0
        };
        _loopContextStack.Push(context);

        // Create child state for loop body
        var loopState = preLoopState.CreateChild();

        // Apply narrowing from condition (e.g., while x is Player:)
        ApplyNarrowingFromCondition(whileStmt.Condition, loopState);

        _currentState = loopState;
        RecordState(whileStmt);
    }

    public override void Left(GDWhileStatement whileStmt)
    {
        var parentState = _stateStack.Count > 0 ? _stateStack.Pop() : _currentState;
        var context = _loopContextStack.Count > 0 ? _loopContextStack.Pop() : null;

        if (context != null)
        {
            // Perform fixed-point iteration to stabilize loop types
            _currentState = ComputeLoopFixedPoint(
                context.PreLoopState,
                _currentState,
                null,
                null,
                whileStmt.Statements);
        }
        else
        {
            // Fallback: simple merge
            _currentState = GDFlowState.MergeBranches(_currentState, parentState, parentState);
        }

        RecordState(whileStmt);
    }

    /// <summary>
    /// Computes the fixed-point for loop type analysis.
    /// Iterates until types stabilize or max iterations reached.
    /// </summary>
    private GDFlowState ComputeLoopFixedPoint(
        GDFlowState preLoopState,
        GDFlowState firstIterationState,
        string? iteratorName,
        string? iteratorType,
        GDStatementsList? statements)
    {
        // Start with the result of the first iteration
        var currentState = firstIterationState;

        // Get initial snapshot
        var previousSnapshot = currentState.GetTypeSnapshot();

        // Iterate until fixed point or max iterations
        for (int i = 0; i < MaxFixedPointIterations; i++)
        {
            // Create a merged state: pre-loop merged with current iteration
            // This simulates another iteration where the loop body starts
            // with types that could come from either before the loop or after previous iteration
            var mergedEntry = GDFlowState.MergeBranches(currentState, preLoopState, preLoopState);

            // Create a new iteration state from the merged entry
            var iterationState = mergedEntry.CreateChild();

            // Re-declare iterator if present
            if (!string.IsNullOrEmpty(iteratorName))
            {
                iterationState.DeclareVariable(iteratorName, null, iteratorType);
            }

            // Merge the new iteration state into current state
            // This accumulates types across iterations
            var changed = currentState.MergeInto(iterationState);

            // Check if we've reached a fixed point (no new types added)
            if (!changed)
            {
                break;
            }

            // Also check via snapshot comparison
            var newSnapshot = currentState.GetTypeSnapshot();
            if (currentState.MatchesSnapshot(previousSnapshot))
            {
                break;
            }

            previousSnapshot = newSnapshot;
        }

        // Final merge: loop may execute 0+ times
        // So the result is the union of pre-loop state (0 iterations)
        // and the fixed-point state (1+ iterations)
        return GDFlowState.MergeBranches(currentState, preLoopState, preLoopState);
    }

    /// <summary>
    /// Infers the element type from a collection type.
    /// </summary>
    private static string? InferIteratorElementType(string? collectionType)
    {
        if (string.IsNullOrEmpty(collectionType))
            return "Variant";

        // Handle typed arrays: Array[Type] -> Type
        if (collectionType.StartsWith("Array[") && collectionType.EndsWith("]"))
        {
            return collectionType.Substring(6, collectionType.Length - 7);
        }

        // Handle range() -> int
        if (collectionType == "Range" || collectionType == "int")
            return "int";

        // Handle String -> String (iterating chars)
        if (collectionType == "String")
            return "String";

        // Handle Dictionary -> Variant (iterating keys)
        if (collectionType == "Dictionary" || collectionType.StartsWith("Dictionary["))
            return "Variant";

        // Handle PackedArray types
        if (collectionType.StartsWith("Packed") && collectionType.EndsWith("Array"))
        {
            // PackedStringArray -> String, PackedInt32Array -> int, etc.
            var inner = collectionType.Substring(6, collectionType.Length - 11);
            return inner switch
            {
                "String" => "String",
                "Int32" or "Int64" => "int",
                "Float32" or "Float64" => "float",
                "Vector2" => "Vector2",
                "Vector3" => "Vector3",
                "Color" => "Color",
                "Byte" => "int",
                _ => "Variant"
            };
        }

        return "Variant";
    }

    #endregion

    #region Match Statements

    public override void Visit(GDMatchStatement matchStmt)
    {
        // Save current state before match
        _stateStack.Push(_currentState);
        // Create list to collect case end states
        _branchStatesStack.Push(new List<GDFlowState>());
        RecordState(matchStmt);
    }

    public override void Visit(GDMatchCaseDeclaration matchCase)
    {
        // Create child state from parent (not from previous case)
        var parentState = _stateStack.Count > 0 ? _stateStack.Peek() : _currentState;
        var caseState = parentState.CreateChild();

        // Declare any binding variables from match patterns
        DeclareMatchBindings(matchCase, caseState);

        _currentState = caseState;
        RecordState(matchCase);
    }

    public override void Left(GDMatchCaseDeclaration matchCase)
    {
        // Save case end state for merging
        RecordState(matchCase);
        if (_branchStatesStack.Count > 0)
        {
            _branchStatesStack.Peek().Add(_currentState);
        }
    }

    public override void Left(GDMatchStatement matchStmt)
    {
        var parentState = _stateStack.Count > 0 ? _stateStack.Pop() : _currentState;
        var caseStates = _branchStatesStack.Count > 0 ? _branchStatesStack.Pop() : new List<GDFlowState>();

        if (caseStates.Count == 0)
        {
            // No cases - keep parent state
            _currentState = parentState;
        }
        else
        {
            // Merge all case states (similar to if/elif/else)
            _currentState = MergeMultipleBranches(caseStates, parentState);
        }

        RecordState(matchStmt);
    }

    /// <summary>
    /// Declares binding variables from match case patterns.
    /// </summary>
    private void DeclareMatchBindings(GDMatchCaseDeclaration matchCase, GDFlowState state)
    {
        if (matchCase.Conditions == null)
            return;

        foreach (var condition in matchCase.Conditions)
        {
            DeclareBindingsFromPattern(condition, state);
        }
    }

    /// <summary>
    /// Recursively declares binding variables from a pattern expression.
    /// </summary>
    private void DeclareBindingsFromPattern(GDExpression? pattern, GDFlowState state)
    {
        if (pattern == null)
            return;

        // Handle var binding: var x
        if (pattern is GDMatchCaseVariableExpression varExpr)
        {
            var name = varExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name))
            {
                // Binding variables get type from the matched value (Variant for now)
                state.DeclareVariable(name, null, "Variant");
            }
            return;
        }

        // Handle array patterns: [a, b, c]
        if (pattern is GDArrayInitializerExpression arrayExpr)
        {
            foreach (var element in arrayExpr.Values ?? Enumerable.Empty<GDExpression>())
            {
                DeclareBindingsFromPattern(element, state);
            }
            return;
        }

        // Handle dictionary patterns: {key: value}
        if (pattern is GDDictionaryInitializerExpression dictExpr)
        {
            foreach (var kvp in dictExpr.KeyValues ?? Enumerable.Empty<GDDictionaryKeyValueDeclaration>())
            {
                DeclareBindingsFromPattern(kvp.Value, state);
            }
        }
    }

    #endregion

    #region Lambda Expressions

    // Track lambdas and their captured state
    private readonly Dictionary<GDMethodExpression, GDFlowState> _lambdaCaptureStates = new();

    /// <summary>
    /// Gets the flow state captured at lambda creation time.
    /// </summary>
    public IReadOnlyDictionary<GDMethodExpression, GDFlowState> LambdaCaptureStates => _lambdaCaptureStates;

    public override void Visit(GDMethodExpression lambdaExpr)
    {
        // Record the flow state at lambda creation time
        // This captures the types of variables visible to the lambda
        _lambdaCaptureStates[lambdaExpr] = _currentState;
        RecordState(lambdaExpr);

        // Create a child state for the lambda body
        // Lambda parameters are local to the lambda
        var lambdaState = _currentState.CreateChild();

        // Add lambda parameters to the lambda state
        if (lambdaExpr.Parameters != null)
        {
            foreach (var param in lambdaExpr.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                var paramType = param.Type?.BuildName();
                if (!string.IsNullOrEmpty(paramName))
                    lambdaState.DeclareVariable(paramName, paramType);
            }
        }

        // Note: We don't walk into the lambda body here because:
        // 1. Lambda body executes at call time, not definition time
        // 2. Type inference inside lambda would need separate analysis
        // 3. For now, captured variables use their type at definition time
        // Future improvement: track lambda invocations and use call-time types
    }

    public override void Left(GDMethodExpression lambdaExpr)
    {
        // Lambda has its own scope, but doesn't affect outer flow state
        // (variables assigned inside lambda don't escape to outer scope)
        RecordState(lambdaExpr);
    }

    #endregion

    #region Control Flow Termination

    public override void Visit(GDReturnExpression returnExpr)
    {
        // Mark current state as terminated by return
        _currentState.MarkTerminated(TerminationType.Return);
        RecordState(returnExpr);
    }

    public override void Visit(GDBreakExpression breakExpr)
    {
        // Mark current state as terminated by break
        _currentState.MarkTerminated(TerminationType.Break);
        RecordState(breakExpr);
    }

    public override void Visit(GDContinueExpression continueExpr)
    {
        // Mark current state as terminated by continue
        _currentState.MarkTerminated(TerminationType.Continue);
        RecordState(continueExpr);
    }

    #endregion

    #region Type Narrowing

    private void ApplyNarrowingFromCondition(GDExpression? condition, GDFlowState state)
    {
        if (condition == null)
            return;

        // Handle: x is Type
        if (condition is GDDualOperatorExpression dualOp &&
            dualOp.Operator?.OperatorType == GDDualOperatorType.Is)
        {
            if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
            {
                var varName = identExpr.Identifier?.Sequence;
                var typeName = GetTypeNameFromExpression(dualOp.RightExpression);

                if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(typeName))
                {
                    state.NarrowType(varName, typeName);
                }
            }
        }

        // Handle: x is Type and y is OtherType
        if (condition is GDDualOperatorExpression andOp &&
            andOp.Operator?.OperatorType == GDDualOperatorType.And)
        {
            // Apply narrowing from both sides
            ApplyNarrowingFromCondition(andOp.LeftExpression, state);
            ApplyNarrowingFromCondition(andOp.RightExpression, state);
        }
    }

    private static string? GetTypeNameFromExpression(GDExpression? expr)
    {
        if (expr == null)
            return null;

        // Simple identifier: Dictionary, Array, Node, etc.
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        // Could also handle member expressions like SomeClass.InnerType
        if (expr is GDMemberOperatorExpression memberExpr)
        {
            var caller = GetTypeNameFromExpression(memberExpr.CallerExpression);
            var member = memberExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(member))
                return $"{caller}.{member}";
        }

        return expr.ToString();
    }

    #endregion

    #region State Recording

    private void RecordState(GDNode node)
    {
        _nodeStates[node] = _currentState;
        _statementOrder[node] = _currentOrder++;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets the type of a variable at a specific AST node location.
    /// </summary>
    public string? GetTypeAtLocation(string variableName, GDNode location)
    {
        if (string.IsNullOrEmpty(variableName) || location == null)
            return null;

        // Find the nearest state for this location by walking up
        var node = location;
        while (node != null)
        {
            if (_nodeStates.TryGetValue(node, out var state))
            {
                var varType = state.GetVariableType(variableName);
                if (varType != null)
                    return varType.EffectiveType;
            }
            node = node.Parent as GDNode;
        }

        // Fall back to final state
        var finalVarType = _currentState.GetVariableType(variableName);
        return finalVarType?.EffectiveType;
    }

    /// <summary>
    /// Gets the flow state at a specific location.
    /// </summary>
    public GDFlowState? GetStateAtLocation(GDNode location)
    {
        if (location == null)
            return null;

        var node = location;
        while (node != null)
        {
            if (_nodeStates.TryGetValue(node, out var state))
                return state;
            node = node.Parent as GDNode;
        }

        return _currentState;
    }

    /// <summary>
    /// Gets the full variable type info at a specific location.
    /// </summary>
    public GDFlowVariableType? GetVariableTypeAtLocation(string variableName, GDNode location)
    {
        var state = GetStateAtLocation(location);
        return state?.GetVariableType(variableName);
    }

    #endregion
}
