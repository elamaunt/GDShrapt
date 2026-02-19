using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified entry point for all type queries.
/// Provides a clean API over the scattered type-related methods in GDSemanticModel.
/// </summary>
public interface IGDTypeSystem
{
    // ========================================
    // Type Queries
    // ========================================

    /// <summary>
    /// Gets the semantic type for an AST node.
    /// </summary>
    GDSemanticType GetType(GDNode node);

    /// <summary>
    /// Gets the semantic type for an expression.
    /// </summary>
    GDSemanticType GetType(GDExpression expr);

    /// <summary>
    /// Gets the type node (AST representation) for an expression.
    /// Returns null if type cannot be represented as AST.
    /// </summary>
    GDTypeNode? GetTypeNode(GDExpression expr);

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location.
    /// Returns null if no narrowing applies.
    /// </summary>
    string? GetNarrowedType(string variableName, GDNode atLocation);

    // ========================================
    // Union and Duck Types
    // ========================================

    /// <summary>
    /// Gets the union type for a symbol (variable, parameter, or return value).
    /// Union types represent variables that can hold multiple types.
    /// </summary>
    GDUnionType? GetUnionType(string symbolName);

    /// <summary>
    /// Gets the duck type requirements for a variable.
    /// Duck types represent inferred method/property requirements from usage.
    /// </summary>
    GDDuckType? GetDuckType(string variableName);

    // ========================================
    // Container Analysis
    // ========================================

    /// <summary>
    /// Gets the container element type for a variable.
    /// </summary>
    GDContainerElementType? GetContainerElementType(string variableName);

    /// <summary>
    /// Gets the container element type for a class-level variable.
    /// </summary>
    GDContainerElementType? GetClassContainerElementType(string className, string variableName);

    /// <summary>
    /// Gets the container usage profile for a variable.
    /// </summary>
    GDContainerUsageProfile? GetContainerProfile(string variableName);

    /// <summary>
    /// Gets the container usage profile for a class-level variable.
    /// </summary>
    GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName);

    // ========================================
    // Type Relationships (from RuntimeProvider)
    // ========================================

    /// <summary>
    /// Checks if source type can be assigned to target type.
    /// </summary>
    bool IsAssignableTo(string sourceType, string targetType);

    /// <summary>
    /// Checks if a type supports a specific operator.
    /// </summary>
    bool SupportsOperator(string typeName, GDDualOperatorType op, string? rightType = null);

    /// <summary>
    /// Resolves the result type for an operator expression.
    /// </summary>
    string? ResolveOperatorResult(string leftType, GDDualOperatorType op, string rightType);

    /// <summary>
    /// Checks if a type supports iteration (for-in loops).
    /// </summary>
    bool IsIterable(string typeName);

    /// <summary>
    /// Checks if a type supports indexing with [] operator.
    /// </summary>
    bool IsIndexable(string typeName);

    /// <summary>
    /// Checks if a type can be null.
    /// </summary>
    bool IsNullable(string typeName);

    /// <summary>
    /// Checks if a type is numeric (int or float).
    /// </summary>
    bool IsNumeric(string typeName);

    /// <summary>
    /// Checks if a type is a vector type.
    /// </summary>
    bool IsVector(string typeName);

    // ========================================
    // Type Info
    // ========================================

    /// <summary>
    /// Gets full type info for a variable at a specific location.
    /// Combines declared, inferred, and narrowed type information.
    /// </summary>
    GDTypeInfo? GetTypeInfo(string variableName, GDNode? atLocation = null);

    /// <summary>
    /// Gets full type info for an expression.
    /// </summary>
    GDTypeInfo GetExpressionTypeInfo(GDExpression expression);

    // ========================================
    // Parameter Inference
    // ========================================

    /// <summary>
    /// Infers the type of a parameter from usage analysis.
    /// </summary>
    GDInferredParameterType InferParameterType(GDParameterDeclaration param);

    // ========================================
    // Return Type Analysis
    // ========================================

    /// <summary>
    /// Analyzes return types for a method declaration.
    /// Returns information about all return paths, their types, and consistency.
    /// </summary>
    GDMethodReturnAnalysis? AnalyzeMethodReturns(GDMethodDeclaration method);

    /// <summary>
    /// Gets the runtime provider used for type resolution.
    /// </summary>
    IGDRuntimeProvider RuntimeProvider { get; }
}
