using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for argument type analysis in function calls.
/// Extracted from GDSemanticModel to reduce its size.
/// </summary>
internal class GDArgumentTypeService
{
    private readonly IGDRuntimeProvider? _runtimeProvider;

    // Delegates to avoid circular dependencies
    private Func<string, GDSymbolInfo?>? _findSymbol;
    private Func<GDExpression?, string?>? _getExpressionType;
    private Func<string, string, GDRuntimeMemberInfo?>? _findMemberWithInheritance;
    private string? _baseTypeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDArgumentTypeService"/> class.
    /// </summary>
    public GDArgumentTypeService(IGDRuntimeProvider? runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Sets the delegate for finding symbols.
    /// </summary>
    internal void SetFindSymbolDelegate(Func<string, GDSymbolInfo?> findSymbol)
    {
        _findSymbol = findSymbol;
    }

    /// <summary>
    /// Sets the delegate for getting expression type.
    /// </summary>
    internal void SetGetExpressionTypeDelegate(Func<GDExpression?, string?> getExpressionType)
    {
        _getExpressionType = getExpressionType;
    }

    /// <summary>
    /// Sets the delegate for finding member with inheritance.
    /// </summary>
    internal void SetFindMemberWithInheritanceDelegate(Func<string, string, GDRuntimeMemberInfo?> findMember)
    {
        _findMemberWithInheritance = findMember;
    }

    /// <summary>
    /// Sets the base type name for implicit self method resolution.
    /// </summary>
    internal void SetBaseTypeName(string? baseTypeName)
    {
        _baseTypeName = baseTypeName;
    }

    /// <summary>
    /// Gets the type diff for a call expression argument at the given index.
    /// </summary>
    public GDArgumentTypeDiff? GetArgumentTypeDiff(GDCallExpression call, int argumentIndex)
    {
        var args = call.Parameters?.ToList();
        if (args == null || argumentIndex >= args.Count)
            return null;

        var arg = args[argumentIndex];

        var (methodDecl, parameterInfo) = ResolveCalledMethod(call, argumentIndex);
        var actualType = _getExpressionType?.Invoke(arg);
        var actualSource = GetExpressionTypeSource(arg);

        // If we have a method declaration with parameter info
        if (methodDecl != null)
        {
            var parameters = methodDecl.Parameters?.ToList();
            if (parameters != null && argumentIndex < parameters.Count)
            {
                var param = parameters[argumentIndex];
                var paramName = param.Identifier?.Sequence;

                var explicitType = param.Type?.BuildName();
                if (string.IsNullOrEmpty(explicitType))
                {
                    return GDArgumentTypeDiff.Skip(argumentIndex, paramName);
                }

                var expectedTypes = new List<string> { explicitType };
                var expectedSource = "type annotation";

                var isCompatible = CheckTypeCompatibility(actualType, expectedTypes, null);
                var reason = isCompatible ? null : FormatIncompatibilityReason(actualType, expectedTypes, null);

                if (isCompatible)
                {
                    return GDArgumentTypeDiff.Compatible(
                        argumentIndex, paramName, actualType, actualSource,
                        expectedTypes, expectedSource,
                        GDReferenceConfidence.Strict);
                }
                else
                {
                    return GDArgumentTypeDiff.Incompatible(
                        argumentIndex, paramName, actualType, actualSource,
                        expectedTypes, expectedSource, reason!,
                        GDReferenceConfidence.Strict);
                }
            }
        }

        // If we have runtime parameter info (for built-in functions/methods)
        if (parameterInfo != null)
        {
            var expectedType = parameterInfo.Type;

            if (parameterInfo.IsParams)
            {
                return GDArgumentTypeDiff.Skip(argumentIndex, parameterInfo.Name);
            }

            if (string.IsNullOrEmpty(expectedType) || expectedType == "Variant")
            {
                return GDArgumentTypeDiff.Skip(argumentIndex, parameterInfo.Name);
            }

            var expectedTypes = new List<string> { expectedType };
            var isCompatible = CheckTypeCompatibility(actualType, expectedTypes, null);
            var reason = isCompatible ? null : FormatIncompatibilityReason(actualType, expectedTypes, null);

            if (isCompatible)
            {
                return GDArgumentTypeDiff.Compatible(
                    argumentIndex, parameterInfo.Name, actualType, actualSource,
                    expectedTypes, "function signature",
                    GDReferenceConfidence.Strict);
            }
            else
            {
                return GDArgumentTypeDiff.Incompatible(
                    argumentIndex, parameterInfo.Name, actualType, actualSource,
                    expectedTypes, "function signature", reason!,
                    GDReferenceConfidence.Strict);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all argument type diffs for a call expression.
    /// </summary>
    public IEnumerable<GDArgumentTypeDiff> GetAllArgumentTypeDiffs(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            yield break;

        for (int i = 0; i < args.Count; i++)
        {
            var diff = GetArgumentTypeDiff(call, i);
            if (diff != null)
                yield return diff;
        }
    }

    /// <summary>
    /// Gets the source description for an expression type.
    /// </summary>
    public string? GetExpressionTypeSource(GDExpression? expr)
    {
        return expr switch
        {
            GDStringExpression => "string literal",
            GDNumberExpression num => num.Number?.Sequence?.Contains('.') == true ? "float literal" : "integer literal",
            GDBoolExpression => "boolean literal",
            GDArrayInitializerExpression => "array literal",
            GDDictionaryInitializerExpression => "dictionary literal",
            GDIdentifierExpression id when id.Identifier?.Sequence == "null" => "null literal",
            GDIdentifierExpression id => $"variable '{id.Identifier?.Sequence}'",
            GDMemberOperatorExpression => "property access",
            GDCallExpression => "function call result",
            GDIndexerExpression => "indexer access",
            _ => null
        };
    }

    /// <summary>
    /// Resolves the called method/function and returns parameter info.
    /// </summary>
    private (GDMethodDeclaration? method, GDRuntimeParameterInfo? paramInfo) ResolveCalledMethod(GDCallExpression call, int argIndex)
    {
        var caller = call.CallerExpression;

        // Direct function call
        if (caller is GDIdentifierExpression idExpr)
        {
            var funcName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(funcName))
            {
                // Check user-defined functions first
                var symbol = _findSymbol?.Invoke(funcName);
                if (symbol?.DeclarationNode is GDMethodDeclaration method)
                {
                    return (method, null);
                }

                // Check built-in global functions
                if (_runtimeProvider != null)
                {
                    var funcInfo = _runtimeProvider.GetGlobalFunction(funcName);
                    if (funcInfo?.Parameters != null && argIndex < funcInfo.Parameters.Count)
                    {
                        return (null, funcInfo.Parameters[argIndex]);
                    }
                }

                // Fallback: Check methods of the base class (implicit self)
                if (_runtimeProvider != null && !string.IsNullOrEmpty(_baseTypeName))
                {
                    var memberInfo = _findMemberWithInheritance?.Invoke(_baseTypeName, funcName);
                    if (memberInfo?.Parameters != null && argIndex < memberInfo.Parameters.Count)
                    {
                        return (null, memberInfo.Parameters[argIndex]);
                    }
                }
            }
        }
        // Method call: obj.method() or self.method()
        else if (caller is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            var callerExprType = _getExpressionType?.Invoke(memberExpr.CallerExpression);

            if (!string.IsNullOrEmpty(methodName))
            {
                // self.method()
                if (memberExpr.CallerExpression is GDIdentifierExpression selfExpr &&
                    selfExpr.Identifier?.Sequence == "self")
                {
                    var symbol = _findSymbol?.Invoke(methodName);
                    if (symbol?.DeclarationNode is GDMethodDeclaration method)
                    {
                        return (method, null);
                    }
                }

                // Type.method() - check RuntimeProvider
                if (!string.IsNullOrEmpty(callerExprType) && _runtimeProvider != null)
                {
                    var memberInfo = _findMemberWithInheritance?.Invoke(callerExprType, methodName);
                    if (memberInfo?.Parameters != null && argIndex < memberInfo.Parameters.Count)
                    {
                        return (null, memberInfo.Parameters[argIndex]);
                    }
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Checks if actual type is compatible with any of the expected types.
    /// </summary>
    private bool CheckTypeCompatibility(string? actualType, IReadOnlyList<string> expectedTypes, GDDuckType? duckConstraints)
    {
        if (string.IsNullOrEmpty(actualType))
            return true;

        if (expectedTypes.Count == 0 && (duckConstraints == null || !duckConstraints.HasRequirements))
            return true;

        foreach (var expected in expectedTypes)
        {
            if (string.IsNullOrEmpty(expected) || expected == "Variant")
                return true;

            if (actualType == expected)
                return true;

            if (actualType == "null")
                return true;

            if (_runtimeProvider?.IsAssignableTo(actualType, expected) == true)
                return true;
        }

        if (duckConstraints != null && duckConstraints.HasRequirements)
        {
            return CheckDuckTypeCompatibility(actualType, duckConstraints);
        }

        return expectedTypes.Count == 0;
    }

    /// <summary>
    /// Checks if a type satisfies duck typing constraints.
    /// </summary>
    private bool CheckDuckTypeCompatibility(string actualType, GDDuckType duckConstraints)
    {
        if (_runtimeProvider == null || _findMemberWithInheritance == null)
            return true;

        foreach (var method in duckConstraints.RequiredMethods.Keys)
        {
            var memberInfo = _findMemberWithInheritance(actualType, method);
            if (memberInfo == null || memberInfo.Kind != GDRuntimeMemberKind.Method)
                return false;
        }

        foreach (var prop in duckConstraints.RequiredProperties.Keys)
        {
            var memberInfo = _findMemberWithInheritance(actualType, prop);
            if (memberInfo == null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Formats the incompatibility reason message.
    /// </summary>
    private string FormatIncompatibilityReason(string? actualType, IReadOnlyList<string> expectedTypes, GDDuckType? duckConstraints)
    {
        actualType ??= "unknown";

        if (expectedTypes.Count == 1)
        {
            var expected = expectedTypes[0];

            if (_runtimeProvider != null)
            {
                if (_runtimeProvider.IsAssignableTo(expected, actualType))
                {
                    return $"'{actualType}' is not a subtype of '{expected}'. Hint: '{expected}' extends '{actualType}', but not vice versa";
                }
            }

            return $"'{actualType}' is not assignable to '{expected}'";
        }
        else if (expectedTypes.Count > 1)
        {
            return $"'{actualType}' is not among expected types [{string.Join(", ", expectedTypes)}]";
        }
        else if (duckConstraints != null && duckConstraints.HasRequirements && _findMemberWithInheritance != null)
        {
            var missing = new List<string>();

            foreach (var method in duckConstraints.RequiredMethods.Keys)
            {
                var memberInfo = _findMemberWithInheritance(actualType, method);
                if (memberInfo == null || memberInfo.Kind != GDRuntimeMemberKind.Method)
                    missing.Add($"{method}()");
            }

            foreach (var prop in duckConstraints.RequiredProperties.Keys)
            {
                var memberInfo = _findMemberWithInheritance(actualType, prop);
                if (memberInfo == null)
                    missing.Add(prop);
            }

            if (missing.Count > 0)
            {
                return $"Type '{actualType}' does not have: {string.Join(", ", missing)}";
            }
        }

        return $"'{actualType}' is not compatible";
    }
}
