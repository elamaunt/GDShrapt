using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
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
    private readonly Func<GDExpression, string?>? _expressionTypeProvider;
    private readonly Stack<GDFlowState> _stateStack = new();
    private readonly Stack<List<GDFlowState>> _branchStatesStack = new();
    private GDFlowState _currentState;

    // Results: maps AST nodes to their flow state at that point
    private readonly Dictionary<GDNode, GDFlowState> _nodeStates = new();

    // Track statement order for linear flow
    private readonly Dictionary<GDNode, int> _statementOrder = new();
    private int _currentOrder;

    // Container usage profiles for untyped containers (for 'in' operator narrowing)
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();

    public GDFlowAnalyzer(GDTypeInferenceEngine? typeEngine)
    {
        _typeEngine = typeEngine;
        _currentState = new GDFlowState();
    }

    /// <summary>
    /// Creates a flow analyzer with an optional expression type provider callback.
    /// The callback can provide richer type information (e.g., from union types).
    /// </summary>
    public GDFlowAnalyzer(GDTypeInferenceEngine? typeEngine, Func<GDExpression, string?>? expressionTypeProvider)
        : this(typeEngine)
    {
        _expressionTypeProvider = expressionTypeProvider;
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

        // Collect container usage profiles for untyped containers
        CollectContainerProfiles(method);

        // Walk the method body
        method.WalkIn(this);
    }

    /// <summary>
    /// Collects container usage profiles from the method for untyped Array/Dictionary variables.
    /// </summary>
    private void CollectContainerProfiles(GDMethodDeclaration method)
    {
        var scopes = new GDScopeStack();
        var containerCollector = new GDContainerUsageCollector(scopes, _typeEngine);
        containerCollector.Collect(method);

        foreach (var kv in containerCollector.Profiles)
        {
            _containerProfiles[kv.Key] = kv.Value;
        }
    }

    #region Variable Declarations

    public override void Visit(GDVariableDeclarationStatement varDecl)
    {
        var name = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var declType = varDecl.Type?.BuildName();
        var initType = varDecl.Initializer != null
            ? ResolveTypeWithFallback(varDecl.Initializer)
            : null;

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

        if (IsAssignmentOperator(opType.Value))
        {
            if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    var rhsType = ResolveTypeWithFallback(dualOp.RightExpression);
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

    /// <summary>
    /// Resolves type for an expression, trying the expression type provider first,
    /// then falling back to the type engine. Avoids returning "Variant" when a more
    /// specific type is available.
    /// </summary>
    private string? ResolveTypeWithFallback(GDExpression? expr)
    {
        if (expr == null)
            return null;

        // Try expression type provider first (can use union types and richer inference)
        var primaryType = _expressionTypeProvider?.Invoke(expr);
        if (!string.IsNullOrEmpty(primaryType) && primaryType != "Variant")
            return primaryType;

        // Fall back to basic type engine inference
        var fallbackType = _typeEngine?.InferType(expr);
        if (!string.IsNullOrEmpty(fallbackType) && fallbackType != "Variant")
            return fallbackType;

        // Return Variant as last resort if that's what we got
        return primaryType ?? fallbackType;
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
        // DON'T RecordState here - we want the entry state with narrowing, not exit state
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
        // DON'T RecordState here - we want the entry state with narrowing, not exit state
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
        // DON'T RecordState here - we want the entry state, not exit state
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
            // Simulate another iteration: loop body starts with types from either before the loop or after previous iteration
            var mergedEntry = GDFlowState.MergeBranches(currentState, preLoopState, preLoopState);

            var iterationState = mergedEntry.CreateChild();

            // Re-declare iterator if present
            if (!string.IsNullOrEmpty(iteratorName))
            {
                iterationState.DeclareVariable(iteratorName, null, iteratorType);
            }

            // Merge the new iteration state into current state
            // This accumulates types across iterations
            var changed = currentState.MergeInto(iterationState);

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
    /// Extracts the generic type parameter from a generic type string.
    /// For example: "Array[int]" -> "int", "Dictionary[String, int]" -> "String, int"
    /// </summary>
    /// <param name="genericType">The generic type string.</param>
    /// <param name="prefix">The type prefix (e.g., "Array[").</param>
    /// <returns>The extracted type parameter, or null if not matching.</returns>
    private static string? ExtractGenericTypeParameter(string genericType, string prefix)
    {
        if (string.IsNullOrEmpty(genericType) ||
            !genericType.StartsWith(prefix) ||
            !genericType.EndsWith("]"))
        {
            return null;
        }

        return genericType.Substring(prefix.Length, genericType.Length - prefix.Length - 1);
    }

    /// <summary>
    /// Infers the element type from a collection type.
    /// </summary>
    private static string? InferIteratorElementType(string? collectionType)
    {
        if (string.IsNullOrEmpty(collectionType))
            return "Variant";

        // Handle typed arrays: Array[Type] -> Type
        var arrayElementType = ExtractGenericTypeParameter(collectionType, GDTypeInferenceConstants.ArrayTypePrefix);
        if (arrayElementType != null)
        {
            return arrayElementType;
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
        // Record the flow state at lambda creation time (captured variables)
        _lambdaCaptureStates[lambdaExpr] = _currentState;

        // Save parent state to restore after lambda
        _stateStack.Push(_currentState);

        // Create lambda scope with parameters
        var lambdaState = _currentState.CreateChild();

        // Add lambda parameters to the lambda state
        // Lambda parameters are never null - they receive values from iteration
        if (lambdaExpr.Parameters != null)
        {
            foreach (var param in lambdaExpr.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                var paramType = param.Type?.BuildName();
                if (!string.IsNullOrEmpty(paramName))
                {
                    lambdaState.DeclareVariable(paramName, paramType);
                    lambdaState.MarkNonNull(paramName);
                }
            }
        }

        // Use lambdaState for body traversal so parameters are visible
        _currentState = lambdaState;
        RecordState(lambdaExpr);
    }

    public override void Left(GDMethodExpression lambdaExpr)
    {
        // Restore parent state - lambda assignments don't escape to outer scope
        _currentState = _stateStack.Count > 0 ? _stateStack.Pop() : _currentState;
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
                    // Type narrowing implies non-null
                    state.MarkNonNull(varName);
                }
            }
        }

        // Handle: x is Type and y is OtherType (or other AND conditions)
        if (condition is GDDualOperatorExpression andOp &&
            andOp.Operator?.OperatorType == GDDualOperatorType.And)
        {
            // Apply narrowing from both sides
            ApplyNarrowingFromCondition(andOp.LeftExpression, state);
            ApplyNarrowingFromCondition(andOp.RightExpression, state);
            return; // Already handled
        }

        // Handle: x != null / x == null / x == literal
        if (condition is GDDualOperatorExpression eqOp)
        {
            var opType = eqOp.Operator?.OperatorType;
            if (opType == GDDualOperatorType.NotEqual || opType == GDDualOperatorType.Equal)
            {
                ApplyNullComparisonNarrowing(eqOp, state);

                // Also handle: x == literal (narrows to literal's type)
                if (opType == GDDualOperatorType.Equal)
                {
                    ApplyLiteralComparisonNarrowing(eqOp, state);
                }
            }

            // Handle: x in container (narrows to element/key type)
            if (opType == GDDualOperatorType.In)
            {
                ApplyInOperatorNarrowing(eqOp, state);
            }
        }

        // Handle: if x (truthiness check)
        if (condition is GDIdentifierExpression truthyIdent)
        {
            ApplyTruthinessNarrowing(truthyIdent, state);
        }

        // Handle: has_method(), has(), has_signal(), is_instance_valid()
        if (condition is GDCallExpression callExpr)
        {
            ApplyHasMethodNarrowing(callExpr, state);
            ApplyIsInstanceValidNarrowing(callExpr, state);
        }

        // Handle: not x
        if (condition is GDSingleOperatorExpression notOp)
        {
            var singleOpType = notOp.Operator?.OperatorType;
            if (singleOpType == GDSingleOperatorType.Not || singleOpType == GDSingleOperatorType.Not2)
            {
                // Negation inverts the narrowing
                // For now, we don't apply narrowing from negations in the true branch
                // (that would be handled in else branches)
            }
        }
    }

    /// <summary>
    /// Applies literal comparison narrowing.
    /// x == 42 -> x is narrowed to int
    /// x == "hello" -> x is narrowed to String
    /// </summary>
    private void ApplyLiteralComparisonNarrowing(GDDualOperatorExpression eqOp, GDFlowState state)
    {
        string? varName = null;
        string? literalType = null;

        // variable == literal
        if (eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
            IsLiteralExpression(eqOp.RightExpression))
        {
            varName = leftIdent.Identifier?.Sequence;
            literalType = GetLiteralType(eqOp.RightExpression);
        }
        // literal == variable
        else if (eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                 IsLiteralExpression(eqOp.LeftExpression))
        {
            varName = rightIdent.Identifier?.Sequence;
            literalType = GetLiteralType(eqOp.LeftExpression);
        }

        if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(literalType))
        {
            state.NarrowType(varName, literalType);
            // Non-null literals mark variable as non-null
            if (literalType != "null")
                state.MarkNonNull(varName);
        }
    }

    /// <summary>
    /// Applies narrowing from the 'in' operator.
    /// x in [1, 2, 3] -> x is narrowed to int
    /// x in {"a": 1} -> x is narrowed to String (dict key type)
    /// x in "hello" -> x is narrowed to String
    /// x in range(1, 10) -> x is narrowed to int
    /// </summary>
    private void ApplyInOperatorNarrowing(GDDualOperatorExpression inExpr, GDFlowState state)
    {
        // Left expression is the variable to narrow
        if (inExpr.LeftExpression is not GDIdentifierExpression leftIdent)
            return;

        var varName = leftIdent.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Right expression is the container
        var containerExpr = inExpr.RightExpression;
        if (containerExpr == null)
            return;

        // Try to extract element/key type from container
        var elementType = ExtractElementTypeFromContainer(containerExpr);
        if (string.IsNullOrEmpty(elementType) || elementType == "Variant")
            return;

        // Get current variable type for intersection calculation
        var currentVarType = state.GetVariableType(varName);

        // If variable has a known union type, compute intersection
        if (currentVarType != null && !currentVarType.CurrentType.IsEmpty)
        {
            var intersection = currentVarType.CurrentType.IntersectWithType(elementType, _typeEngine?.RuntimeProvider);

            if (!intersection.IsEmpty)
            {
                // Apply intersection result
                state.NarrowToIntersection(varName, intersection);
            }
            else
            {
                // Empty intersection means incompatible types, but GDScript is dynamic
                // Still narrow to container element type (runtime might succeed)
                state.NarrowType(varName, elementType);
            }
        }
        else
        {
            // No existing union type - use container element type directly
            state.NarrowType(varName, elementType);
        }

        state.MarkNonNull(varName);
    }

    /// <summary>
    /// Extracts the element/key type from a container expression.
    /// </summary>
    private string? ExtractElementTypeFromContainer(GDExpression? containerExpr)
    {
        if (containerExpr == null)
            return null;

        // Handle array literals: [1, 2, 3] -> int, ["a", "b"] -> String
        if (containerExpr is GDArrayInitializerExpression arrayInit)
        {
            return InferArrayLiteralElementType(arrayInit);
        }

        // Handle dictionary literals: {"a": 1} -> String (key type)
        if (containerExpr is GDDictionaryInitializerExpression dictInit)
        {
            return InferDictionaryLiteralKeyType(dictInit);
        }

        // Handle string literals: "hello" -> String
        if (containerExpr is GDStringExpression)
        {
            return "String";
        }

        // Handle range() calls: range(1, 10) -> int
        if (containerExpr is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDIdentifierExpression callIdent &&
                callIdent.Identifier?.Sequence == "range")
            {
                return "int";
            }
        }

        // Handle variable with known type or container profile
        if (containerExpr is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // First try typed variable
                var varType = _currentState.GetVariableType(varName);
                var typeName = varType?.EffectiveType;
                if (!string.IsNullOrEmpty(typeName) && typeName != "Array" && typeName != "Dictionary")
                {
                    var extractedType = ExtractElementTypeFromTypeName(typeName);
                    if (!string.IsNullOrEmpty(extractedType) && extractedType != "Variant")
                        return extractedType;
                }

                // For untyped containers - use container usage profile
                if (_containerProfiles.TryGetValue(varName, out var profile))
                {
                    var inferredElementType = profile.ComputeInferredType();
                    // For Dictionary, key type is used for 'in' operator
                    if (profile.IsDictionary && inferredElementType.KeyUnionType != null)
                    {
                        var keyType = inferredElementType.KeyUnionType.EffectiveType;
                        if (!string.IsNullOrEmpty(keyType) && keyType != "Variant")
                            return keyType;
                    }
                    // For Array, element type is used
                    if (!inferredElementType.ElementUnionType.IsEmpty)
                    {
                        var elementType = inferredElementType.ElementUnionType.EffectiveType;
                        if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
                            return elementType;
                    }
                }
            }
        }

        // Fall back to type engine inference
        var inferredType = ResolveTypeWithFallback(containerExpr);
        if (!string.IsNullOrEmpty(inferredType))
        {
            return ExtractElementTypeFromTypeName(inferredType);
        }

        return null;
    }

    /// <summary>
    /// Infers the element type from an array literal by examining its elements.
    /// Returns null if types are mixed or empty.
    /// </summary>
    private string? InferArrayLiteralElementType(GDArrayInitializerExpression arrayInit)
    {
        var values = arrayInit.Values?.ToList();
        if (values == null || values.Count == 0)
            return null;

        string? commonType = null;
        foreach (var value in values)
        {
            var elementType = GetLiteralType(value) ?? ResolveTypeWithFallback(value);
            if (string.IsNullOrEmpty(elementType) || elementType == "Unknown")
                continue;

            if (commonType == null)
            {
                commonType = elementType;
            }
            else if (commonType != elementType)
            {
                // Mixed types - check if they're compatible (e.g., int and float -> numeric)
                if (IsNumericType(commonType) && IsNumericType(elementType))
                {
                    // Keep as numeric (prefer float if mixed)
                    commonType = (commonType == "float" || elementType == "float") ? "float" : "int";
                }
                else
                {
                    // Truly mixed types - cannot narrow
                    return "Variant";
                }
            }
        }

        return commonType;
    }

    /// <summary>
    /// Infers the key type from a dictionary literal.
    /// </summary>
    private string? InferDictionaryLiteralKeyType(GDDictionaryInitializerExpression dictInit)
    {
        var keyValues = dictInit.KeyValues?.ToList();
        if (keyValues == null || keyValues.Count == 0)
            return null;

        string? commonKeyType = null;
        foreach (var kv in keyValues)
        {
            var keyType = GetLiteralType(kv.Key) ?? ResolveTypeWithFallback(kv.Key);
            if (string.IsNullOrEmpty(keyType) || keyType == "Unknown")
                continue;

            if (commonKeyType == null)
            {
                commonKeyType = keyType;
            }
            else if (commonKeyType != keyType)
            {
                // Mixed key types
                return "Variant";
            }
        }

        return commonKeyType;
    }

    /// <summary>
    /// Extracts element/key type from a type name.
    /// Array[int] -> int, Dictionary[String, int] -> String, String -> String
    /// </summary>
    private static string? ExtractElementTypeFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Array[T] -> T
        var arrayElement = ExtractGenericTypeParameter(typeName, GDTypeInferenceConstants.ArrayTypePrefix);
        if (arrayElement != null)
            return arrayElement;

        // Dictionary[K, V] -> K (key type only)
        if (typeName.StartsWith("Dictionary["))
        {
            var inner = typeName.Substring(11, typeName.Length - 12);
            var commaIndex = FindTopLevelComma(inner);
            if (commaIndex > 0)
                return inner.Substring(0, commaIndex).Trim();
        }

        // String -> String
        if (typeName == "String")
            return "String";

        // Range -> int
        if (typeName == "Range")
            return "int";

        // PackedArrays
        if (typeName.StartsWith("Packed") && typeName.EndsWith("Array"))
            return InferPackedArrayElementType(typeName);

        return null;
    }

    /// <summary>
    /// Finds the first comma at the top level (not inside nested generics).
    /// </summary>
    private static int FindTopLevelComma(string str)
    {
        int depth = 0;
        for (int i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (c == '[')
                depth++;
            else if (c == ']')
                depth--;
            else if (c == ',' && depth == 0)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Infers element type from packed array type names.
    /// </summary>
    private static string? InferPackedArrayElementType(string typeName)
    {
        // PackedStringArray -> String, PackedInt32Array -> int, etc.
        var inner = typeName.Substring(6, typeName.Length - 11);
        return inner switch
        {
            "String" => "String",
            "Int32" or "Int64" => "int",
            "Float32" or "Float64" => "float",
            "Vector2" => "Vector2",
            "Vector3" => "Vector3",
            "Color" => "Color",
            "Byte" => "int",
            _ => null
        };
    }

    private static bool IsNumericType(string? type) =>
        type == "int" || type == "float";

    /// <summary>
    /// Checks if expression is a literal value (not a variable).
    /// </summary>
    private static bool IsLiteralExpression(GDExpression? expr)
    {
        return expr is GDNumberExpression ||
               expr is GDStringExpression ||
               expr is GDBoolExpression ||
               expr is GDArrayInitializerExpression ||
               expr is GDDictionaryInitializerExpression ||
               IsNullLiteral(expr);
    }

    /// <summary>
    /// Gets the type of a literal expression.
    /// </summary>
    private static string? GetLiteralType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression numExpr => IsIntegerNumber(numExpr) ? "int" : "float",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            _ when IsNullLiteral(expr) => "null",
            _ => null
        };
    }

    /// <summary>
    /// Checks if a number expression represents an integer (no decimal point).
    /// </summary>
    private static bool IsIntegerNumber(GDNumberExpression numExpr)
    {
        var sequence = numExpr.Number?.Sequence;
        if (string.IsNullOrEmpty(sequence))
            return true; // Default to int if unknown

        // If it contains a dot, it's a float
        return !sequence.Contains('.');
    }

    /// <summary>
    /// Applies null comparison narrowing.
    /// x != null -> MarkNonNull
    /// x == null -> Mark as definitely null (inverse - for else branch)
    /// </summary>
    private void ApplyNullComparisonNarrowing(GDDualOperatorExpression eqOp, GDFlowState state)
    {
        var opType = eqOp.Operator?.OperatorType;
        var leftExpr = eqOp.LeftExpression;
        var rightExpr = eqOp.RightExpression;

        string? varName = null;
        bool rightIsNull = false;
        bool leftIsNull = false;

        // Check if right side is null (represented as GDIdentifierExpression with "null")
        if (IsNullLiteral(rightExpr))
        {
            rightIsNull = true;
            if (leftExpr is GDIdentifierExpression leftIdent)
                varName = leftIdent.Identifier?.Sequence;
        }
        // Check if left side is null
        else if (IsNullLiteral(leftExpr))
        {
            leftIsNull = true;
            if (rightExpr is GDIdentifierExpression rightIdent)
                varName = rightIdent.Identifier?.Sequence;
        }

        if (string.IsNullOrEmpty(varName))
            return;

        if (rightIsNull || leftIsNull)
        {
            if (opType == GDDualOperatorType.NotEqual)
            {
                // x != null -> x is guaranteed non-null
                state.MarkNonNull(varName);
            }
            else if (opType == GDDualOperatorType.Equal)
            {
                // x == null -> x is definitely null in this branch
                state.MarkPotentiallyNull(varName);
            }
        }
    }

    /// <summary>
    /// Applies truthiness narrowing.
    /// if x: -> x is truthy (non-null, non-zero, non-empty)
    /// </summary>
    private void ApplyTruthinessNarrowing(GDIdentifierExpression identExpr, GDFlowState state)
    {
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Truthiness check implies non-null
        state.MarkNonNull(varName);
    }

    /// <summary>
    /// Applies narrowing from has_method/has/has_signal checks.
    /// Records the required method/property/signal as a duck-type constraint.
    /// </summary>
    private void ApplyHasMethodNarrowing(GDCallExpression callExpr, GDFlowState state)
    {
        if (callExpr.CallerExpression is not GDMemberOperatorExpression memberOp)
            return;

        var methodName = memberOp.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return;

        // Get the variable being checked
        var callerVar = GetRootVariableName(memberOp.CallerExpression);
        if (string.IsNullOrEmpty(callerVar))
            return;

        // Get the first string argument (the checked member name)
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var firstArg = args[0];
        var checkedName = GetStringLiteralValue(firstArg);
        if (string.IsNullOrEmpty(checkedName))
            return;

        switch (methodName)
        {
            case "has_method":
                state.RequireMethod(callerVar, checkedName);
                state.MarkNonNull(callerVar);
                break;

            case "has":
                state.RequireProperty(callerVar, checkedName);
                state.MarkNonNull(callerVar);
                break;

            case "has_signal":
                state.RequireSignal(callerVar, checkedName);
                state.MarkNonNull(callerVar);
                break;
        }
    }

    /// <summary>
    /// Applies narrowing from is_instance_valid() checks.
    /// is_instance_valid(x) -> x is guaranteed non-null and valid.
    /// </summary>
    private static void ApplyIsInstanceValidNarrowing(GDCallExpression callExpr, GDFlowState state)
    {
        // Handle both is_instance_valid(x) (global function) and obj.is_instance_valid() patterns
        string? funcName = null;
        GDExpression? checkedExpr = null;

        if (callExpr.CallerExpression is GDIdentifierExpression funcIdent)
        {
            // Global function: is_instance_valid(x)
            funcName = funcIdent.Identifier?.Sequence;
            var args = callExpr.Parameters?.ToList();
            if (args != null && args.Count > 0)
                checkedExpr = args[0];
        }
        else if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            // Member method (less common, but handle it): obj.is_instance_valid() - not standard but just in case
            funcName = memberOp.Identifier?.Sequence;
        }

        if (funcName != "is_instance_valid")
            return;

        if (checkedExpr is GDIdentifierExpression checkedIdent)
        {
            var varName = checkedIdent.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
                state.MarkNonNull(varName);
        }
    }

    /// <summary>
    /// Gets the root variable name from an expression chain.
    /// </summary>
    private static string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;

        return null;
    }

    /// <summary>
    /// Checks if an expression is a null literal.
    /// In GDScript, null is represented as GDIdentifierExpression with "null" identifier.
    /// </summary>
    private static bool IsNullLiteral(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
        {
            return identExpr.Identifier?.Sequence == "null";
        }
        return false;
    }

    /// <summary>
    /// Extracts string literal value from an expression.
    /// </summary>
    private static string? GetStringLiteralValue(GDExpression? expr)
    {
        if (expr is GDStringExpression strExpr)
        {
            // Get value from the string node
            var str = strExpr.String?.Sequence;
            if (!string.IsNullOrEmpty(str))
                return str;
        }

        return null;
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
        // Clone the current state to create an immutable snapshot
        // This ensures that subsequent mutations to _currentState don't affect recorded states
        _nodeStates[node] = _currentState.Clone();
        _statementOrder[node] = _currentOrder++;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets the type of a variable at a specific AST node location.
    /// Walks up the AST tree to find the nearest ancestor with a recorded state
    /// that contains the variable. Skips assignment expressions since they record
    /// post-assignment state but we need pre-assignment state when evaluating RHS.
    /// </summary>
    public string? GetTypeAtLocation(string variableName, GDNode location)
    {
        if (string.IsNullOrEmpty(variableName) || location == null)
            return null;

        // Walk up the AST tree to find the nearest ancestor with a recorded state
        var node = location;
        while (node != null)
        {
            // Skip assignment expressions - they contain post-assignment state
            if (node is GDDualOperatorExpression dualOp)
            {
                var opType = dualOp.Operator?.OperatorType;
                if (IsAssignmentOperator(opType ?? GDDualOperatorType.Null))
                {
                    node = node.Parent as GDNode;
                    continue;
                }
            }

            if (_nodeStates.TryGetValue(node, out var state))
            {
                var varType = state.GetVariableType(variableName);
                if (varType != null)
                    return varType.EffectiveType;
            }

            node = node.Parent as GDNode;
        }

        // Fall back to final state (covers cases where variable hasn't been modified yet)
        var finalVarType = _currentState.GetVariableType(variableName);
        return finalVarType?.EffectiveType;
    }

    /// <summary>
    /// Reference equality comparer for GDNode (uses object identity, not value equality).
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<GDNode>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(GDNode? x, GDNode? y) => ReferenceEquals(x, y);
        public int GetHashCode(GDNode obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Finds the statement order for a node by walking up to find a recorded parent.
    /// </summary>
    private int FindStatementOrder(GDNode location)
    {
        var node = location;
        while (node != null)
        {
            if (_statementOrder.TryGetValue(node, out var order))
                return order;
            node = node.Parent as GDNode;
        }
        // If no order found, return max to get the final state
        return int.MaxValue;
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
