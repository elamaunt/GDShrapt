using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes signal-related expressions and await types.
/// Handles emit_signal, connect, disconnect, is_connected, and signal await types.
/// </summary>
internal class GDSignalTypeAnalyzer
{
    private readonly IGDRuntimeTypeInjector _typeInjector;
    private readonly GDTypeInjectionContext _injectionContext;
    private readonly Func<GDExpression, string> _inferType;
    private readonly Func<string, string, GDRuntimeMemberInfo> _findMemberWithInheritance;

    /// <summary>
    /// Creates a new signal type analyzer.
    /// </summary>
    /// <param name="typeInjector">Optional type injector for signal parameter types.</param>
    /// <param name="injectionContext">Optional injection context.</param>
    /// <param name="inferType">Function to infer type of an expression.</param>
    /// <param name="findMemberWithInheritance">Function to find member info with inheritance.</param>
    public GDSignalTypeAnalyzer(
        IGDRuntimeTypeInjector typeInjector,
        GDTypeInjectionContext injectionContext,
        Func<GDExpression, string> inferType,
        Func<string, string, GDRuntimeMemberInfo> findMemberWithInheritance)
    {
        _typeInjector = typeInjector;
        _injectionContext = injectionContext;
        _inferType = inferType ?? throw new ArgumentNullException(nameof(inferType));
        _findMemberWithInheritance = findMemberWithInheritance;
    }

    /// <summary>
    /// Infers the type for signal-related calls (emit_signal, connect, disconnect, etc.).
    /// </summary>
    /// <param name="callExpr">The call expression.</param>
    /// <param name="caller">The caller expression (method or identifier).</param>
    /// <returns>The return type of the signal method, or null if not a signal call.</returns>
    public string InferSignalCallType(GDCallExpression callExpr, GDExpression caller)
    {
        string methodName = null;

        // Check for direct call: emit_signal(...), connect(...)
        if (caller is GDIdentifierExpression identExpr)
        {
            methodName = identExpr.Identifier?.Sequence;
        }
        // Check for method call: obj.emit_signal(...), obj.connect(...)
        else if (caller is GDMemberOperatorExpression memberExpr)
        {
            methodName = memberExpr.Identifier?.Sequence;
        }

        if (string.IsNullOrEmpty(methodName))
            return null;

        return methodName switch
        {
            "emit_signal" => "void",           // emit_signal returns void (triggers handlers)
            "connect" => "Error",              // connect returns Error (int enum in Godot 4)
            "disconnect" => "void",            // disconnect returns void
            "is_connected" => "bool",          // is_connected returns bool
            "get_signal_connection_list" => "Array",  // Returns Array of Dictionaries
            "get_signal_list" => "Array",      // Returns Array of Dictionaries
            "has_signal" => "bool",            // has_signal returns bool
            _ => null
        };
    }

    /// <summary>
    /// Infers the type of an await expression.
    /// For signals: returns the emission type (first param, void for no params, Array for multiple).
    /// For coroutines: returns the function's return type.
    /// </summary>
    /// <param name="awaitExpr">The await expression.</param>
    /// <param name="inferCallType">Function to infer call expression type.</param>
    /// <returns>The type that the await expression resolves to.</returns>
    public GDTypeNode InferAwaitType(
        GDAwaitExpression awaitExpr,
        Func<GDCallExpression, string> inferCallType)
    {
        var innerExpr = awaitExpr.Expression;
        if (innerExpr == null)
            return GDTypeInferenceUtilities.CreateSimpleType("Variant");

        // 1. Call expression - coroutine or method returning Signal
        if (innerExpr is GDCallExpression callExpr)
        {
            var returnType = inferCallType(callExpr);

            // If it returns a Signal, we can't know the emission type without more context
            if (returnType == "Signal")
                return GDTypeInferenceUtilities.CreateSimpleType("Variant");

            // Otherwise, return the function's return type (coroutine semantics)
            return GDTypeInferenceUtilities.CreateSimpleType(returnType ?? "Variant");
        }

        // 2. Identifier - local signal (signal defined in current class)
        if (innerExpr is GDIdentifierExpression identExpr)
        {
            var signalName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(signalName))
            {
                // Look for signal declaration in current class
                var signalDecl = FindLocalSignalDeclaration(signalName, awaitExpr);
                if (signalDecl != null)
                {
                    return GDTypeInferenceUtilities.CreateSimpleType(
                        GetSignalEmissionTypeFromDecl(signalDecl));
                }

                // Try type injector for inherited/Godot signals
                if (_typeInjector != null)
                {
                    var currentType = _injectionContext?.CurrentClass ?? "self";
                    var paramTypes = _typeInjector.GetSignalParameterTypes(signalName, currentType);
                    if (paramTypes != null)
                    {
                        return GDTypeInferenceUtilities.CreateSimpleType(
                            GetSignalEmissionType(paramTypes));
                    }
                }
            }
        }

        // 3. Member access - signal on an object (obj.signal_name)
        if (innerExpr is GDMemberOperatorExpression memberExpr)
        {
            var signalName = memberExpr.Identifier?.Sequence;
            var callerType = _inferType(memberExpr.CallerExpression);

            if (!string.IsNullOrEmpty(signalName) && !string.IsNullOrEmpty(callerType))
            {
                // Check if it's a signal via runtime provider (with inheritance)
                var memberInfo = _findMemberWithInheritance?.Invoke(callerType, signalName);
                if (memberInfo?.Kind == GDRuntimeMemberKind.Signal)
                {
                    // Try type injector for signal parameter types
                    if (_typeInjector != null)
                    {
                        var paramTypes = _typeInjector.GetSignalParameterTypes(signalName, callerType);
                        if (paramTypes != null)
                        {
                            return GDTypeInferenceUtilities.CreateSimpleType(
                                GetSignalEmissionType(paramTypes));
                        }
                    }
                    // Signal exists but we can't determine emission type
                    return GDTypeInferenceUtilities.CreateSimpleType("Variant");
                }

                // Check if member type is Signal
                if (memberInfo?.Type == "Signal")
                {
                    if (_typeInjector != null)
                    {
                        var paramTypes = _typeInjector.GetSignalParameterTypes(signalName, callerType);
                        if (paramTypes != null)
                        {
                            return GDTypeInferenceUtilities.CreateSimpleType(
                                GetSignalEmissionType(paramTypes));
                        }
                    }
                    return GDTypeInferenceUtilities.CreateSimpleType("Variant");
                }
            }
        }

        // 4. Fallback - Variant
        return GDTypeInferenceUtilities.CreateSimpleType("Variant");
    }

    /// <summary>
    /// Finds a signal declaration in the current class context.
    /// </summary>
    public GDSignalDeclaration FindLocalSignalDeclaration(string signalName, GDNode context)
    {
        var classDecl = context.RootClassDeclaration;
        if (classDecl == null)
            return null;

        foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
        {
            if (member is GDSignalDeclaration signalDecl &&
                signalDecl.Identifier?.Sequence == signalName)
            {
                return signalDecl;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the emission type from a signal declaration.
    /// 0 params = "void", 1 param = param type, multiple params = "Array".
    /// </summary>
    public string GetSignalEmissionTypeFromDecl(GDSignalDeclaration signalDecl)
    {
        var parameters = signalDecl.Parameters;
        if (parameters == null)
            return "void";

        var paramCount = 0;
        GDParameterDeclaration firstParam = null;

        foreach (var param in parameters)
        {
            if (paramCount == 0)
                firstParam = param;
            paramCount++;
            if (paramCount > 1)
                return "Array";
        }

        if (paramCount == 0)
            return "void";

        // Single parameter - return its type
        return firstParam?.Type?.BuildName() ?? "Variant";
    }

    /// <summary>
    /// Gets the emission type from signal parameter types list.
    /// 0 params = "void", 1 param = param type, multiple params = "Array".
    /// </summary>
    public string GetSignalEmissionType(IReadOnlyList<string> paramTypes)
    {
        if (paramTypes == null || paramTypes.Count == 0)
            return "void";

        if (paramTypes.Count == 1)
            return paramTypes[0];

        return "Array";
    }
}
