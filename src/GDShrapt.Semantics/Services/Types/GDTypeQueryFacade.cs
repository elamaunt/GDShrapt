using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Facade that implements IGDUnifiedTypeQuery by delegating to GDSemanticModel.
/// This is a transitional implementation that will be replaced with direct service composition.
/// </summary>
internal class GDTypeQueryFacade : IGDUnifiedTypeQuery
{
    private readonly GDSemanticModel _model;

    public GDTypeQueryFacade(GDSemanticModel model)
    {
        _model = model ?? throw new System.ArgumentNullException(nameof(model));
    }

    // ===========================================
    // Expression Type Resolution
    // ===========================================

    public string? GetExpressionType(GDExpression? expression)
        => _model.GetExpressionType(expression);

    public string? GetEffectiveType(string variableName, GDNode? atLocation = null)
        => _model.GetEffectiveType(variableName, atLocation);

    // ===========================================
    // Type Narrowing
    // ===========================================

    public string? GetNarrowedType(string variableName, GDNode atLocation)
        => _model.GetNarrowedType(variableName, atLocation);

    // ===========================================
    // Union Types
    // ===========================================

    public GDUnionType? GetUnionType(string symbolName)
        => _model.GetUnionType(symbolName);

    public GDUnionType? GetCallSiteTypes(string methodName, string paramName)
        => _model.GetCallSiteTypes(methodName, paramName);

    // ===========================================
    // Duck Types
    // ===========================================

    public GDDuckType? GetDuckType(string variableName)
        => _model.GetDuckType(variableName);

    public bool ShouldSuppressDuckConstraints(string symbolName)
        => _model.ShouldSuppressDuckConstraints(symbolName);

    // ===========================================
    // Container Types
    // ===========================================

    public GDContainerUsageProfile? GetContainerProfile(string variableName)
        => _model.GetContainerProfile(variableName);

    public GDContainerElementType? GetInferredContainerType(string variableName)
        => _model.GetInferredContainerType(variableName);

    public GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName)
        => _model.GetClassContainerProfile(className, variableName);

    // ===========================================
    // Confidence Analysis
    // ===========================================

    public GDReferenceConfidence GetMemberAccessConfidence(GDMemberOperatorExpression memberAccess)
        => _model.GetMemberAccessConfidence(memberAccess);

    public GDReferenceConfidence GetIdentifierConfidence(GDIdentifier identifier)
        => _model.GetIdentifierConfidence(identifier);

    // ===========================================
    // Call Site Analysis
    // ===========================================

    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesFromCallSites(GDMethodExpression lambda)
        => _model.InferLambdaParameterTypesFromCallSites(lambda);

    public string? InferLambdaParameterTypeFromCallSites(GDMethodExpression lambda, int parameterIndex)
        => _model.InferLambdaParameterTypeFromCallSites(lambda, parameterIndex);

    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesWithFlow(GDMethodExpression lambda)
        => _model.InferLambdaParameterTypesWithFlow(lambda);

    // ===========================================
    // Type Compatibility
    // ===========================================

    public bool AreTypesCompatible(string sourceType, string targetType)
        => _model.AreTypesCompatible(sourceType, targetType);

    public string? GetExpectedType(GDNode node)
        => _model.GetExpectedType(node);
}
