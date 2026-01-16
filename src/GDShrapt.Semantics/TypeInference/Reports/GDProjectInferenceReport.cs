using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.Semantics;

/// <summary>
/// Complete project-wide report about type inference.
/// Contains all method reports, dependency graph, and statistics.
/// </summary>
public class GDProjectInferenceReport
{
    /// <summary>
    /// Project name or identifier.
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// Timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// All method inference reports (keyed by method key).
    /// </summary>
    public Dictionary<string, GDMethodInferenceReport> Methods { get; init; } = new();

    /// <summary>
    /// The dependency graph for visualization.
    /// </summary>
    public GDInferenceDependencyGraph? DependencyGraph { get; init; }

    /// <summary>
    /// All detected cycles (each cycle is a list of method keys).
    /// </summary>
    public List<List<string>> DetectedCycles { get; init; } = new();

    /// <summary>
    /// Total number of methods analyzed.
    /// </summary>
    public int TotalMethodsAnalyzed => Methods.Count;

    /// <summary>
    /// Number of methods with inferred parameter types.
    /// </summary>
    public int MethodsWithInferredParameters => Methods.Values.Count(m => m.InferredParameterCount > 0);

    /// <summary>
    /// Number of methods with inferred return types.
    /// </summary>
    public int MethodsWithInferredReturnType => Methods.Values.Count(m =>
        m.ReturnTypeReport != null &&
        !m.ReturnTypeReport.HasExplicitType &&
        m.ReturnTypeReport.InferredUnionType != null);

    /// <summary>
    /// Number of methods with cyclic dependencies.
    /// </summary>
    public int MethodsWithCyclicDependencies => Methods.Values.Count(m => m.HasCyclicDependency);

    /// <summary>
    /// Total number of parameters analyzed.
    /// </summary>
    public int TotalParametersAnalyzed => Methods.Values.Sum(m => m.Parameters.Count);

    /// <summary>
    /// Total number of call sites analyzed.
    /// </summary>
    public int TotalCallSitesAnalyzed => Methods.Values.Sum(m => m.TotalCallSitesAnalyzed);

    /// <summary>
    /// Gets a method report by key.
    /// </summary>
    public GDMethodInferenceReport? GetMethod(string methodKey)
    {
        return Methods.TryGetValue(methodKey, out var report) ? report : null;
    }

    /// <summary>
    /// Gets a method report by class and method name.
    /// </summary>
    public GDMethodInferenceReport? GetMethod(string className, string methodName)
    {
        return GetMethod($"{className}.{methodName}");
    }

    /// <summary>
    /// Gets all methods in a class.
    /// </summary>
    public IEnumerable<GDMethodInferenceReport> GetMethodsInClass(string className)
    {
        return Methods.Values.Where(m => m.ClassName == className);
    }

    /// <summary>
    /// Gets all methods that are part of cycles.
    /// </summary>
    public IEnumerable<GDMethodInferenceReport> GetCyclicMethods()
    {
        return Methods.Values.Where(m => m.HasCyclicDependency);
    }

    /// <summary>
    /// Gets statistics summary.
    /// </summary>
    public GDInferenceStatistics GetStatistics()
    {
        return new GDInferenceStatistics
        {
            TotalMethods = TotalMethodsAnalyzed,
            MethodsWithInferredParams = MethodsWithInferredParameters,
            MethodsWithInferredReturn = MethodsWithInferredReturnType,
            CyclesDetected = DetectedCycles.Count,
            MethodsInCycles = MethodsWithCyclicDependencies,
            TotalParameters = TotalParametersAnalyzed,
            TotalCallSites = TotalCallSitesAnalyzed
        };
    }

    /// <summary>
    /// Exports the report to JSON format.
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
            Project = ProjectName,
            GeneratedAt,
            Statistics = GetStatistics(),
            Methods = Methods.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    kvp.Value.ClassName,
                    kvp.Value.MethodName,
                    kvp.Value.FilePath,
                    kvp.Value.Line,
                    Parameters = kvp.Value.Parameters.ToDictionary(
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
                    ReturnType = kvp.Value.ReturnTypeReport != null ? new
                    {
                        kvp.Value.ReturnTypeReport.ExplicitType,
                        InferredUnionType = kvp.Value.ReturnTypeReport.InferredUnionType != null ? new
                        {
                            Types = kvp.Value.ReturnTypeReport.InferredUnionType.Types.ToList(),
                            EffectiveType = kvp.Value.ReturnTypeReport.InferredUnionType.ToString()
                        } : null,
                        Returns = kvp.Value.ReturnTypeReport.ReturnStatements.Select(r => new
                        {
                            r.Line,
                            Expression = r.ReturnExpression,
                            r.InferredType,
                            r.IsImplicit,
                            r.BranchContext
                        }).ToList()
                    } : null,
                    Dependencies = kvp.Value.Dependencies.Select(d => $"{d.ToMethod}").ToList(),
                    kvp.Value.HasCyclicDependency
                }),
            DependencyGraph = DependencyGraph != null ? new
            {
                Nodes = DependencyGraph.Nodes.Select(n => new
                {
                    n.MethodKey,
                    n.ClassName,
                    n.MethodName,
                    n.HasCyclicDependency,
                    n.InDegree,
                    n.OutDegree
                }).ToList(),
                Edges = DependencyGraph.Edges.Select(e => new
                {
                    e.FromMethod,
                    e.ToMethod,
                    Kind = e.Kind.ToString(),
                    e.IsPartOfCycle
                }).ToList()
            } : null,
            Cycles = DetectedCycles
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    public override string ToString()
    {
        var stats = GetStatistics();
        return $"InferenceReport: {stats.TotalMethods} methods, {stats.MethodsWithInferredParams} with inferred params, {stats.CyclesDetected} cycles";
    }
}

/// <summary>
/// Statistics about type inference.
/// </summary>
public class GDInferenceStatistics
{
    public int TotalMethods { get; init; }
    public int MethodsWithInferredParams { get; init; }
    public int MethodsWithInferredReturn { get; init; }
    public int CyclesDetected { get; init; }
    public int MethodsInCycles { get; init; }
    public int TotalParameters { get; init; }
    public int TotalCallSites { get; init; }
}
