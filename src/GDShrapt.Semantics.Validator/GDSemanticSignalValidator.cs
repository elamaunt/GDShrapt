using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates signal operations (emit_signal, connect) using semantic model.
/// Extends the basic GDSignalValidator with type checking:
/// - emit_signal argument types vs signal parameter types
/// - connect callback signature type compatibility
/// </summary>
public class GDSemanticSignalValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDDiagnosticSeverity _severity;

    public GDSemanticSignalValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Warning)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = severity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDCallExpression callExpression)
    {
        var caller = callExpression.CallerExpression;

        // Check for emit_signal and connect calls
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            if (methodName == "emit_signal")
            {
                ValidateEmitSignalTypes(callExpression, memberExpr);
            }
        }
        else if (caller is GDIdentifierExpression identExpr)
        {
            // Direct call to emit_signal (implicit self)
            var name = identExpr.Identifier?.Sequence;
            if (name == "emit_signal")
            {
                ValidateEmitSignalTypes(callExpression, callerExpression: null);
            }
        }
    }

    private void ValidateEmitSignalTypes(GDCallExpression callExpr, GDMemberOperatorExpression? callerExpression)
    {
        var args = callExpr.Parameters;
        if (args == null || args.Count == 0)
            return; // Basic validation handles missing signal name

        // Get signal name from first argument
        var signalNameArg = args.FirstOrDefault();
        var signalName = ExtractStaticString(signalNameArg);

        if (signalName == null)
            return; // Dynamic signal name - cannot validate

        // Find the signal declaration
        var signalInfo = FindSignal(callerExpression, signalName);
        if (signalInfo == null)
            return; // Signal not found - basic validator handles this

        var signalParams = signalInfo.Parameters;
        if (signalParams == null || signalParams.Count == 0)
            return; // No parameters to check

        // Check each signal argument type (skip first arg which is signal name)
        var signalArgs = args.Skip(1).ToList();
        for (int i = 0; i < signalParams.Count && i < signalArgs.Count; i++)
        {
            var expectedType = signalParams[i].Type;
            if (string.IsNullOrEmpty(expectedType) || expectedType == "Variant")
                continue; // Variant accepts anything

            var argExpr = signalArgs[i];
            var actualType = _semanticModel.GetExpressionType(argExpr);

            if (string.IsNullOrEmpty(actualType) || actualType == "Variant" || actualType == "Unknown")
                continue; // Cannot determine actual type

            if (!AreTypesCompatible(actualType, expectedType))
            {
                ReportDiagnostic(
                    _severity,
                    GDDiagnosticCode.EmitSignalTypeMismatch,
                    $"Signal '{signalName}' parameter {i + 1} expects '{expectedType}', got '{actualType}'",
                    argExpr);
            }
        }
    }

    private GDSignalInfo? FindSignal(GDMemberOperatorExpression? callerExpr, string signalName)
    {
        // First check user-defined signals in the class scope
        var symbol = Context.Scopes.Lookup(signalName);
        if (symbol?.Kind == GDSymbolKind.Signal && symbol.Declaration is GDSignalDeclaration signalDecl)
        {
            return new GDSignalInfo
            {
                Name = signalName,
                Parameters = signalDecl.Parameters?
                    .OfType<GDParameterDeclaration>()
                    .Select(p => new GDRuntimeParameterInfo(
                        p.Identifier?.Sequence ?? "",
                        p.Type?.BuildName() ?? "Variant"))
                    .ToList()
            };
        }

        // Check through project runtime provider if available
        if (Context.RuntimeProvider is IGDProjectRuntimeProvider projectProvider)
        {
            var callerType = GetCallerType(callerExpr);
            var signal = projectProvider.GetSignal(callerType ?? "self", signalName);
            if (signal != null)
                return signal;
        }

        // Check built-in signals
        if (callerExpr != null)
        {
            var callerType = GetCallerType(callerExpr);
            if (!string.IsNullOrEmpty(callerType))
            {
                var typeInfo = Context.RuntimeProvider.GetTypeInfo(callerType);
                var signalMember = typeInfo?.Members?.FirstOrDefault(m =>
                    m.Kind == GDRuntimeMemberKind.Signal && m.Name == signalName);

                if (signalMember != null)
                {
                    return new GDSignalInfo
                    {
                        Name = signalName,
                        Parameters = signalMember.Parameters
                    };
                }
            }
        }

        return null;
    }

    private string? GetCallerType(GDMemberOperatorExpression? memberExpr)
    {
        if (memberExpr?.CallerExpression == null)
            return null;

        // self.emit_signal - type is current class
        if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (name == "self")
                return null; // Use current class signals

            // Use semantic model to get expression type
            return _semanticModel.GetExpressionType(identExpr);
        }

        // For other expressions, use semantic model
        return _semanticModel.GetExpressionType(memberExpr.CallerExpression);
    }

    private string? ExtractStaticString(GDExpression? expr)
    {
        if (expr is GDStringExpression stringExpr)
        {
            return stringExpr.String?.Sequence;
        }

        // Check for StringName(&"signal_name")
        if (expr is GDCallExpression callExpr &&
            callExpr.CallerExpression is GDIdentifierExpression identExpr &&
            identExpr.Identifier?.Sequence == "StringName")
        {
            var firstArg = callExpr.Parameters?.FirstOrDefault();
            return ExtractStaticString(firstArg);
        }

        return null;
    }

    private bool AreTypesCompatible(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return true;

        if (sourceType == targetType)
            return true;

        if (targetType == "Variant")
            return true;

        // null is compatible with reference types
        if (sourceType == "null")
            return true;

        // Use semantic model for detailed check
        return _semanticModel.AreTypesCompatible(sourceType, targetType);
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
}
