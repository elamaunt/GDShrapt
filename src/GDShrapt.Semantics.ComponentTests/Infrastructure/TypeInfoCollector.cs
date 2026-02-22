namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Collects rich type information for all declarations using model.TypeSystem.GetTypeInfo().
/// Covers: declared type, inferred type, effective type, confidence, nullability, union, container.
/// </summary>
public class TypeInfoCollector
{
    public record TypeInfoEntry(
        int Line,
        int Column,
        string Name,
        string SymbolKind,
        string? DeclaredType,
        string InferredType,
        string EffectiveType,
        string Confidence,
        bool IsNullable,
        bool IsPotentiallyNull,
        bool IsUnionType,
        string? UnionMembers,
        string? ContainerInfo);

    public List<TypeInfoEntry> CollectEntries(GDScriptFile file)
    {
        var result = new List<TypeInfoEntry>();

        var model = file.SemanticModel;
        if (model == null || file.Class == null)
            return result;

        foreach (var symbol in model.Symbols)
        {
            // Only collect declarations that have meaningful type info
            if (symbol.Kind != GDShrapt.Abstractions.GDSymbolKind.Variable &&
                symbol.Kind != GDShrapt.Abstractions.GDSymbolKind.Parameter &&
                symbol.Kind != GDShrapt.Abstractions.GDSymbolKind.Constant &&
                symbol.Kind != GDShrapt.Abstractions.GDSymbolKind.Property &&
                symbol.Kind != GDShrapt.Abstractions.GDSymbolKind.Iterator)
                continue;

            var declNode = symbol.DeclarationNode;
            int line = GetNodeLine(declNode);
            int column = GetNodeColumn(declNode);

            if (line == 0 && column == 0)
                continue;

            // Get rich type info
            var typeInfo = model.TypeSystem.GetTypeInfo(symbol.Name);
            var containerInfo = model.TypeSystem.GetContainerElementType(symbol.Name);

            string? declaredType = typeInfo?.DeclaredTypeName;
            string inferredType = typeInfo?.InferredType?.DisplayName ?? "Variant";
            string effectiveType = typeInfo?.EffectiveType.DisplayName ?? "Variant";
            string confidence = typeInfo?.Confidence.ToString() ?? "Unknown";
            bool isNullable = typeInfo?.IsNullable ?? false;
            bool isPotentiallyNull = typeInfo?.IsPotentiallyNull ?? false;
            bool isUnionType = typeInfo?.IsUnionType ?? false;

            string? unionMembers = null;
            if (isUnionType && typeInfo?.UnionMembers != null)
            {
                unionMembers = string.Join("|", typeInfo.UnionMembers.Select(m => m.DisplayName));
            }

            string? containerInfoStr = null;
            if (containerInfo != null && containerInfo.HasElementTypes)
            {
                if (containerInfo.IsDictionary)
                {
                    containerInfoStr = $"Dictionary[{containerInfo.EffectiveKeyType?.DisplayName ?? "Variant"}, {containerInfo.EffectiveElementType.DisplayName}]";
                    if (containerInfo.IsHomogeneous)
                        containerInfoStr += " (homogeneous)";
                }
                else
                {
                    containerInfoStr = $"Array[{containerInfo.EffectiveElementType}]";
                    if (containerInfo.IsHomogeneous)
                        containerInfoStr += " (homogeneous)";
                }
            }

            result.Add(new TypeInfoEntry(
                line, column,
                symbol.Name,
                symbol.Kind.ToString(),
                declaredType,
                inferredType,
                effectiveType,
                confidence,
                isNullable,
                isPotentiallyNull,
                isUnionType,
                unionMembers,
                containerInfoStr));
        }

        return result.OrderBy(e => e.Line).ThenBy(e => e.Column).ToList();
    }

    private int GetNodeLine(GDShrapt.Reader.GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token != null ? token.StartLine + 1 : 0;
    }

    private int GetNodeColumn(GDShrapt.Reader.GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }
}
