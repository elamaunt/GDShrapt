using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for Union type management and resolution.
/// Tracks union types for Variant variables based on their assignments.
/// </summary>
public class GDUnionTypeService
{
    private readonly Dictionary<string, GDUnionType> _unionTypeCache = new();
    private readonly Dictionary<string, GDUnionType> _callSiteParameterTypes = new();
    private readonly Dictionary<string, GDVariableUsageProfile> _variableProfiles = new();
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private GDTypeInferenceEngine? _typeEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDUnionTypeService"/> class.
    /// </summary>
    public GDUnionTypeService(IGDRuntimeProvider? runtimeProvider = null)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Sets the type inference engine (internal, since GDTypeInferenceEngine is internal).
    /// </summary>
    internal void SetTypeEngine(GDTypeInferenceEngine? typeEngine)
    {
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Gets the union type for a symbol (variable, parameter, or method).
    /// For variables, computes from all assignments.
    /// For methods, computes from all return statements.
    /// Returns null if the symbol is not found.
    /// </summary>
    public GDUnionType? GetUnionType(string symbolName, GDSymbolInfo? symbol, GDScriptFile? scriptFile)
    {
        if (string.IsNullOrEmpty(symbolName))
            return null;

        // Check cache first
        if (_unionTypeCache.TryGetValue(symbolName, out var cached))
            return cached;

        if (symbol?.DeclarationNode is GDMethodDeclaration method)
        {
            var union = ComputeMethodReturnUnion(method);
            if (union != null)
            {
                EnrichUnionTypeIfNeeded(union);
                _unionTypeCache[symbolName] = union;
                return union;
            }
        }

        if (symbol?.Kind == GDSymbolKind.Parameter && symbol.DeclarationNode is GDParameterDeclaration param)
        {
            var union = ComputeParameterUnion(param, symbol);
            if (union != null)
            {
                EnrichUnionTypeIfNeeded(union);
                _unionTypeCache[symbolName] = union;
                return union;
            }
        }

        // Compute from variable profile (for local variables)
        var profile = GetVariableProfile(symbolName);
        if (profile == null)
            return null;

        var varUnion = profile.ComputeUnionType();
        EnrichUnionTypeIfNeeded(varUnion);
        _unionTypeCache[symbolName] = varUnion;
        return varUnion;
    }

    /// <summary>
    /// Gets the member access confidence for a Union type.
    /// </summary>
    public GDReferenceConfidence GetUnionMemberConfidence(GDUnionType unionType, string memberName)
    {
        if (unionType == null || string.IsNullOrEmpty(memberName) || _runtimeProvider == null)
            return GDReferenceConfidence.NameMatch;

        var resolver = new GDUnionTypeResolver(_runtimeProvider);
        return resolver.GetMemberConfidence(unionType, memberName);
    }

    /// <summary>
    /// Gets the call site types for a method parameter.
    /// </summary>
    public GDUnionType? GetCallSiteTypes(string methodName, string paramName)
    {
        if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(paramName))
            return null;

        var key = BuildParameterKey(methodName, paramName);
        return _callSiteParameterTypes.TryGetValue(key, out var union) ? union : null;
    }

    /// <summary>
    /// Gets the variable usage profile.
    /// </summary>
    public GDVariableUsageProfile? GetVariableProfile(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _variableProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets all variable usage profiles.
    /// </summary>
    public IEnumerable<GDVariableUsageProfile> GetAllVariableProfiles()
    {
        return _variableProfiles.Values;
    }

    /// <summary>
    /// Sets the variable usage profile.
    /// </summary>
    internal void SetVariableProfile(string variableName, GDVariableUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _variableProfiles[variableName] = profile;
        }
    }

    /// <summary>
    /// Sets call site parameter types for inter-procedural analysis.
    /// </summary>
    internal void SetCallSiteParameterTypes(string methodName, string paramName, GDUnionType callSiteTypes)
    {
        if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(paramName) || callSiteTypes == null)
            return;

        var key = BuildParameterKey(methodName, paramName);
        _callSiteParameterTypes[key] = callSiteTypes;

        // Invalidate cache for this parameter
        _unionTypeCache.Remove(paramName);
    }

    /// <summary>
    /// Clears union type cache for a variable (used during reassignment).
    /// </summary>
    internal void ClearUnionTypeCache(string variableName)
    {
        if (!string.IsNullOrEmpty(variableName))
        {
            _unionTypeCache.Remove(variableName);
        }
    }

    /// <summary>
    /// Computes the union type for a method's return statements.
    /// </summary>
    private GDUnionType? ComputeMethodReturnUnion(GDMethodDeclaration method)
    {
        var collector = new GDReturnTypeCollector(method, _runtimeProvider);
        collector.Collect();
        return collector.ComputeReturnUnionType();
    }

    /// <summary>
    /// Computes the union type for a parameter based on type guards, null checks, and call site arguments.
    /// </summary>
    private GDUnionType? ComputeParameterUnion(GDParameterDeclaration param, GDSymbolInfo symbol)
    {
        var paramName = param.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return null;

        var method = param.Parent?.Parent as GDMethodDeclaration;
        if (method?.Statements == null)
            return null;

        // Use analyzer to compute expected types from code analysis
        var analyzer = new GDParameterTypeAnalyzer(_runtimeProvider, _typeEngine);
        var union = analyzer.ComputeExpectedTypes(param, method);

        // Add call site argument types if available
        var methodName = method.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(methodName))
        {
            var key = BuildParameterKey(methodName, paramName);
            if (_callSiteParameterTypes.TryGetValue(key, out var callSiteUnion) && callSiteUnion != null)
            {
                foreach (var type in callSiteUnion.Types)
                {
                    union.AddType(type, isHighConfidence: false);
                }
            }
        }

        return union.IsEmpty ? null : union;
    }

    /// <summary>
    /// Enriches a Union type with common base type information.
    /// </summary>
    private void EnrichUnionTypeIfNeeded(GDUnionType? union)
    {
        if (union == null || _runtimeProvider == null)
            return;

        var resolver = new GDUnionTypeResolver(_runtimeProvider);
        resolver.EnrichUnionType(union);
    }

    /// <summary>
    /// Builds a unique key for method.parameter.
    /// </summary>
    private static string BuildParameterKey(string methodName, string paramName)
    {
        return $"{methodName}.{paramName}";
    }
}
