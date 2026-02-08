using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for determining reference confidence levels.
/// Analyzes member access expressions to determine if they are Strict, Potential, or NameMatch.
/// </summary>
internal class GDConfidenceService
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDDuckTypeService _duckTypeService;
    private readonly GDUnionTypeService _unionTypeService;
    private readonly GDContainerTypeService _containerTypeService;

    /// <summary>
    /// Delegate for getting expression type.
    /// </summary>
    public delegate string? GetExpressionTypeDelegate(GDExpression? expression);

    /// <summary>
    /// Delegate for finding a symbol by name.
    /// </summary>
    public delegate GDSymbolInfo? FindSymbolDelegate(string name);

    /// <summary>
    /// Delegate for getting root variable name from expression.
    /// </summary>
    public delegate string? GetRootVariableNameDelegate(GDExpression? expression);

    private readonly GetExpressionTypeDelegate? _getExpressionType;
    private readonly FindSymbolDelegate? _findSymbol;
    private readonly GetRootVariableNameDelegate? _getRootVariableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDConfidenceService"/> class.
    /// </summary>
    public GDConfidenceService(
        IGDRuntimeProvider? runtimeProvider,
        GDDuckTypeService duckTypeService,
        GDUnionTypeService unionTypeService,
        GDContainerTypeService containerTypeService,
        GetExpressionTypeDelegate? getExpressionType = null,
        FindSymbolDelegate? findSymbol = null,
        GetRootVariableNameDelegate? getRootVariableName = null)
    {
        _runtimeProvider = runtimeProvider;
        _duckTypeService = duckTypeService ?? new GDDuckTypeService();
        _unionTypeService = unionTypeService ?? new GDUnionTypeService(runtimeProvider);
        _containerTypeService = containerTypeService ?? new GDContainerTypeService(runtimeProvider);
        _getExpressionType = getExpressionType;
        _findSymbol = findSymbol;
        _getRootVariableName = getRootVariableName;
    }

    /// <summary>
    /// Gets the confidence level for a member access expression.
    /// </summary>
    public GDReferenceConfidence GetMemberAccessConfidence(
        GDMemberOperatorExpression memberAccess,
        string? scriptTypeName = null)
    {
        if (memberAccess?.CallerExpression == null)
            return GDReferenceConfidence.Potential;

        var callerType = _getExpressionType?.Invoke(memberAccess.CallerExpression);

        // Type is known and concrete
        if (IsConcreteType(callerType))
            return GDReferenceConfidence.Strict;

        // For indexer-based member access
        if (memberAccess.CallerExpression is GDIndexerExpression indexerExpr)
        {
            return GetIndexerMemberAccessConfidence(indexerExpr, memberAccess, scriptTypeName);
        }

        // Check for type narrowing and Union types
        var varName = _getRootVariableName?.Invoke(memberAccess.CallerExpression);
        if (!string.IsNullOrEmpty(varName))
        {
            var narrowed = _duckTypeService.GetNarrowedType(varName, memberAccess);
            if (!string.IsNullOrEmpty(narrowed))
                return GDReferenceConfidence.Strict;

            // Check narrowing context for duck type constraints
            var narrowingContext = _duckTypeService.FindNarrowingContextForNode(memberAccess);
            if (narrowingContext != null)
            {
                var narrowedInfo = narrowingContext.GetNarrowedType(varName);
                if (narrowedInfo != null)
                {
                    var memberName = memberAccess.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(memberName) && narrowedInfo.RequiredMethods.ContainsKey(memberName))
                        return GDReferenceConfidence.Potential;

                    if (narrowedInfo.ExcludedTypes.Contains(GDNullSemanticType.Instance))
                        return GDReferenceConfidence.Potential;
                }
            }

            // Check Union type
            var symbol = _findSymbol?.Invoke(varName);
            var unionType = _unionTypeService.GetUnionType(varName, symbol, null);
            if (unionType != null && !unionType.IsEmpty)
            {
                var memberName = memberAccess.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                {
                    return _unionTypeService.GetUnionMemberConfidence(unionType, memberName);
                }
            }

            // Check duck-type constraints
            var duckType = _duckTypeService.GetDuckType(varName);
            if (duckType != null && duckType.HasRequirements)
            {
                var memberName = memberAccess.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                {
                    if (CheckDuckTypeMemberInTypeSystem(duckType, memberName))
                        return GDReferenceConfidence.Potential;
                }
            }
        }

        // Type is Variant or unknown without type guard
        return GDReferenceConfidence.NameMatch;
    }

    /// <summary>
    /// Gets the confidence level for any identifier.
    /// </summary>
    public GDReferenceConfidence GetIdentifierConfidence(
        GDIdentifier identifier,
        string? scriptTypeName = null)
    {
        if (identifier == null)
            return GDReferenceConfidence.NameMatch;

        var parent = identifier.Parent;

        // Member access - check caller type
        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
            return GetMemberAccessConfidence(memberOp, scriptTypeName);

        // Simple identifier - always strict
        return GDReferenceConfidence.Strict;
    }

    /// <summary>
    /// Gets confidence for a known type's member.
    /// </summary>
    public GDReferenceConfidence GetMemberConfidenceOnType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return GDReferenceConfidence.Potential;

        var memberInfo = _runtimeProvider.GetMember(typeName, memberName);
        if (memberInfo != null)
            return GDReferenceConfidence.Potential;

        return GDReferenceConfidence.Potential;
    }

    /// <summary>
    /// Gets the confidence level for member access on an indexer result.
    /// </summary>
    private GDReferenceConfidence GetIndexerMemberAccessConfidence(
        GDIndexerExpression indexerExpr,
        GDMemberOperatorExpression memberAccess,
        string? scriptTypeName)
    {
        var memberName = memberAccess.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            return GDReferenceConfidence.Potential;

        var containerVarName = _getRootVariableName?.Invoke(indexerExpr.CallerExpression);
        if (string.IsNullOrEmpty(containerVarName))
            return GDReferenceConfidence.Potential;

        // Try local container profile
        var localProfile = _containerTypeService.GetContainerProfile(containerVarName);
        if (localProfile != null)
        {
            var inferredType = localProfile.ComputeInferredType();
            var elementType = inferredType.EffectiveElementType;

            if (!elementType.IsVariant)
            {
                return GetMemberConfidenceOnType(elementType.DisplayName, memberName);
            }
        }

        // Try class-level container profile
        var classProfile = _containerTypeService.GetClassContainerProfile(scriptTypeName ?? "", containerVarName);
        if (classProfile != null)
        {
            var inferredType = classProfile.ComputeInferredType();
            var elementType = inferredType.EffectiveElementType;

            if (!elementType.IsVariant)
            {
                return GetMemberConfidenceOnType(elementType.DisplayName, memberName);
            }

            if (inferredType.ElementUnionType != null && inferredType.ElementUnionType.IsUnion)
            {
                return _unionTypeService.GetUnionMemberConfidence(inferredType.ElementUnionType, memberName);
            }
        }

        return GDReferenceConfidence.Potential;
    }

    /// <summary>
    /// Checks if a duck type member exists in the type system.
    /// </summary>
    private bool CheckDuckTypeMemberInTypeSystem(GDDuckType duckType, string memberName)
    {
        if (_runtimeProvider is GDGodotTypesProvider typesProvider)
        {
            if (duckType.RequiredMethods.ContainsKey(memberName))
            {
                var typesWithMethod = typesProvider.FindTypesWithMethod(memberName);
                if (typesWithMethod.Count > 0)
                    return true;
            }

            if (duckType.RequiredProperties.ContainsKey(memberName))
            {
                var typesWithProperty = typesProvider.FindTypesWithProperty(memberName);
                if (typesWithProperty.Count > 0)
                    return true;
            }
        }
        else if (_runtimeProvider is GDCompositeRuntimeProvider compositeProvider)
        {
            var godotProvider = compositeProvider.GodotTypesProvider;
            if (godotProvider != null)
            {
                if (duckType.RequiredMethods.ContainsKey(memberName))
                {
                    var typesWithMethod = godotProvider.FindTypesWithMethod(memberName);
                    if (typesWithMethod.Count > 0)
                        return true;
                }

                if (duckType.RequiredProperties.ContainsKey(memberName))
                {
                    var typesWithProperty = godotProvider.FindTypesWithProperty(memberName);
                    if (typesWithProperty.Count > 0)
                        return true;
                }
            }

            var projectProvider = compositeProvider.ProjectTypesProvider;
            if (projectProvider != null)
            {
                if (duckType.RequiredMethods.ContainsKey(memberName))
                {
                    var typesWithMethod = projectProvider.FindTypesWithMethod(memberName);
                    if (typesWithMethod.Count > 0)
                        return true;
                }

                if (duckType.RequiredProperties.ContainsKey(memberName))
                {
                    var typesWithProperty = projectProvider.FindTypesWithProperty(memberName);
                    if (typesWithProperty.Count > 0)
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a human-readable reason for confidence determination.
    /// </summary>
    public string? GetConfidenceReason(GDIdentifier identifier)
    {
        if (identifier == null)
            return null;

        var parent = identifier.Parent;

        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
        {
            var callerType = memberOp.CallerExpression != null
                ? _getExpressionType?.Invoke(memberOp.CallerExpression)
                : null;

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                return $"Caller type is '{callerType}'";

            var varName = memberOp.CallerExpression != null
                ? _getRootVariableName?.Invoke(memberOp.CallerExpression)
                : null;

            if (!string.IsNullOrEmpty(varName))
            {
                var narrowed = _duckTypeService.GetNarrowedType(varName, memberOp);
                if (narrowed != null)
                    return $"Variable '{varName}' narrowed to '{narrowed}' by control flow";

                var duckType = _duckTypeService.GetDuckType(varName);
                if (duckType != null)
                    return $"Variable '{varName}' is duck-typed";

                return $"Variable '{varName}' type is unknown";
            }

            return "Caller expression type unknown";
        }

        // Simple identifier
        var symbol = _findSymbol?.Invoke(identifier.Sequence ?? "");
        if (symbol != null)
        {
            if (symbol.IsInherited)
                return $"Inherited member from {symbol.DeclaringTypeName}";
            if (symbol.Kind == GDSymbolKind.Parameter)
                return "Method parameter";
            if (symbol.Kind == GDSymbolKind.Variable && symbol.DeclaringTypeName == null)
                return "Local variable";
            if (symbol.DeclaringTypeName != null)
                return $"Class member in {symbol.DeclaringTypeName}";
        }

        return "Symbol in scope";
    }

    /// <summary>
    /// Checks if a type name represents a concrete (non-Variant, non-Unknown) type.
    /// </summary>
    private static bool IsConcreteType(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName)
            && typeName != "Variant"
            && !typeName.StartsWith("Unknown");
    }
}
