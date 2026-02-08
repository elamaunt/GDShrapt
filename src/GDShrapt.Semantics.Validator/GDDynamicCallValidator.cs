using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates dynamic method and property access (call(), get(), set()) with known string arguments.
/// Reports warnings for:
/// - call("unknown_method") on a type known not to have that method
/// - get("unknown_property") on a type known not to have that property
/// - set("unknown_property", value) on a type known not to have that property
/// </summary>
public class GDDynamicCallValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDDiagnosticSeverity _severity;

    public GDDynamicCallValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Warning)
        : base(context)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = context.RuntimeProvider;
        _severity = severity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Only validate member calls (obj.method())
        if (callExpr.CallerExpression is not GDMemberOperatorExpression memberExpr)
            return;

        var methodName = memberExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return;

        switch (methodName)
        {
            case "call":
            case "callv":
                ValidateCallMethod(callExpr, memberExpr);
                break;
            case "get":
                ValidateGetMethod(callExpr, memberExpr);
                break;
            case "set":
                ValidateSetMethod(callExpr, memberExpr);
                break;
        }
    }

    /// <summary>
    /// Validates call()/callv() dynamic method calls.
    /// </summary>
    private void ValidateCallMethod(GDCallExpression callExpr, GDMemberOperatorExpression memberExpr)
    {
        // Get the method name being called dynamically
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var calledMethod = GetStringLiteralValue(args[0]);
        if (string.IsNullOrEmpty(calledMethod))
            return; // Dynamic method name - skip

        // Get the type of the object being called on
        var callerExpr = memberExpr.CallerExpression;
        if (callerExpr == null)
            return;

        var callerTypeInfo = _semanticModel.TypeSystem.GetType(callerExpr);
        if (callerTypeInfo.IsVariant)
            return; // Unknown type - skip
        var callerType = callerTypeInfo.DisplayName;

        // Check if the method exists on the type
        var memberInfo = FindMember(callerType, calledMethod);
        if (memberInfo == null)
        {
            ReportDiagnostic(
                GDDiagnosticCode.DynamicMethodNotFound,
                $"Method '{calledMethod}' not found on type '{callerType}'",
                callExpr);
        }
        else if (memberInfo.Kind != GDRuntimeMemberKind.Method)
        {
            ReportDiagnostic(
                GDDiagnosticCode.DynamicMethodNotFound,
                $"'{calledMethod}' on type '{callerType}' is not a method",
                callExpr);
        }
    }

    /// <summary>
    /// Validates get() dynamic property access.
    /// </summary>
    private void ValidateGetMethod(GDCallExpression callExpr, GDMemberOperatorExpression memberExpr)
    {
        // Get the property name being accessed
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var propertyName = GetStringLiteralValue(args[0]);
        if (string.IsNullOrEmpty(propertyName))
            return; // Dynamic property name - skip

        // Get the type of the object
        var callerExpr = memberExpr.CallerExpression;
        if (callerExpr == null)
            return;

        var callerTypeInfo = _semanticModel.TypeSystem.GetType(callerExpr);
        if (callerTypeInfo.IsVariant)
            return; // Unknown type - skip
        var callerType = callerTypeInfo.DisplayName;

        // Skip Dictionary - it can have any keys dynamically
        if (GDGenericTypeHelper.IsDictionaryType(callerType))
            return;

        // Check if the property exists on the type
        var memberInfo = FindMember(callerType, propertyName);
        if (memberInfo == null)
        {
            ReportDiagnostic(
                GDDiagnosticCode.DynamicPropertyNotFound,
                $"Property '{propertyName}' not found on type '{callerType}'",
                callExpr);
        }
        else if (memberInfo.Kind != GDRuntimeMemberKind.Property)
        {
            // Also check if it's a method (common mistake)
            if (memberInfo.Kind == GDRuntimeMemberKind.Method)
            {
                ReportDiagnostic(
                    GDDiagnosticCode.DynamicPropertyNotFound,
                    $"'{propertyName}' on type '{callerType}' is a method, not a property. Use call() instead.",
                    callExpr);
            }
        }
    }

    /// <summary>
    /// Validates set() dynamic property access.
    /// </summary>
    private void ValidateSetMethod(GDCallExpression callExpr, GDMemberOperatorExpression memberExpr)
    {
        // Get the property name being set
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count < 1)
            return;

        var propertyName = GetStringLiteralValue(args[0]);
        if (string.IsNullOrEmpty(propertyName))
            return; // Dynamic property name - skip

        // Get the type of the object
        var callerExpr = memberExpr.CallerExpression;
        if (callerExpr == null)
            return;

        var callerTypeInfo = _semanticModel.TypeSystem.GetType(callerExpr);
        if (callerTypeInfo.IsVariant)
            return; // Unknown type - skip
        var callerType = callerTypeInfo.DisplayName;

        // Skip Dictionary - it can have any keys dynamically
        if (GDGenericTypeHelper.IsDictionaryType(callerType))
            return;

        // Check if the property exists on the type
        var memberInfo = FindMember(callerType, propertyName);
        if (memberInfo == null)
        {
            ReportDiagnostic(
                GDDiagnosticCode.DynamicPropertyNotFound,
                $"Property '{propertyName}' not found on type '{callerType}'",
                callExpr);
        }
        else if (memberInfo.Kind != GDRuntimeMemberKind.Property)
        {
            // Also check if it's a method (common mistake)
            if (memberInfo.Kind == GDRuntimeMemberKind.Method)
            {
                ReportDiagnostic(
                    GDDiagnosticCode.DynamicPropertyNotFound,
                    $"'{propertyName}' on type '{callerType}' is a method, cannot use set()",
                    callExpr);
            }
        }
    }

    private void ReportDiagnostic(GDDiagnosticCode code, string message, GDNode node)
    {
        switch (_severity)
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

    private static string? GetStringLiteralValue(GDExpression? expr)
    {
        if (expr is GDStringExpression strExpr)
        {
            return strExpr.String?.Sequence;
        }
        return null;
    }

    private GDRuntimeMemberInfo? FindMember(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        // Extract base type for generics
        var baseTypeName = ExtractBaseTypeName(typeName);

        // Check direct member
        var memberInfo = _runtimeProvider.GetMember(baseTypeName, memberName);
        if (memberInfo != null)
            return memberInfo;

        // Check inherited members
        var baseType = _runtimeProvider.GetBaseType(baseTypeName);
        while (!string.IsNullOrEmpty(baseType))
        {
            memberInfo = _runtimeProvider.GetMember(baseType, memberName);
            if (memberInfo != null)
                return memberInfo;

            baseType = _runtimeProvider.GetBaseType(baseType);
        }

        return null;
    }

    private static string ExtractBaseTypeName(string typeName)
        => GDGenericTypeHelper.ExtractBaseTypeName(typeName);
}
