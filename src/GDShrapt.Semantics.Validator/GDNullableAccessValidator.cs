using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Context for nullable access validation - extracted from the access node.
/// Contains all information needed to analyze and report nullable access diagnostics.
/// </summary>
internal sealed class GDNullableAccessContext
{
    public required string VarName { get; init; }
    public required GDNode AccessNode { get; init; }
    public required GDExpression CallerExpr { get; init; }
    public required GDDiagnosticCode Code { get; init; }

    // Variable classification from semantic model
    public bool IsOnreadyVariable { get; init; }
    public bool IsReadyInitializedVariable { get; init; }
    public bool HasConditionalReadyInit { get; init; }
}

/// <summary>
/// Result of nullability safety analysis.
/// Determines what kind of diagnostic (if any) should be reported.
/// </summary>
internal enum GDNullabilitySafetyResult
{
    Safe,              // No warning needed
    UnsafeOnready,     // @onready variable in unsafe context
    UnsafeReadyInit,   // _ready()-initialized variable in unsafe context
    UnsafeConditional, // Conditional initialization in _ready()
    UnsafeNullable     // Regular potentially-null variable
}

/// <summary>
/// Validates access on potentially-null variables.
/// Reports warnings for:
/// - Property access on potentially null variables (x.property where x may be null)
/// - Method calls on potentially null variables (x.method() where x may be null)
/// - Indexer access on potentially null variables (x[i] where x may be null)
/// </summary>
public class GDNullableAccessValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDDiagnosticSeverity _severity;
    private readonly GDNullableStrictnessMode _strictness;
    private readonly bool _warnOnDictionaryIndexer;
    private readonly bool _warnOnUntypedParameters;

    /// <summary>
    /// Tracks member expressions validated as part of method calls.
    /// Used to prevent duplicate GD7005/GD7007 diagnostics when a member expression
    /// is both a standalone access and the caller of a method call.
    /// </summary>
    private readonly HashSet<GDMemberOperatorExpression> _validatedMemberExpressions = new();

    public GDNullableAccessValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Warning)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = severity;
        _strictness = GDNullableStrictnessMode.Strict;
        _warnOnDictionaryIndexer = true;
        _warnOnUntypedParameters = true;
    }

    public GDNullableAccessValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = options.NullableAccessSeverity;
        _strictness = options.NullableStrictness;
        _warnOnDictionaryIndexer = options.WarnOnDictionaryIndexer;
        _warnOnUntypedParameters = options.WarnOnUntypedParameters;
    }

    public void Validate(GDNode? node)
    {
        _validatedMemberExpressions.Clear();
        node?.WalkIn(this);
    }

    public override void Visit(GDMemberOperatorExpression memberAccess)
    {
        // Skip if already validated as part of a method call
        // In that case, Visit(GDCallExpression) already reported GD7007 instead
        if (_validatedMemberExpressions.Contains(memberAccess))
            return;

        ValidateNullAccess(memberAccess, memberAccess.CallerExpression, GDDiagnosticCode.PotentiallyNullAccess);
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Only validate member calls (obj.method())
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            // Mark this member expression as validated to prevent duplicate GD7005/GD7007
            _validatedMemberExpressions.Add(memberExpr);
            ValidateNullAccess(callExpr, memberExpr.CallerExpression, GDDiagnosticCode.PotentiallyNullMethodCall);
        }
    }

    public override void Visit(GDIndexerExpression indexerExpr)
    {
        ValidateNullAccess(indexerExpr, indexerExpr.CallerExpression, GDDiagnosticCode.PotentiallyNullIndexer);
    }

    private void ValidateNullAccess(GDNode accessNode, GDExpression? callerExpr, GDDiagnosticCode code)
    {
        // Step 1: Extract and validate context
        var context = TryCreateAccessContext(accessNode, callerExpr, code);
        if (context == null)
            return; // Early exit conditions met

        // Step 2: Analyze nullability safety
        var safetyResult = AnalyzeNullabilitySafety(context);
        if (safetyResult == GDNullabilitySafetyResult.Safe)
            return;

        // Step 3: Build and report diagnostic
        ReportNullableDiagnostic(context, safetyResult);
    }

    /// <summary>
    /// Attempts to create a validation context. Returns null if validation should be skipped.
    /// Handles all early exit conditions: strictness mode, null checks, guards, etc.
    /// </summary>
    private GDNullableAccessContext? TryCreateAccessContext(GDNode accessNode, GDExpression? callerExpr, GDDiagnosticCode code)
    {
        // Check strictness mode - if Off, skip all checks
        if (_strictness == GDNullableStrictnessMode.Off)
            return null;

        if (callerExpr == null)
            return null;

        // Get the root variable name from the caller expression
        var varName = GetRootVariableName(callerExpr);
        if (string.IsNullOrEmpty(varName))
            return null;

        // Skip 'self' - it's never null
        if (varName == "self")
            return null;

        // Skip 'super' - it's a special keyword for parent class method calls, never null
        if (varName == "super")
            return null;

        // Skip Signal type - signals are never null, they're built-in class properties
        var exprTypeInfo = _semanticModel.TypeSystem.GetType(callerExpr);
        if (exprTypeInfo.DisplayName == "Signal")
            return null;

        // Check if guarded by null check in 'and' expression
        if (GDNullGuardDetector.IsGuardedByNullCheck(accessNode, varName))
            return null;

        // Check if protected by guard clause with early return
        if (GDNullGuardDetector.IsProtectedByGuardClause(accessNode, varName))
            return null;

        // Skip untyped parameters based on options
        if (!_warnOnUntypedParameters && IsUntypedParameter(varName, accessNode))
            return null;

        // Skip dictionary indexer results based on options
        if (!_warnOnDictionaryIndexer && IsFromDictionaryIndexer(callerExpr))
            return null;

        // Relaxed mode: only warn on explicitly nullable variables
        if (_strictness == GDNullableStrictnessMode.Relaxed && !IsExplicitlyNullable(varName, accessNode))
            return null;

        // Build the context with semantic model information
        return new GDNullableAccessContext
        {
            VarName = varName,
            AccessNode = accessNode,
            CallerExpr = callerExpr,
            Code = code,
            IsOnreadyVariable = _semanticModel.IsOnreadyVariable(varName),
            IsReadyInitializedVariable = _semanticModel.IsReadyInitializedVariable(varName),
            HasConditionalReadyInit = _semanticModel.HasConditionalReadyInitialization(varName)
        };
    }

    /// <summary>
    /// Analyzes the nullability safety of the access and returns the result.
    /// </summary>
    private GDNullabilitySafetyResult AnalyzeNullabilitySafety(GDNullableAccessContext context)
    {
        // Handle @onready and _ready()-initialized variables
        if (context.IsOnreadyVariable || context.IsReadyInitializedVariable)
        {
            return AnalyzeOnreadySafety(context);
        }

        // Regular variable - check if potentially null
        if (_semanticModel.IsVariablePotentiallyNull(context.VarName, context.AccessNode))
        {
            return GDNullabilitySafetyResult.UnsafeNullable;
        }

        return GDNullabilitySafetyResult.Safe;
    }

    /// <summary>
    /// Analyzes safety for @onready and _ready()-initialized variables.
    /// </summary>
    private GDNullabilitySafetyResult AnalyzeOnreadySafety(GDNullableAccessContext context)
    {
        // Conditional initialization - needs null check even in lifecycle methods
        if (context.HasConditionalReadyInit)
        {
            if (!_semanticModel.IsVariablePotentiallyNull(context.VarName, context.AccessNode))
                return GDNullabilitySafetyResult.Safe;

            return GDNullabilitySafetyResult.UnsafeConditional;
        }

        // Check if in lifecycle method that runs after _ready()
        if (IsInLifecycleMethodAfterReady(context.AccessNode))
            return GDNullabilitySafetyResult.Safe;

        // Check if protected by is_node_ready() guard
        if (IsInIsNodeReadyGuard(context.AccessNode))
            return GDNullabilitySafetyResult.Safe;

        // Check if method is safe via cross-method call-site analysis
        if (IsMethodSafeForOnready(context.AccessNode))
            return GDNullabilitySafetyResult.Safe;

        // Not safe - return appropriate result type
        return context.IsOnreadyVariable
            ? GDNullabilitySafetyResult.UnsafeOnready
            : GDNullabilitySafetyResult.UnsafeReadyInit;
    }

    /// <summary>
    /// Reports the appropriate diagnostic based on the safety result.
    /// </summary>
    private void ReportNullableDiagnostic(GDNullableAccessContext context, GDNullabilitySafetyResult safetyResult)
    {
        var memberName = GetAccessedMemberName(context.AccessNode);
        var message = BuildNullableWarningMessage(context.VarName, memberName, safetyResult);
        ReportDiagnosticWithSeverity(context.Code, message, context.AccessNode, GetEffectiveSeverity());
    }

    /// <summary>
    /// Builds the warning message based on the safety result type.
    /// </summary>
    private static string BuildNullableWarningMessage(string varName, string? memberName, GDNullabilitySafetyResult safetyResult)
    {
        var baseMessage = safetyResult switch
        {
            GDNullabilitySafetyResult.UnsafeOnready =>
                $"Variable '{varName}' is @onready - _ready() may not have been called. Use 'if is_node_ready():' guard",

            GDNullabilitySafetyResult.UnsafeReadyInit =>
                $"Variable '{varName}' is initialized in _ready() - may be accessed before _ready() is called. Use 'if is_node_ready():' guard or a null check",

            GDNullabilitySafetyResult.UnsafeConditional =>
                $"Variable '{varName}' may not be initialized (conditional initialization in _ready())",

            GDNullabilitySafetyResult.UnsafeNullable =>
                $"Variable '{varName}' may be null",

            _ => $"Variable '{varName}' may be null"
        };

        return BuildNullableMessage(baseMessage, memberName);
    }

    /// <summary>
    /// Checks if the variable is an untyped function parameter.
    /// </summary>
    private bool IsUntypedParameter(string varName, GDNode atLocation)
    {
        var method = GDNullGuardDetector.FindContainingMethod(atLocation);
        if (method?.Parameters == null)
            return false;

        foreach (var param in method.Parameters)
        {
            if (param.Identifier?.Sequence == varName)
                return param.Type == null;
        }
        return false;
    }

    /// <summary>
    /// Checks if the expression comes from a dictionary indexer access.
    /// </summary>
    private static bool IsFromDictionaryIndexer(GDExpression? expr)
    {
        return expr is GDIndexerExpression;
    }

    /// <summary>
    /// Checks if the variable is explicitly initialized to null (var x = null).
    /// Used for Relaxed mode.
    /// </summary>
    private bool IsExplicitlyNullable(string varName, GDNode atLocation)
    {
        // Check if the variable was assigned null
        return _semanticModel.IsVariablePotentiallyNull(varName, atLocation);
    }

    private void ReportDiagnosticWithSeverity(GDDiagnosticCode code, string message, GDNode node, GDDiagnosticSeverity severity)
    {
        switch (severity)
        {
            case GDDiagnosticSeverity.Error:
                ReportError(code, message, node);
                break;
            case GDDiagnosticSeverity.Warning:
                ReportWarning(code, message, node);
                break;
            case GDDiagnosticSeverity.Hint:
                ReportHint(code, message, node);
                break;
        }
    }

    /// <summary>
    /// Builds a nullable access warning message with optional member name suffix.
    /// </summary>
    private static string BuildNullableMessage(string baseMessage, string? memberName)
    {
        return string.IsNullOrEmpty(memberName)
            ? baseMessage
            : $"{baseMessage} when accessing '{memberName}'";
    }

    /// <summary>
    /// Gets the effective severity based on strictness mode.
    /// </summary>
    private GDDiagnosticSeverity GetEffectiveSeverity()
    {
        return _strictness == GDNullableStrictnessMode.Error
            ? GDDiagnosticSeverity.Error
            : _severity;
    }

    /// <summary>
    /// Checks if the access node is inside a lifecycle method that runs after _ready().
    /// Methods like _process, _physics_process, _input, _draw are guaranteed to run after _ready.
    /// </summary>
    private static bool IsInLifecycleMethodAfterReady(GDNode accessNode)
    {
        var method = GDNullGuardDetector.FindContainingMethod(accessNode);
        if (method == null)
            return false;

        return method.IsLifecycleMethodAfterReady();
    }

    /// <summary>
    /// Checks if the access node is inside an is_node_ready() guard.
    /// Pattern: if is_node_ready(): var.method()
    /// </summary>
    private static bool IsInIsNodeReadyGuard(GDNode accessNode)
    {
        // Find containing if/elif branch
        var current = accessNode?.Parent as GDNode;
        while (current != null)
        {
            if (current is GDIfBranch ifBranch)
            {
                // Check that accessNode is in body, not in condition
                if (ifBranch.Condition != null && !GDNullGuardDetector.IsDescendantOf(accessNode, ifBranch.Condition))
                {
                    if (IsNodeReadyGuard(ifBranch.Condition))
                        return true;
                }
            }
            else if (current is GDElifBranch elifBranch)
            {
                if (elifBranch.Condition != null && !GDNullGuardDetector.IsDescendantOf(accessNode, elifBranch.Condition))
                {
                    if (IsNodeReadyGuard(elifBranch.Condition))
                        return true;
                }
            }
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Checks if the condition is or contains is_node_ready() call.
    /// </summary>
    private static bool IsNodeReadyGuard(GDExpression? condition)
    {
        if (condition == null)
            return false;

        // Direct is_node_ready() or self.is_node_ready() call
        if (condition is GDCallExpression callExpr)
        {
            // is_node_ready()
            if (callExpr.CallerExpression is GDIdentifierExpression funcIdent &&
                funcIdent.Identifier?.Sequence == "is_node_ready")
            {
                return true;
            }

            // self.is_node_ready()
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberExpr &&
                memberExpr.Identifier?.Sequence == "is_node_ready")
            {
                return true;
            }
        }

        // is_node_ready() in 'and' expression: is_node_ready() and ...
        if (condition is GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;
            if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
            {
                if (IsNodeReadyGuard(dualOp.LeftExpression) ||
                    IsNodeReadyGuard(dualOp.RightExpression))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the method is safe for @onready variables via cross-method analysis.
    /// A method is safe if all its callers are lifecycle methods or other safe methods.
    /// </summary>
    private bool IsMethodSafeForOnready(GDNode accessNode)
    {
        var method = GDNullGuardDetector.FindContainingMethod(accessNode);
        if (method == null)
            return false;

        var methodName = method.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return false;

        // Use the cross-method analysis API
        var safety = _semanticModel.GetMethodOnreadySafety(methodName);
        return safety == GDMethodOnreadySafety.Safe;
    }

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

    private static string? GetAccessedMemberName(GDNode node)
    {
        return node switch
        {
            GDMemberOperatorExpression memberExpr => memberExpr.Identifier?.Sequence,
            GDCallExpression callExpr when callExpr.CallerExpression is GDMemberOperatorExpression memberExpr
                => memberExpr.Identifier?.Sequence,
            GDIndexerExpression => "[...]",
            _ => null
        };
    }
}
