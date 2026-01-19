using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates member access (properties and methods) using type inference.
/// Reports errors for:
/// - Accessing properties not found on known types
/// - Calling methods not found on known types
/// - Unguarded member access on untyped variables (configurable severity)
/// </summary>
public class GDMemberAccessValidator : GDValidationVisitor
{
    private readonly IGDMemberAccessAnalyzer _analyzer;
    private readonly IGDRuntimeProvider _runtimeProvider;
    private readonly GDDiagnosticSeverity _untypedSeverity;

    // Methods on Object that are always available
    private static readonly HashSet<string> ObjectMethods = new HashSet<string>
    {
        "has_method", "has_signal", "has", "get", "set", "call", "callv",
        "connect", "disconnect", "emit_signal", "is_connected",
        "get_class", "is_class", "get_property_list", "get_method_list",
        "notification", "to_string", "free", "queue_free",
        "get_script", "set_script", "get_instance_id", "is_instance_valid"
    };

    // Properties on Object that are always available
    private static readonly HashSet<string> ObjectProperties = new HashSet<string>
    {
        "script"
    };

    public GDMemberAccessValidator(
        GDValidationContext context,
        IGDMemberAccessAnalyzer analyzer,
        GDDiagnosticSeverity untypedSeverity = GDDiagnosticSeverity.Warning)
        : base(context)
    {
        _analyzer = analyzer;
        _runtimeProvider = context.RuntimeProvider;
        _untypedSeverity = untypedSeverity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDCallExpression callExpression)
    {
        // Only validate member calls (obj.method())
        if (callExpression.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            ValidateMethodCall(memberExpr, callExpression);
        }
    }

    public override void Visit(GDMemberOperatorExpression memberAccess)
    {
        // Skip if this is part of a call expression (handled by Visit(GDCallExpression))
        if (memberAccess.Parent is GDCallExpression)
            return;

        ValidatePropertyAccess(memberAccess);
    }

    private void ValidatePropertyAccess(GDMemberOperatorExpression memberAccess)
    {
        var memberName = memberAccess.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            return;

        var callerExpr = memberAccess.CallerExpression;
        if (callerExpr == null)
            return;

        // Skip self.property
        if (callerExpr is GDIdentifierExpression idExpr && idExpr.Identifier?.Sequence == "self")
            return;

        // Skip GlobalClass.property (static access)
        if (callerExpr is GDIdentifierExpression classExpr)
        {
            var name = classExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && _runtimeProvider.GetGlobalClass(name) != null)
                return;
        }

        // Skip Object built-in properties
        if (ObjectProperties.Contains(memberName))
            return;

        var confidence = _analyzer.GetMemberAccessConfidence(memberAccess);
        var callerType = _analyzer.GetExpressionType(callerExpr);

        switch (confidence)
        {
            case GDReferenceConfidence.Strict:
                ValidatePropertyOnKnownType(memberAccess, callerType, memberName);
                break;

            case GDReferenceConfidence.Potential:
                // Duck typed or narrowed by type guard - OK
                break;

            case GDReferenceConfidence.NameMatch:
                ReportUnguardedPropertyAccess(memberAccess, memberName);
                break;
        }
    }

    private void ValidateMethodCall(GDMemberOperatorExpression memberExpr, GDCallExpression call)
    {
        var methodName = memberExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return;

        // Skip .new() constructor - it's a special built-in, not a regular method
        if (methodName == "new")
            return;

        var callerExpr = memberExpr.CallerExpression;
        if (callerExpr == null)
            return;

        // Skip self.method()
        if (callerExpr is GDIdentifierExpression idExpr && idExpr.Identifier?.Sequence == "self")
            return;

        // Skip GlobalClass.method() (static call)
        if (callerExpr is GDIdentifierExpression classExpr)
        {
            var name = classExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && _runtimeProvider.GetGlobalClass(name) != null)
                return;
        }

        // Skip Object built-in methods
        if (ObjectMethods.Contains(methodName))
            return;

        var confidence = _analyzer.GetMemberAccessConfidence(memberExpr);
        var callerType = _analyzer.GetExpressionType(callerExpr);

        switch (confidence)
        {
            case GDReferenceConfidence.Strict:
                ValidateMethodOnKnownType(memberExpr, call, callerType, methodName);
                break;

            case GDReferenceConfidence.Potential:
                // Duck typed or narrowed by type guard - OK
                break;

            case GDReferenceConfidence.NameMatch:
                ReportUnguardedMethodCall(call, methodName);
                break;
        }
    }

    private void ValidatePropertyOnKnownType(GDMemberOperatorExpression memberAccess, string? typeName, string memberName)
    {
        if (string.IsNullOrEmpty(typeName))
            return;

        var memberInfo = FindMember(typeName, memberName);
        if (memberInfo == null)
        {
            ReportWarning(
                GDDiagnosticCode.PropertyNotFound,
                $"Property '{memberName}' not found on type '{typeName}'",
                memberAccess);
        }
    }

    private void ValidateMethodOnKnownType(GDMemberOperatorExpression memberExpr, GDCallExpression call, string? typeName, string methodName)
    {
        if (string.IsNullOrEmpty(typeName))
            return;

        var memberInfo = FindMember(typeName, methodName);
        if (memberInfo == null)
        {
            ReportWarning(
                GDDiagnosticCode.MethodNotFound,
                $"Method '{methodName}' not found on type '{typeName}'",
                call);
            return;
        }

        if (memberInfo.Kind != GDRuntimeMemberKind.Method)
        {
            ReportError(
                GDDiagnosticCode.NotCallable,
                $"'{methodName}' on type '{typeName}' is not a method",
                call);
            return;
        }

        // Validate argument count
        var argCount = call.Parameters?.Count ?? 0;
        if (argCount < memberInfo.MinArgs)
        {
            ReportError(
                GDDiagnosticCode.WrongArgumentCount,
                $"'{typeName}.{methodName}' requires at least {memberInfo.MinArgs} argument(s), got {argCount}",
                call);
        }
        else if (!memberInfo.IsVarArgs && memberInfo.MaxArgs >= 0 && argCount > memberInfo.MaxArgs)
        {
            ReportError(
                GDDiagnosticCode.WrongArgumentCount,
                $"'{typeName}.{methodName}' takes at most {memberInfo.MaxArgs} argument(s), got {argCount}",
                call);
        }
    }

    private void ReportUnguardedPropertyAccess(GDMemberOperatorExpression memberAccess, string memberName)
    {
        var varName = GetRootVariableName(memberAccess.CallerExpression);
        var message = string.IsNullOrEmpty(varName)
            ? $"Accessing property '{memberName}' on untyped expression without type guard"
            : $"Accessing property '{memberName}' on untyped variable '{varName}' without type guard";

        ReportDiagnostic(_untypedSeverity, GDDiagnosticCode.UnguardedPropertyAccess, message, memberAccess);
    }

    private void ReportUnguardedMethodCall(GDCallExpression call, string methodName)
    {
        var memberExpr = call.CallerExpression as GDMemberOperatorExpression;
        var varName = memberExpr != null ? GetRootVariableName(memberExpr.CallerExpression) : null;
        var message = string.IsNullOrEmpty(varName)
            ? $"Calling method '{methodName}' on untyped expression without type guard"
            : $"Calling method '{methodName}' on untyped variable '{varName}' without type guard";

        ReportDiagnostic(_untypedSeverity, GDDiagnosticCode.UnguardedMethodCall, message, call);
    }

    private void ReportDiagnostic(GDDiagnosticSeverity severity, GDDiagnosticCode code, string message, GDNode node)
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

    private GDRuntimeMemberInfo? FindMember(string typeName, string memberName)
    {
        // Check direct member
        var memberInfo = _runtimeProvider.GetMember(typeName, memberName);
        if (memberInfo != null)
            return memberInfo;

        // Check inherited members
        var baseType = _runtimeProvider.GetBaseType(typeName);
        while (!string.IsNullOrEmpty(baseType))
        {
            memberInfo = _runtimeProvider.GetMember(baseType, memberName);
            if (memberInfo != null)
                return memberInfo;

            baseType = _runtimeProvider.GetBaseType(baseType);
        }

        return null;
    }

    private string? GetRootVariableName(GDExpression? expr)
    {
        return expr switch
        {
            GDIdentifierExpression idExpr => idExpr.Identifier?.Sequence,
            GDMemberOperatorExpression memberExpr => GetRootVariableName(memberExpr.CallerExpression),
            GDIndexerExpression indexerExpr => GetRootVariableName(indexerExpr.CallerExpression),
            _ => null
        };
    }
}
