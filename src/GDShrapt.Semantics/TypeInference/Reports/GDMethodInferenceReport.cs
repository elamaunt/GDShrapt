using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.Semantics;

/// <summary>
/// Complete report about type inference for a method.
/// Contains parameter inference, return type inference, and dependency information.
/// </summary>
internal class GDMethodInferenceReport
{
    /// <summary>
    /// Name of the class containing this method.
    /// </summary>
    public string ClassName { get; init; } = "";

    /// <summary>
    /// Name of the method.
    /// </summary>
    public string MethodName { get; init; } = "";

    /// <summary>
    /// Full method key (ClassName.MethodName).
    /// </summary>
    public string FullKey => $"{ClassName}.{MethodName}";

    /// <summary>
    /// File path where the method is defined.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Line number where the method starts.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number where the method starts.
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Parameter inference reports (keyed by parameter name).
    /// </summary>
    public Dictionary<string, GDParameterInferenceReport> Parameters { get; init; } = new();

    /// <summary>
    /// Return type inference report.
    /// </summary>
    public GDReturnInferenceReport? ReturnTypeReport { get; init; }

    /// <summary>
    /// Dependencies to other methods (for visualization).
    /// </summary>
    public List<GDInferenceDependency> Dependencies { get; init; } = new();

    /// <summary>
    /// Whether this method is part of a cyclic dependency.
    /// </summary>
    public bool HasCyclicDependency { get; init; }

    /// <summary>
    /// Total number of call sites analyzed for this method.
    /// </summary>
    public int TotalCallSitesAnalyzed { get; init; }

    /// <summary>
    /// Total number of return statements analyzed.
    /// </summary>
    public int TotalReturnStatementsAnalyzed => ReturnTypeReport?.ReturnStatements.Count ?? 0;

    /// <summary>
    /// Number of parameters with inferred types.
    /// </summary>
    public int InferredParameterCount => Parameters.Values.Count(p => !p.HasExplicitType && p.InferredUnionType != null);

    /// <summary>
    /// Whether any types were inferred for this method.
    /// </summary>
    public bool HasInferredTypes =>
        InferredParameterCount > 0 ||
        (ReturnTypeReport != null && !ReturnTypeReport.HasExplicitType && ReturnTypeReport.InferredUnionType != null);

    /// <summary>
    /// Gets the parameter report by name.
    /// </summary>
    public GDParameterInferenceReport? GetParameter(string name)
    {
        return Parameters.TryGetValue(name, out var report) ? report : null;
    }

    /// <summary>
    /// Gets the parameter report by index.
    /// </summary>
    public GDParameterInferenceReport? GetParameterByIndex(int index)
    {
        return Parameters.Values.FirstOrDefault(p => p.ParameterIndex == index);
    }

    public override string ToString()
    {
        var paramCount = Parameters.Count;
        var inferredCount = InferredParameterCount;
        var hasReturn = ReturnTypeReport?.InferredUnionType != null;
        var cycleMarker = HasCyclicDependency ? " [CYCLE]" : "";

        return $"{FullKey}({paramCount} params, {inferredCount} inferred){(hasReturn ? " -> " + ReturnTypeReport!.EffectiveType : "")}{cycleMarker}";
    }

    /// <summary>
    /// Exports this method report to JSON format.
    /// </summary>
    public string ExportToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var exportData = new
        {
            ClassName,
            MethodName,
            FilePath,
            Line,
            Parameters = Parameters.ToDictionary(
                p => p.Key,
                p => new
                {
                    p.Value.ExplicitType,
                    InferredUnionType = p.Value.InferredUnionType != null ? new
                    {
                        Types = p.Value.InferredUnionType.Types.ToList(),
                        CommonBaseType = p.Value.InferredUnionType.CommonBaseType,
                        EffectiveType = p.Value.InferredUnionType.ToString()
                    } : null,
                    CallSites = p.Value.CallSiteArguments.Select(c => new
                    {
                        File = c.SourceFilePath,
                        c.Line,
                        Expression = c.ArgumentExpression,
                        c.InferredType,
                        c.IsDuckTyped
                    }).ToList(),
                    Confidence = p.Value.Confidence.ToString()
                }),
            ReturnType = ReturnTypeReport != null ? new
            {
                ReturnTypeReport.ExplicitType,
                InferredUnionType = ReturnTypeReport.InferredUnionType != null ? new
                {
                    Types = ReturnTypeReport.InferredUnionType.Types.ToList(),
                    EffectiveType = ReturnTypeReport.InferredUnionType.ToString()
                } : null,
                Returns = ReturnTypeReport.ReturnStatements.Select(r => new
                {
                    r.Line,
                    Expression = r.ReturnExpression,
                    r.InferredType,
                    r.IsImplicit,
                    r.BranchContext
                }).ToList()
            } : null,
            Dependencies = Dependencies.Select(d => d.ToMethod).ToList(),
            HasCyclicDependency
        };

        return JsonSerializer.Serialize(exportData, options);
    }
}
