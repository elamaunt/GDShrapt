using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly Func<IEnumerable<string>>? _onreadyVariablesProvider;
    private readonly Stack<GDFlowState> _stateStack = new();
    private readonly Stack<List<GDFlowState>> _branchStatesStack = new();
    private readonly Stack<GDExpression?> _matchSubjectStack = new();
    private GDFlowState _currentState;

    // Results: maps AST nodes to their flow state at that point
    private readonly Dictionary<GDNode, GDFlowState> _nodeStates = new();

    // Track statement order for linear flow
    private readonly Dictionary<GDNode, int> _statementOrder = new();
    private int _currentOrder;

    // Container usage profiles for untyped containers (for 'in' operator narrowing)
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();

    // Variables that have been reassigned after declaration
    private readonly HashSet<string> _reassignedVariables = new();

    // Guard against infinite recursion in ResolveTypeWithFallback
    private readonly HashSet<GDExpression> _resolvingExpressions = new();
    private const int MaxResolveDepth = 30;

    // File path for origin tracking
    private string? _filePath;

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
    /// Creates a flow analyzer with expression type and onready variables providers.
    /// </summary>
    public GDFlowAnalyzer(
        GDTypeInferenceEngine? typeEngine,
        Func<GDExpression, string?>? expressionTypeProvider,
        Func<IEnumerable<string>>? onreadyVariablesProvider)
        : this(typeEngine, expressionTypeProvider)
    {
        _onreadyVariablesProvider = onreadyVariablesProvider;
    }

    /// <summary>
    /// Sets the file path for origin tracking.
    /// </summary>
    public void SetFilePath(string? filePath) => _filePath = filePath;

    private GDFlowLocation LocationFromNode(GDNode? node)
    {
        if (node == null)
            return default;
        return new GDFlowLocation(_filePath, node.StartLine, node.StartColumn);
    }

    private GDTypeOrigin CreateOrigin(GDTypeOriginKind kind, GDNode? node, GDTypeOriginConfidence confidence = GDTypeOriginConfidence.Inferred, string? description = null, GDAbstractValue? value = null)
    {
        return new GDTypeOrigin(kind, confidence, LocationFromNode(node), description: description, value: value);
    }

    /// <summary>
    /// Infers the appropriate origin kind based on the expression AST structure.
    /// Call expressions get CallSiteReturn, literals get Literal, etc.
    /// </summary>
    private static GDTypeOriginKind InferOriginKindFromExpression(GDExpression? expr, GDTypeOriginKind fallback)
    {
        if (expr == null)
            return fallback;

        if (expr is GDCallExpression callExpr)
        {
            // Check for member call (method call on object)
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (methodName == "instantiate")
                    return GDTypeOriginKind.InstantiateInjection;
            }

            // Check for preload/load
            var callName = callExpr.CallerExpression is GDIdentifierExpression identExpr
                ? identExpr.Identifier?.Sequence
                : null;
            if (callName == "preload" || callName == "load")
                return GDTypeOriginKind.PreloadInjection;

            return GDTypeOriginKind.CallSiteReturn;
        }

        if (expr is GDGetNodeExpression || expr is GDGetUniqueNodeExpression)
            return GDTypeOriginKind.SceneInjection;

        if (expr is GDNumberExpression || expr is GDStringExpression || expr is GDBoolExpression)
            return GDTypeOriginKind.Literal;

        if (expr is GDMemberOperatorExpression)
            return GDTypeOriginKind.MemberAccess;

        return fallback;
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
    /// Gets the initial flow state at method entry (after parameter declarations, before body).
    /// </summary>
    public GDFlowState? InitialState { get; private set; }

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
                {
                    var semType = GDSemanticType.FromRuntimeTypeName(declType);
                    var origin = CreateOrigin(GDTypeOriginKind.ParameterDeclaration, param, GDTypeOriginConfidence.Exact);
                    _currentState.DeclareVariable(name, semType, null, origin, null);
                }
            }
        }

        // Collect container usage profiles for untyped containers
        CollectContainerProfiles(method);

        // Snapshot the initial state before method body processing
        InitialState = _currentState.Clone();

        // Walk the method body
        method.WalkIn(this);
    }

    /// <summary>
    /// Analyzes a method-like scope (method, setter body, or getter body) for flow-sensitive types.
    /// </summary>
    public void AnalyzeScope(GDNode scope)
    {
        if (scope is GDMethodDeclaration method)
        {
            Analyze(method);
            return;
        }

        if (scope is GDSetAccessorBodyDeclaration setter)
        {
            AnalyzeSetterBody(setter);
            return;
        }

        if (scope is GDGetAccessorBodyDeclaration getter)
        {
            AnalyzeGetterBody(getter);
            return;
        }
    }

    private void AnalyzeSetterBody(GDSetAccessorBodyDeclaration setter)
    {
        // Initialize the setter parameter (e.g., "value" in set(value):)
        var param = setter.Parameter;
        if (param != null)
        {
            var name = param.Identifier?.Sequence;
            var declType = param.Type?.BuildName();

            // If no explicit type on the parameter, infer from the containing variable's type
            if (string.IsNullOrEmpty(declType))
            {
                var varDecl = setter.Parent as GDVariableDeclaration;
                declType = varDecl?.Type?.BuildName();
            }

            if (!string.IsNullOrEmpty(name))
            {
                var semType = GDSemanticType.FromRuntimeTypeName(declType);
                var origin = CreateOrigin(GDTypeOriginKind.ParameterDeclaration, param, GDTypeOriginConfidence.Exact);
                _currentState.DeclareVariable(name, semType, null, origin, null);
            }
        }

        // Collect container usage profiles
        CollectContainerProfiles(setter);

        InitialState = _currentState.Clone();
        setter.WalkIn(this);
    }

    private void AnalyzeGetterBody(GDGetAccessorBodyDeclaration getter)
    {
        // Getters have no parameters
        CollectContainerProfiles(getter);
        InitialState = _currentState.Clone();
        getter.WalkIn(this);
    }

    /// <summary>
    /// Collects container usage profiles from a method-like scope for untyped Array/Dictionary variables.
    /// </summary>
    private void CollectContainerProfiles(GDNode scope)
    {
        var scopes = new GDScopeStack();
        var containerCollector = new GDContainerUsageCollector(scopes, _typeEngine);
        containerCollector.Collect(scope);

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

        var declSemType = varDecl.Type != null
            ? GDSemanticType.FromTypeNode(varDecl.Type)
            : null;
        var initSemType = varDecl.Initializer != null
            ? ResolveSemanticType(varDecl.Initializer)
            : null;

        // When both declared and init types exist, pick the most specific
        if (declSemType != null && initSemType != null && !initSemType.IsVariant)
        {
            var declName = declSemType.DisplayName;
            var initName = initSemType.DisplayName;
            if (declName != initName && IsSubclassOf(declName, initName))
                initSemType = declSemType;
        }

        // Create origins for declaration and initialization
        var declOrigin = declSemType != null
            ? CreateOrigin(GDTypeOriginKind.Declaration, varDecl, GDTypeOriginConfidence.Exact)
            : null;
        var initOriginKind = InferOriginKindFromExpression(varDecl.Initializer, GDTypeOriginKind.Initialization);
        var initOrigin = initSemType != null
            ? CreateOrigin(initOriginKind, varDecl.Initializer as GDNode ?? varDecl, GDTypeOriginConfidence.Inferred)
            : null;

        _currentState.DeclareVariable(name, declSemType, initSemType, declOrigin, initOrigin);
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
                    _reassignedVariables.Add(name);

                    // Ensure class member variables have their declared type registered
                    EnsureClassMemberDeclared(name, identExpr);

                    var rhsSemType = ResolveSemanticType(dualOp.RightExpression);
                    if (rhsSemType != null && !rhsSemType.IsVariant)
                    {
                        var baseKind = opType.Value == GDDualOperatorType.Assignment
                            ? GDTypeOriginKind.Assignment
                            : GDTypeOriginKind.CompoundAssignment;
                        var originKind = opType.Value == GDDualOperatorType.Assignment
                            ? InferOriginKindFromExpression(dualOp.RightExpression, baseKind)
                            : baseKind;
                        var origin = CreateOrigin(originKind, dualOp);

                        var adjustedType = AdjustTypeForDeclaredType(name, rhsSemType.DisplayName);
                        if (adjustedType != rhsSemType.DisplayName)
                            _currentState.SetVariableType(name, GDSemanticType.FromRuntimeTypeName(adjustedType), origin);
                        else
                            _currentState.SetVariableType(name, rhsSemType, origin);
                    }
                }
            }
            else if (dualOp.LeftExpression is GDMemberOperatorExpression memberExpr)
            {
                TryTrackPropertyMutation(dualOp, memberExpr);
            }
            RecordState(dualOp);
        }
    }

    #endregion

    #region Mutation Tracking

    public override void Visit(GDExpressionStatement exprStmt)
    {
        if (exprStmt.Expression is GDCallExpression call)
        {
            TryTrackMutationFromCall(call);
        }
    }

    private void TryTrackMutationFromCall(GDCallExpression call)
    {
        var callName = GetMethodName(call);
        if (callName == null)
            return;

        var receiverName = GetReceiverName(call);
        var isConditional = _branchStatesStack.Count > 0;
        var location = LocationFromNode(call);

        switch (callName)
        {
            case "add_child" or "add_sibling":
            {
                if (receiverName == null)
                    return;

                var flowType = _currentState.GetVariableType(receiverName);
                if (flowType == null)
                    return;

                var childType = InferFirstArgType(call);
                var childSnapshot = InferFirstArgSceneSnapshot(call);

                var mutation = new GDStateMutation(
                    GDStateMutationKind.AddChild,
                    location,
                    isConditional: isConditional,
                    nodeType: childType,
                    addedSceneSnapshot: childSnapshot);

                ApplyMutationToVariable(receiverName, mutation);
                break;
            }

            case "remove_child":
            {
                if (receiverName == null)
                    return;

                var childPath = InferFirstArgStringValue(call);

                var mutation = new GDStateMutation(
                    GDStateMutationKind.RemoveChild,
                    location,
                    isConditional: isConditional,
                    nodePath: childPath);

                ApplyMutationToVariable(receiverName, mutation);
                break;
            }

            case "queue_free" or "free":
            {
                if (receiverName == null)
                    return;

                var mutation = new GDStateMutation(
                    GDStateMutationKind.QueueFree,
                    location,
                    isConditional: isConditional);

                ApplyMutationToVariable(receiverName, mutation);
                break;
            }

            case "show":
            {
                if (receiverName == null)
                    return;

                var mutation = new GDStateMutation(
                    GDStateMutationKind.MethodCall,
                    location,
                    isConditional: isConditional,
                    propertyName: "visible",
                    newValue: new GDLiteralValue(true, GDSemanticType.FromRuntimeTypeName("bool")));

                ApplyMutationToVariable(receiverName, mutation);
                break;
            }

            case "hide":
            {
                if (receiverName == null)
                    return;

                var mutation = new GDStateMutation(
                    GDStateMutationKind.MethodCall,
                    location,
                    isConditional: isConditional,
                    propertyName: "visible",
                    newValue: new GDLiteralValue(false, GDSemanticType.FromRuntimeTypeName("bool")));

                ApplyMutationToVariable(receiverName, mutation);
                break;
            }
        }
    }

    private void TryTrackPropertyMutation(GDDualOperatorExpression dualOp, GDMemberOperatorExpression memberExpr)
    {
        if (dualOp.Operator?.OperatorType != GDDualOperatorType.Assignment)
            return;

        var receiverName = GetIdentifierName(memberExpr.CallerExpression);
        var propertyName = memberExpr.Identifier?.Sequence;

        if (receiverName == null || propertyName == null)
            return;

        var flowType = _currentState.GetVariableType(receiverName);
        if (flowType == null)
            return;

        var isConditional = _branchStatesStack.Count > 0;
        var location = LocationFromNode(dualOp);

        switch (propertyName)
        {
            case "collision_layer":
            {
                if (TryGetIntValue(dualOp.RightExpression, out var layerValue))
                {
                    var mutation = new GDStateMutation(
                        GDStateMutationKind.CollisionLayerChange,
                        location,
                        isConditional: isConditional,
                        newCollisionState: new GDCollisionLayerState(layerValue, 0));
                    ApplyMutationToVariable(receiverName, mutation);
                }
                break;
            }

            case "collision_mask":
            {
                if (TryGetIntValue(dualOp.RightExpression, out var maskValue))
                {
                    var mutation = new GDStateMutation(
                        GDStateMutationKind.CollisionMaskChange,
                        location,
                        isConditional: isConditional,
                        newCollisionState: new GDCollisionLayerState(0, maskValue));
                    ApplyMutationToVariable(receiverName, mutation);
                }
                break;
            }

            case "visible" or "modulate" or "position" or "rotation" or "scale":
            {
                var abstractValue = TryResolveAbstractValue(dualOp.RightExpression);
                var mutation = new GDStateMutation(
                    GDStateMutationKind.PropertySet,
                    location,
                    isConditional: isConditional,
                    propertyName: propertyName,
                    newValue: abstractValue);
                ApplyMutationToVariable(receiverName, mutation);
                break;
            }
        }
    }

    private void ApplyMutationToVariable(string variableName, GDStateMutation mutation)
    {
        var flowType = _currentState.GetVariableType(variableName);
        if (flowType == null)
            return;

        // Walk through origins to find one with an object state and apply mutation
        foreach (var (semType, origins) in flowType.CurrentType.GetAllOrigins())
        {
            foreach (var origin in origins)
            {
                if (origin.ObjectState != null)
                {
                    var newState = origin.ObjectState.WithMutation(mutation);
                    var updatedOrigin = new GDTypeOrigin(
                        origin.Kind,
                        origin.Confidence,
                        origin.Location,
                        upstream: origin.Upstream,
                        description: origin.Description,
                        value: origin.Value,
                        objectState: newState,
                        escapePoint: origin.EscapePoint);

                    flowType.CurrentType.ReplaceOrigin(semType, origin, updatedOrigin);
                    return;
                }
            }
        }
    }

    private static string? GetMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr)
            return memberExpr.Identifier?.Sequence;
        return null;
    }

    private static string? GetReceiverName(GDCallExpression call)
    {
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr)
            return GetIdentifierName(memberExpr.CallerExpression);
        return null;
    }

    private static string? GetIdentifierName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;
        return null;
    }

    private string? InferFirstArgType(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        var argType = ResolveSemanticType(args[0]);
        return argType?.DisplayName;
    }

    private Abstractions.GDSceneSnapshot? InferFirstArgSceneSnapshot(GDCallExpression call)
    {
        // If add_child argument is x.instantiate(), try to get the scene snapshot
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        if (args[0] is GDCallExpression innerCall)
        {
            var innerCallName = GetMethodName(innerCall);
            if (innerCallName == "instantiate" && innerCall.CallerExpression is GDMemberOperatorExpression)
            {
                // Try to extract scene path from preload call
                // This will be resolved through the type injection system
            }
        }
        return null;
    }

    private static string? InferFirstArgStringValue(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        if (args[0] is GDStringExpression strExpr)
            return strExpr.String?.Sequence;

        if (args[0] is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        return null;
    }

    private static bool TryGetIntValue(GDExpression? expr, out int value)
    {
        value = 0;
        if (expr is GDNumberExpression numExpr)
            return int.TryParse(numExpr.Number?.Sequence, out value);
        return false;
    }

    private GDAbstractValue? TryResolveAbstractValue(GDExpression? expr)
    {
        if (expr == null)
            return null;

        if (expr is GDBoolExpression boolExpr)
        {
            var boolVal = boolExpr.Value == true;
            return new GDLiteralValue(boolVal, GDSemanticType.FromRuntimeTypeName("bool"));
        }

        if (expr is GDNumberExpression numExpr)
        {
            var seq = numExpr.Number?.Sequence;
            if (seq != null && int.TryParse(seq, out var intVal))
                return new GDLiteralValue(intVal, GDSemanticType.FromRuntimeTypeName("int"));
            if (seq != null && float.TryParse(seq, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                return new GDLiteralValue(floatVal, GDSemanticType.FromRuntimeTypeName("float"));
        }

        if (expr is GDStringExpression strExpr)
        {
            var strVal = strExpr.String?.Sequence;
            if (strVal != null)
                return new GDLiteralValue(strVal, GDSemanticType.FromRuntimeTypeName("String"));
        }

        return null;
    }

    #endregion

    #region Assignment Helpers

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
    /// For class member variables not yet in the flow state, registers them with their declared type.
    /// This ensures that when a class member like `@export var stats: BattlerStats` is assigned,
    /// the flow analyzer knows its declared type for type adjustment.
    /// </summary>
    private void EnsureClassMemberDeclared(string name, GDIdentifierExpression identExpr)
    {
        if (_currentState.GetVariableType(name) != null)
            return;

        var symbol = _typeEngine?.Scopes?.Lookup(name);
        if (symbol?.TypeName != null)
        {
            var semType = GDSemanticType.FromRuntimeTypeName(symbol.TypeName);
            var origin = CreateOrigin(GDTypeOriginKind.Declaration, identExpr, GDTypeOriginConfidence.Exact);
            _currentState.DeclareVariable(name, semType, null, origin, null);
        }
    }

    /// <summary>
    /// When RHS type is an ancestor of the variable's declared type, preserve the declared type.
    /// e.g., stats: BattlerStats = stats.duplicate() → duplicate() returns Resource,
    /// but BattlerStats extends Resource, so keep BattlerStats.
    /// </summary>
    private string AdjustTypeForDeclaredType(string name, string rhsType)
    {
        var existing = _currentState.GetVariableType(name);
        if (existing?.DeclaredType == null)
            return rhsType;

        var declaredTypeName = existing.DeclaredType.DisplayName;
        if (string.IsNullOrEmpty(declaredTypeName) || declaredTypeName == rhsType)
            return rhsType;

        if (IsSubclassOf(declaredTypeName, rhsType))
            return declaredTypeName;

        return rhsType;
    }

    private bool IsSubclassOf(string childType, string parentType)
    {
        var provider = _typeEngine?.RuntimeProvider;
        if (provider == null)
            return false;

        var current = provider.GetBaseType(childType);
        while (!string.IsNullOrEmpty(current))
        {
            if (current == parentType)
                return true;
            current = provider.GetBaseType(current);
        }
        return false;
    }

    /// <summary>
    /// Resolves semantic type for an expression without string roundtrip.
    /// Returns GDSemanticType directly, preserving GDCallableSemanticType for method refs.
    /// </summary>
    private GDSemanticType? ResolveSemanticType(GDExpression? expr)
    {
        if (expr == null)
            return null;

        if (_resolvingExpressions.Contains(expr))
            return null;

        if (_resolvingExpressions.Count >= MaxResolveDepth)
            return null;

        _resolvingExpressions.Add(expr);
        try
        {
            // Try expression type provider first (string path — legacy)
            var primaryType = _expressionTypeProvider?.Invoke(expr);
            if (!string.IsNullOrEmpty(primaryType) && primaryType != "Variant")
                return GDSemanticType.FromRuntimeTypeName(primaryType);

            // Semantic type engine — direct path, preserves GDCallableSemanticType
            var semanticType = _typeEngine?.InferSemanticType(expr);
            if (semanticType != null && !semanticType.IsVariant)
                return semanticType;

            return primaryType != null ? GDSemanticType.FromRuntimeTypeName(primaryType) : semanticType;
        }
        finally
        {
            _resolvingExpressions.Remove(expr);
        }
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

        // Guard against infinite recursion
        if (_resolvingExpressions.Contains(expr))
            return null;

        if (_resolvingExpressions.Count >= MaxResolveDepth)
            return null;

        _resolvingExpressions.Add(expr);
        try
        {
            // Try expression type provider first (can use union types and richer inference)
            var primaryType = _expressionTypeProvider?.Invoke(expr);
            if (!string.IsNullOrEmpty(primaryType) && primaryType != "Variant")
                return primaryType;

            // Fall back to basic type engine inference
            var fallbackSemanticType = _typeEngine?.InferSemanticType(expr);
            var fallbackType = fallbackSemanticType?.DisplayName;
            if (fallbackSemanticType != null && !fallbackSemanticType.IsVariant)
                return fallbackType;

            // Return Variant as last resort if that's what we got
            return primaryType ?? fallbackType;
        }
        finally
        {
            _resolvingExpressions.Remove(expr);
        }
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
            // Single branch (if without else) — pass null for else branch so that
            // variables only narrowed in the parent (not modified in the if) are
            // preserved via parentType.Clone() instead of going through MergeVariableTypes
            // which strips IsNarrowed.
            _currentState = GDFlowState.MergeBranches(branchStates[0], null, parentState);
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

        // Declare iterator variable with type from annotation or inferred from collection
        var iteratorName = forStmt.Variable?.Sequence;
        if (!string.IsNullOrEmpty(iteratorName))
        {
            // Prefer explicit type annotation (e.g., `for p: TacticsPawn in get_children()`)
            var declaredType = forStmt.VariableType?.BuildName();
            string? elementType;

            if (!string.IsNullOrEmpty(declaredType))
            {
                elementType = declaredType;
            }
            else
            {
                var collectionType = _typeEngine?.InferSemanticType(forStmt.Collection)?.DisplayName;
                elementType = GDLoopFlowHelper.InferIteratorElementType(collectionType);
            }

            context.IteratorName = iteratorName;
            context.IteratorType = elementType;
        }

        _loopContextStack.Push(context);

        // Create initial loop body state
        var loopState = preLoopState.CreateChild();
        if (!string.IsNullOrEmpty(context.IteratorName))
        {
            var iterSemType = GDSemanticType.FromRuntimeTypeName(context.IteratorType);
            var iterOrigin = CreateOrigin(GDTypeOriginKind.ForLoopIterator, forStmt);
            loopState.DeclareVariable(context.IteratorName, null, iterSemType, null, iterOrigin);
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
            _currentState = GDLoopFlowHelper.ComputeLoopFixedPoint(
                context.PreLoopState,
                _currentState,
                context.IteratorName,
                context.IteratorType);
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
            _currentState = GDLoopFlowHelper.ComputeLoopFixedPoint(
                context.PreLoopState,
                _currentState,
                null,
                null);
        }
        else
        {
            // Fallback: simple merge
            _currentState = GDFlowState.MergeBranches(_currentState, parentState, parentState);
        }

        RecordState(whileStmt);
    }

    #endregion

    #region Match Statements

    public override void Visit(GDMatchStatement matchStmt)
    {
        // Save current state before match
        _stateStack.Push(_currentState);
        // Create list to collect case end states
        _branchStatesStack.Push(new List<GDFlowState>());
        // Track match subject for binding type inference
        _matchSubjectStack.Push(matchStmt.Value);
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
        if (_matchSubjectStack.Count > 0)
            _matchSubjectStack.Pop();

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
    /// Declares binding variables from match case patterns with type inference from match subject.
    /// </summary>
    private void DeclareMatchBindings(GDMatchCaseDeclaration matchCase, GDFlowState state)
    {
        if (matchCase.Conditions == null)
            return;

        // Infer subject type from match statement
        string? subjectType = null;
        if (_matchSubjectStack.Count > 0 && _matchSubjectStack.Peek() != null)
            subjectType = _typeEngine?.InferSemanticType(_matchSubjectStack.Peek()!)?.DisplayName;

        // Extract guard condition narrowing
        var (guardVar, guardType) = GDMatchPatternHelper.ExtractGuardNarrowing(matchCase);

        foreach (var condition in matchCase.Conditions)
        {
            DeclareBindingsFromPattern(condition, state, subjectType, guardVar, guardType);
        }
    }

    /// <summary>
    /// Recursively declares binding variables from a pattern expression.
    /// </summary>
    private void DeclareBindingsFromPattern(GDExpression? pattern, GDFlowState state, string? subjectType, string? guardVar, string? guardType)
    {
        if (pattern == null)
            return;

        // Handle var binding: var x
        if (pattern is GDMatchCaseVariableExpression varExpr)
        {
            var name = varExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name))
            {
                // Apply guard narrowing if applicable, otherwise use subject type
                var bindingType = (name == guardVar && !string.IsNullOrEmpty(guardType))
                    ? guardType
                    : subjectType;

                var semType = GDSemanticType.FromRuntimeTypeName(bindingType);
                var origin = CreateOrigin(GDTypeOriginKind.MatchPatternNarrowing, varExpr);
                state.DeclareVariable(name, null, semType, null, origin);
            }
            return;
        }

        if (pattern is GDArrayInitializerExpression arrayExpr)
        {
            var elementType = GDLoopFlowHelper.InferIteratorElementType(subjectType);
            foreach (var element in arrayExpr.Values ?? Enumerable.Empty<GDExpression>())
            {
                DeclareBindingsFromPattern(element, state, elementType, guardVar, guardType);
            }
            return;
        }

        // Handle dictionary patterns: {key: value}
        if (pattern is GDDictionaryInitializerExpression dictExpr)
        {
            var (_, valueType) = GDGenericTypeHelper.ExtractDictionaryTypes(subjectType);
            var dictValueType = valueType ?? GDWellKnownTypes.Variant;
            foreach (var kvp in dictExpr.KeyValues ?? Enumerable.Empty<GDDictionaryKeyValueDeclaration>())
            {
                DeclareBindingsFromPattern(kvp.Value, state, dictValueType, guardVar, guardType);
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
                    var semType = GDSemanticType.FromRuntimeTypeName(paramType);
                    var origin = CreateOrigin(GDTypeOriginKind.ParameterDeclaration, param, GDTypeOriginConfidence.Exact);
                    lambdaState.DeclareVariable(paramName, semType, null, origin, null);
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
                var typeName = GDLiteralTypeResolver.GetTypeNameFromExpression(dualOp.RightExpression);

                if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(typeName))
                {
                    var narrowedType = GDSemanticType.FromRuntimeTypeName(typeName);
                    var constraint = new GDNarrowingConstraint(GDNarrowingKind.IsCheck, narrowedType, LocationFromNode(condition));
                    state.NarrowType(varName, narrowedType, constraint);
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
                GDFlowNarrowingHelper.ApplyNullComparisonNarrowing(eqOp, state);

                // Also handle: x == literal (narrows to literal's type)
                if (opType == GDDualOperatorType.Equal)
                {
                    GDFlowNarrowingHelper.ApplyLiteralComparisonNarrowing(eqOp, state);
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
            GDFlowNarrowingHelper.ApplyTruthinessNarrowing(truthyIdent, state);
        }

        // Handle: has_method(), has(), has_signal(), is_instance_valid(), is_node_ready()
        if (condition is GDCallExpression callExpr)
        {
            ApplyHasMethodNarrowing(callExpr, state);
            GDFlowNarrowingHelper.ApplyIsInstanceValidNarrowing(callExpr, state);
            ApplyIsNodeReadyNarrowing(callExpr, state);
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
            GDLiteralTypeResolver.IsLiteralExpression(eqOp.RightExpression))
        {
            varName = leftIdent.Identifier?.Sequence;
            literalType = GDLiteralTypeResolver.GetLiteralType(eqOp.RightExpression);
        }
        // literal == variable
        else if (eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                 GDLiteralTypeResolver.IsLiteralExpression(eqOp.LeftExpression))
        {
            varName = rightIdent.Identifier?.Sequence;
            literalType = GDLiteralTypeResolver.GetLiteralType(eqOp.LeftExpression);
        }

        if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(literalType))
        {
            state.NarrowType(varName, GDSemanticType.FromRuntimeTypeName(literalType));
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
            var intersection = currentVarType.CurrentType.IntersectWithType(GDSemanticType.FromRuntimeTypeName(elementType!), _typeEngine?.RuntimeProvider);

            if (!intersection.IsEmpty)
            {
                // Apply intersection result
                state.NarrowToIntersection(varName, intersection);
            }
            else
            {
                // Empty intersection means incompatible types, but GDScript is dynamic
                // Still narrow to container element type (runtime might succeed)
                state.NarrowType(varName, GDSemanticType.FromRuntimeTypeName(elementType));
            }
        }
        else
        {
            // No existing union type - use container element type directly
            state.NarrowType(varName, GDSemanticType.FromRuntimeTypeName(elementType));
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
            return GDWellKnownTypes.Strings.String;
        }

        // Handle range() calls: range(1, 10) -> int
        if (containerExpr is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDIdentifierExpression callIdent &&
                callIdent.Identifier?.Sequence == GDWellKnownFunctions.Range)
            {
                return GDWellKnownTypes.Numeric.Int;
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
                var typeName = varType?.EffectiveType.DisplayName;
                if (!string.IsNullOrEmpty(typeName) && typeName != "Array" && typeName != "Dictionary")
                {
                    var extractedType = GDFlowNarrowingHelper.ExtractElementTypeFromTypeName(typeName);
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
                        var keyType = inferredElementType.KeyUnionType.EffectiveType.DisplayName;
                        if (!string.IsNullOrEmpty(keyType) && keyType != "Variant")
                            return keyType;
                    }
                    // For Array, element type is used
                    if (!inferredElementType.ElementUnionType.IsEmpty)
                    {
                        var elementType = inferredElementType.ElementUnionType.EffectiveType.DisplayName;
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
            return GDFlowNarrowingHelper.ExtractElementTypeFromTypeName(inferredType);
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
            var elementType = GDLiteralTypeResolver.GetLiteralType(value) ?? ResolveTypeWithFallback(value);
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
                    commonType = (commonType == GDWellKnownTypes.Numeric.Float || elementType == GDWellKnownTypes.Numeric.Float) ? GDWellKnownTypes.Numeric.Float : GDWellKnownTypes.Numeric.Int;
                }
                else
                {
                    // Truly mixed types - cannot narrow
                    return GDWellKnownTypes.Variant;
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
            var keyType = GDLiteralTypeResolver.GetLiteralType(kv.Key) ?? ResolveTypeWithFallback(kv.Key);
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
    private bool IsNumericType(string? type) =>
        !string.IsNullOrEmpty(type) && (_typeEngine?.RuntimeProvider?.IsNumericType(type) ?? GDWellKnownTypes.IsNumericType(type));

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
        var callerVar = GDFlowNarrowingHelper.GetRootVariableName(memberOp.CallerExpression);
        if (string.IsNullOrEmpty(callerVar))
            return;

        // Get the first string argument (the checked member name)
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var firstArg = args[0];
        var checkedName = GDLiteralTypeResolver.GetStringLiteralValue(firstArg);
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
    /// Applies narrowing from is_node_ready() checks.
    /// is_node_ready() -> all @onready and _ready()-initialized variables are guaranteed non-null.
    /// </summary>
    private void ApplyIsNodeReadyNarrowing(GDCallExpression callExpr, GDFlowState state)
    {
        // Handle is_node_ready() (Node method, no arguments)
        string? funcName = null;

        if (callExpr.CallerExpression is GDIdentifierExpression funcIdent)
        {
            funcName = funcIdent.Identifier?.Sequence;
        }
        else if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            // self.is_node_ready() pattern
            funcName = memberOp.Identifier?.Sequence;
        }

        if (funcName != "is_node_ready")
            return;

        // Mark all @onready variables as non-null
        if (_onreadyVariablesProvider != null)
        {
            foreach (var varName in _onreadyVariablesProvider())
            {
                state.MarkNonNull(varName);
            }
        }
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
    /// Checks if a variable has been reassigned (any assignment operator) after its declaration.
    /// </summary>
    public bool HasReassignment(string varName) => _reassignedVariables.Contains(varName);

    /// <summary>
    /// Gets the type of a variable at a specific AST node location.
    /// Walks up the AST tree to find the nearest ancestor with a recorded state
    /// that contains the variable. Skips assignment expressions since they record
    /// post-assignment state but we need pre-assignment state when evaluating RHS.
    /// </summary>
    public GDSemanticType? GetTypeAtLocation(string variableName, GDNode location)
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
