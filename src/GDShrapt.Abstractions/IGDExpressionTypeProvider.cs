using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Provides expression type inference for flow analysis.
/// Unifies GDTypeInferenceEngine (basic AST inference) and
/// GDExpressionTypeService (rich inference with caching and union types)
/// behind a single interface.
/// </summary>
public interface IGDExpressionTypeProvider
{
    /// <summary>
    /// Infers the semantic type of an expression.
    /// Returns null if the type cannot be determined.
    /// </summary>
    GDSemanticType? InferType(GDExpression expression);

    /// <summary>
    /// Looks up a symbol by name in the current scope.
    /// </summary>
    GDSymbol? LookupSymbol(string name);

    /// <summary>
    /// Gets the runtime type provider.
    /// </summary>
    IGDRuntimeProvider? RuntimeProvider { get; }

    /// <summary>
    /// Checks if a type name represents a numeric type.
    /// </summary>
    bool IsNumericType(string typeName);
}
