using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-level type system implementation.
/// Provides cross-file type resolution by delegating to file-level type systems.
/// </summary>
public class GDProjectTypeSystem : IGDProjectTypeSystem
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly Lazy<IGDRuntimeProvider?> _runtimeProvider;

    public GDProjectTypeSystem(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new ArgumentNullException(nameof(projectModel));
        _runtimeProvider = new Lazy<IGDRuntimeProvider?>(() => _projectModel.Project.CreateRuntimeProvider());
    }

    /// <inheritdoc />
    public IGDRuntimeProvider? RuntimeProvider => _runtimeProvider.Value;

    /// <inheritdoc />
    public GDSemanticType GetType(GDNode node)
    {
        if (node == null)
            return GDVariantSemanticType.Instance;

        var file = _projectModel.FindFileContaining(node);
        if (file == null)
            return GDVariantSemanticType.Instance;

        var model = _projectModel.GetSemanticModel(file);
        return model?.TypeSystem.GetType(node) ?? GDVariantSemanticType.Instance;
    }

    /// <inheritdoc />
    public GDTypeInfo? GetTypeInfo(GDNode node)
    {
        if (node == null)
            return null;

        var file = _projectModel.FindFileContaining(node);
        if (file == null)
            return null;

        var model = _projectModel.GetSemanticModel(file);
        if (model == null)
            return null;

        if (node is GDExpression expr)
            return model.TypeSystem.GetExpressionTypeInfo(expr);

        if (node is GDIdentifierExpression identifierExpr)
        {
            var variableName = identifierExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(variableName))
                return model.TypeSystem.GetTypeInfo(variableName, node);
        }

        return null;
    }

    /// <inheritdoc />
    public GDRuntimeTypeInfo? ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        var provider = RuntimeProvider;
        if (provider == null)
            return null;

        return provider.GetTypeInfo(typeName);
    }

    /// <inheritdoc />
    public IGDTypeSystem? GetFileTypeSystem(GDScriptFile file)
    {
        if (file == null)
            return null;

        var model = _projectModel.GetSemanticModel(file);
        return model?.TypeSystem;
    }

    /// <inheritdoc />
    public IGDTypeSystem? GetFileTypeSystem(GDNode node)
    {
        if (node == null)
            return null;

        var file = _projectModel.FindFileContaining(node);
        return file != null ? GetFileTypeSystem(file) : null;
    }

    /// <inheritdoc />
    public bool AreTypesCompatible(string sourceType, string targetType)
    {
        return IsAssignableTo(sourceType, targetType);
    }

    /// <inheritdoc />
    public bool IsAssignableTo(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        if (sourceType == targetType)
            return true;

        if (targetType == "Variant")
            return true;

        var provider = RuntimeProvider;
        if (provider != null)
            return provider.IsAssignableTo(sourceType, targetType);

        return false;
    }

    /// <inheritdoc />
    public string? FindCommonBaseType(params string[] types)
    {
        if (types == null || types.Length == 0)
            return null;

        if (types.Length == 1)
            return types[0];

        var provider = RuntimeProvider;
        if (provider == null)
            return "Variant";

        var validTypes = types.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
        if (validTypes.Count == 0)
            return null;

        if (validTypes.Count == 1)
            return validTypes[0];

        if (validTypes.All(t => t == validTypes[0]))
            return validTypes[0];

        foreach (var candidateBase in validTypes)
        {
            var allAssignable = validTypes.All(t =>
                t == candidateBase || provider.IsAssignableTo(t, candidateBase));

            if (allAssignable)
                return candidateBase;
        }

        var firstType = validTypes[0];
        var firstTypeInfo = provider.GetTypeInfo(firstType);
        if (firstTypeInfo?.BaseType != null)
        {
            var baseType = firstTypeInfo.BaseType;
            var allCompatible = validTypes.All(t => provider.IsAssignableTo(t, baseType));
            if (allCompatible)
                return baseType;
        }

        return "Variant";
    }
}
