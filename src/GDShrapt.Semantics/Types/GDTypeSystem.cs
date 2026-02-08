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

    /// <inheritdoc/>
    public GDSemanticType GetType(GDNode node)
    {
        var typeName = _model.GetTypeForNode(node);
        return GDSemanticType.FromRuntimeTypeName(typeName);
    }

    /// <inheritdoc/>
    public GDSemanticType GetType(GDExpression expr)
    {
        // Try direct semantic type first (bypasses string serialization)
        var semanticType = _model.GetSemanticTypeForExpression(expr);
        if (semanticType != null)
            return semanticType;

        var typeNode = _model.GetTypeNodeForExpression(expr);
        if (typeNode != null)
            return GDSemanticType.FromTypeNode(typeNode);

        var typeName = _model.GetTypeForNode(expr);
        return GDSemanticType.FromRuntimeTypeName(typeName);
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

    /// <inheritdoc/>
    public GDUnionType? GetUnionType(string symbolName)
    {
        return _model.GetUnionType(symbolName);
    }

    /// <inheritdoc/>
    public GDDuckType? GetDuckType(string variableName)
    {
        return _model.GetDuckType(variableName);
    }

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
            return true;

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

    /// <inheritdoc/>
    public GDTypeInfo? GetTypeInfo(string variableName, GDNode? atLocation = null)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        if (atLocation != null)
        {
            var narrowedType = _model.GetNarrowedType(variableName, atLocation);
            if (!string.IsNullOrEmpty(narrowedType))
            {
                return new GDTypeInfo
                {
                    NarrowedType = GDSemanticType.FromRuntimeTypeName(narrowedType),
                    Confidence = GDTypeConfidence.Certain
                };
            }
        }

        var containerProfile = _model.GetContainerProfile(variableName);
        if (containerProfile != null)
        {
            var containerTypeName = containerProfile.IsDictionary ? "Dictionary" : "Array";
            return new GDTypeInfo
            {
                InferredType = GDSemanticType.FromRuntimeTypeName(containerTypeName),
                ContainerInfo = containerProfile.ComputeInferredType(),
                Confidence = GDTypeConfidence.High
            };
        }

        var symbol = _model.FindSymbol(variableName);
        if (symbol != null)
        {
            return new GDTypeInfo
            {
                DeclaredSemanticType = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDSemanticType.FromRuntimeTypeName(symbol.TypeName)
                    : null,
                InferredType = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDSemanticType.FromRuntimeTypeName(symbol.TypeName)
                    : GDVariantSemanticType.Instance,
                Confidence = !string.IsNullOrEmpty(symbol.TypeName)
                    ? GDTypeConfidence.Certain
                    : GDTypeConfidence.Unknown
            };
        }

        return null;
    }

    /// <inheritdoc/>
    public GDTypeInfo GetExpressionTypeInfo(GDExpression expression)
    {
        if (expression == null)
        {
            return new GDTypeInfo
            {
                InferredType = GDVariantSemanticType.Instance,
                Confidence = GDTypeConfidence.Unknown
            };
        }

        var semanticType = GetType(expression);
        return new GDTypeInfo
        {
            InferredType = semanticType,
            Confidence = semanticType.IsVariant ? GDTypeConfidence.Unknown : GDTypeConfidence.High
        };
    }

    /// <inheritdoc/>
    public GDInferredParameterType InferParameterType(GDParameterDeclaration param)
    {
        return _model.InferParameterType(param);
    }

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
