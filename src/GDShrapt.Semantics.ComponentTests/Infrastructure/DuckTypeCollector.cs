using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Collects duck type constraints and parameter inference results for all parameters.
/// Combines model.TypeSystem.InferParameterType() and model.TypeSystem.GetDuckType().
/// </summary>
public class DuckTypeCollector
{
    public record DuckTypeEntry(
        int Line,
        int Column,
        string ParameterName,
        string MethodName,
        string InferredType,
        string Confidence,
        string? Reason,
        bool IsDuckTyped,
        bool IsUnion,
        string? RequiredMethods,
        string? RequiredProperties,
        string? RequiredSignals,
        string? PossibleTypes,
        string? ExcludedTypes,
        string? ConcreteType);

    public List<DuckTypeEntry> CollectEntries(GDScriptFile file)
    {
        var result = new List<DuckTypeEntry>();

        var model = file.SemanticModel;
        if (model == null || file.Class == null)
            return result;

        // Find all methods and their parameters
        foreach (var method in file.Class.AllNodes.OfType<GDMethodDeclaration>())
        {
            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                continue;

            if (method.Parameters == null)
                continue;

            foreach (var param in method.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                int line = GetNodeLine(param);
                int column = GetNodeColumn(param);
                if (line == 0 && column == 0)
                    continue;

                // Get parameter inference
                var inference = model.TypeSystem.InferParameterType(param);

                // Get duck type constraints
                var duckType = model.TypeSystem.GetDuckType(paramName);

                string? requiredMethods = null;
                string? requiredProperties = null;
                string? requiredSignals = null;
                string? possibleTypes = null;
                string? excludedTypes = null;
                string? concreteType = null;

                if (duckType != null && duckType.HasRequirements)
                {
                    if (duckType.RequiredMethods.Count > 0)
                        requiredMethods = string.Join(", ", duckType.RequiredMethods.Select(kv => $"{kv.Key}({kv.Value})"));

                    if (duckType.RequiredProperties.Count > 0)
                        requiredProperties = string.Join(", ", duckType.RequiredProperties.Select(kv =>
                            kv.Value != null ? $"{kv.Key}: {kv.Value.DisplayName}" : kv.Key));

                    if (duckType.RequiredSignals.Count > 0)
                        requiredSignals = string.Join(", ", duckType.RequiredSignals.OrderBy(s => s));
                }

                if (duckType != null)
                {
                    if (duckType.PossibleTypes.Count > 0)
                        possibleTypes = string.Join(", ", duckType.PossibleTypes.Select(t => t.DisplayName).OrderBy(t => t));

                    if (duckType.ExcludedTypes.Count > 0)
                        excludedTypes = string.Join(", ", duckType.ExcludedTypes.Select(t => t.DisplayName).OrderBy(t => t));

                    concreteType = duckType.ConcreteType?.DisplayName;
                }

                result.Add(new DuckTypeEntry(
                    line, column,
                    paramName,
                    methodName,
                    inference.TypeName.DisplayName,
                    inference.Confidence.ToString(),
                    inference.Reason,
                    inference.IsDuckTyped,
                    inference.IsUnion,
                    requiredMethods,
                    requiredProperties,
                    requiredSignals,
                    possibleTypes,
                    excludedTypes,
                    concreteType));
            }
        }

        return result.OrderBy(e => e.Line).ThenBy(e => e.Column).ToList();
    }

    private int GetNodeLine(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token?.StartLine ?? 0;
    }

    private int GetNodeColumn(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }
}
