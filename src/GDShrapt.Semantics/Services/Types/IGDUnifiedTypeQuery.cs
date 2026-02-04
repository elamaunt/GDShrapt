using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified interface for all type queries.
/// Replaces multiple disparate APIs with a single entry point.
/// </summary>
public interface IGDUnifiedTypeQuery
{
    // ===========================================
    // Expression Type Resolution
    // ===========================================

    /// <summary>
    /// Gets the type of an expression.
    /// </summary>
    string? GetExpressionType(GDExpression? expression);

    /// <summary>
    /// Gets the effective type for a variable at a location.
    /// Considers narrowing, declared type, and inferred type.
    /// </summary>
    string? GetEffectiveType(string variableName, GDNode? atLocation = null);

    // ===========================================
    // Type Narrowing (Control Flow)
    // ===========================================

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location.
    /// </summary>
    string? GetNarrowedType(string variableName, GDNode atLocation);

    // ===========================================
    // Union Types
    // ===========================================

    /// <summary>
    /// Gets the union type for a symbol (variable, parameter, or method).
    /// </summary>
    GDUnionType? GetUnionType(string symbolName);

    /// <summary>
    /// Gets call site types for a method parameter.
    /// </summary>
    GDUnionType? GetCallSiteTypes(string methodName, string paramName);

    // ===========================================
    // Duck Types
    // ===========================================

    /// <summary>
    /// Gets the duck type constraints for a variable.
    /// </summary>
    GDDuckType? GetDuckType(string variableName);

    /// <summary>
    /// Checks if duck type constraints should be suppressed.
    /// </summary>
    bool ShouldSuppressDuckConstraints(string symbolName);

    // ===========================================
    // Container Types
    // ===========================================

    /// <summary>
    /// Gets the container usage profile for a variable.
    /// </summary>
    GDContainerUsageProfile? GetContainerProfile(string variableName);

    /// <summary>
    /// Gets the inferred container element type.
    /// </summary>
    GDContainerElementType? GetInferredContainerType(string variableName);

    /// <summary>
    /// Gets the class-level container profile.
    /// </summary>
    GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName);

    // ===========================================
    // Confidence Analysis
    // ===========================================

    /// <summary>
    /// Gets the confidence level for a member access expression.
    /// </summary>
    GDReferenceConfidence GetMemberAccessConfidence(GDMemberOperatorExpression memberAccess);

    /// <summary>
    /// Gets the confidence level for any identifier.
    /// </summary>
    GDReferenceConfidence GetIdentifierConfidence(GDIdentifier identifier);

    // ===========================================
    // Call Site Analysis (Lambda/Callable)
    // ===========================================

    /// <summary>
    /// Infers lambda parameter types from call sites.
    /// </summary>
    IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesFromCallSites(GDMethodExpression lambda);

    /// <summary>
    /// Infers a specific lambda parameter type from call sites.
    /// </summary>
    string? InferLambdaParameterTypeFromCallSites(GDMethodExpression lambda, int parameterIndex);

    /// <summary>
    /// Infers lambda parameter types with inter-procedural analysis.
    /// </summary>
    IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesWithFlow(GDMethodExpression lambda);

    // ===========================================
    // Type Compatibility
    // ===========================================

    /// <summary>
    /// Checks if two types are compatible.
    /// </summary>
    bool AreTypesCompatible(string sourceType, string targetType);

    /// <summary>
    /// Gets the expected type at a position (reverse type inference).
    /// </summary>
    string? GetExpectedType(GDNode node);
}
