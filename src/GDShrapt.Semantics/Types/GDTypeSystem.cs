using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Implementation of IGDTypeSystem.
/// Provides unified access to type information from GDSemanticModel.
/// </summary>
public class GDTypeSystem : IGDTypeSystem
{
    private readonly GDSemanticModel _model;
    private readonly IGDRuntimeProvider _runtimeProvider;

    public GDTypeSystem(GDSemanticModel model, IGDRuntimeProvider runtimeProvider)
    {
        _model = model;
        _runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc/>
    public IGDRuntimeProvider RuntimeProvider => _runtimeProvider;

    // ========================================
    // Type Queries
    // ========================================

    /// <inheritdoc/>
    public GDSemanticType GetType(GDNode node)
    {
        var typeName = _model.GetTypeForNode(node);
        return GDSemanticType.FromTypeName(typeName);
    }

    /// <inheritdoc/>
    public GDSemanticType GetType(GDExpression expr)
    {
        var typeNode = _model.GetTypeNodeForExpression(expr);
        if (typeNode != null)
            return GDSemanticType.FromTypeName(typeNode.BuildName());

        var typeName = _model.GetTypeForNode(expr);
        return GDSemanticType.FromTypeName(typeName);
    }

    /// <inheritdoc/>
    public GDTypeNode? GetTypeNode(GDExpression expr)
    {
        return _model.GetTypeNodeForExpression(expr);
    }

    /// <inheritdoc/>
    public string? GetNarrowedType(string variableName, GDNode atLocation)
    {
        return _model.GetNarrowedType(variableName, atLocation);
    }

    // ========================================
    // Container Analysis
    // ========================================

    /// <inheritdoc/>
    public GDContainerElementType? GetContainerElementType(string variableName)
    {
        var profile = _model.GetContainerProfile(variableName);
        return profile?.ComputeInferredType();
    }

    /// <inheritdoc/>
    public GDContainerUsageProfile? GetContainerProfile(string variableName)
    {
        return _model.GetContainerProfile(variableName);
    }

    // ========================================
    // Type Relationships
    // ========================================

    /// <inheritdoc/>
    public bool IsAssignableTo(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;
        return _runtimeProvider.IsAssignableTo(sourceType, targetType);
    }

    /// <inheritdoc/>
    public bool SupportsOperator(string typeName, GDDualOperatorType op, string? rightType = null)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var operatorName = ConvertOperatorToString(op);
        if (operatorName == null)
            return false;

        var supportingTypes = _runtimeProvider.GetTypesWithOperator(operatorName);
        if (supportingTypes == null || supportingTypes.Count == 0)
            return true; // No info, assume compatible

        return supportingTypes.Contains(typeName) ||
               supportingTypes.Any(st => _runtimeProvider.IsAssignableTo(typeName, st));
    }

    /// <inheritdoc/>
    public string? ResolveOperatorResult(string leftType, GDDualOperatorType op, string rightType)
    {
        if (string.IsNullOrEmpty(leftType) || string.IsNullOrEmpty(rightType))
            return null;

        var operatorName = ConvertOperatorToString(op);
        if (operatorName == null)
            return null;

        return _runtimeProvider.ResolveOperatorResult(leftType, operatorName, rightType);
    }

    /// <inheritdoc/>
    public bool IsIterable(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;
        return _runtimeProvider.IsIterableType(typeName);
    }

    /// <inheritdoc/>
    public bool IsIndexable(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;
        return _runtimeProvider.IsIndexableType(typeName);
    }

    /// <inheritdoc/>
    public bool IsNullable(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return true;
        return _runtimeProvider.IsNullableType(typeName);
    }

    /// <inheritdoc/>
    public bool IsNumeric(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;
        return _runtimeProvider.IsNumericType(typeName);
    }

    /// <inheritdoc/>
    public bool IsVector(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;
        return _runtimeProvider.IsVectorType(typeName);
    }

    // ========================================
    // Type Info
    // ========================================

    /// <inheritdoc/>
    public GDTypeInfo? GetTypeInfo(string variableName, GDNode? atLocation = null)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Try to get from flow analyzer first
        if (atLocation != null)
        {
            var narrowedType = _model.GetNarrowedType(variableName, atLocation);
            if (!string.IsNullOrEmpty(narrowedType))
            {
                return new GDTypeInfo
                {
                    NarrowedType = GDSemanticType.FromTypeName(narrowedType),
                    Confidence = GDTypeConfidence.Certain // Narrowed type is certain from control flow
                };
            }
        }

        // Try to get container profile
        var containerProfile = _model.GetContainerProfile(variableName);
        if (containerProfile != null)
        {
            var containerTypeName = containerProfile.IsDictionary ? "Dictionary" : "Array";
            return new GDTypeInfo
            {
                InferredType = GDSemanticType.FromTypeName(containerTypeName),
                ContainerInfo = containerProfile.ComputeInferredType(),
                Confidence = GDTypeConfidence.High // Inferred from container analysis
            };
        }

        // Fallback: try to get symbol info
        var symbol = _model.FindSymbol(variableName);
        if (symbol != null)
        {
            return new GDTypeInfo
            {
                DeclaredSemanticType = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDSemanticType.FromTypeName(symbol.TypeName)
                    : null,
                InferredType = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDSemanticType.FromTypeName(symbol.TypeName)
                    : GDVariantSemanticType.Instance,
                Confidence = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDTypeConfidence.Certain // Declared type
                    : GDTypeConfidence.Unknown
            };
        }

        return null;
    }

    // ========================================
    // Helper Methods
    // ========================================

    private static string? ConvertOperatorToString(GDDualOperatorType op)
    {
        return op switch
        {
            GDDualOperatorType.Addition => "+",
            GDDualOperatorType.Subtraction => "-",
            GDDualOperatorType.Multiply => "*",
            GDDualOperatorType.Division => "/",
            GDDualOperatorType.Mod => "%",
            GDDualOperatorType.Power => "**",
            GDDualOperatorType.BitwiseAnd => "&",
            GDDualOperatorType.BitwiseOr => "|",
            GDDualOperatorType.Xor => "^",
            GDDualOperatorType.BitShiftLeft => "<<",
            GDDualOperatorType.BitShiftRight => ">>",
            _ => null
        };
    }
}
