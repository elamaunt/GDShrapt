using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Main orchestrator for cross-file parameter and return type inference.
/// Coordinates call site collection, return type analysis, and cycle detection
/// to infer Union types for method signatures.
/// </summary>
internal class GDMethodSignatureInferenceEngine
{
    private readonly GDScriptProject _project;
    private readonly GDCallSiteCollector _callSiteCollector;
    private readonly GDInferenceCycleDetector _cycleDetector;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    // Cached inference results
    private readonly Dictionary<string, GDMethodInferenceReport> _methodReports = new();
    private GDProjectInferenceReport? _projectReport;
    private bool _isBuilt;

    /// <summary>
    /// Creates a new method signature inference engine.
    /// </summary>
    public GDMethodSignatureInferenceEngine(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _callSiteCollector = new GDCallSiteCollector(project);
        _cycleDetector = new GDInferenceCycleDetector(project);
        _runtimeProvider = project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Creates a new method signature inference engine with a custom runtime provider.
    /// </summary>
    public GDMethodSignatureInferenceEngine(GDScriptProject project, IGDRuntimeProvider runtimeProvider)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _runtimeProvider = runtimeProvider ?? throw new ArgumentNullException(nameof(runtimeProvider));
        _callSiteCollector = new GDCallSiteCollector(project);
        _cycleDetector = new GDInferenceCycleDetector(project, runtimeProvider);
    }

    /// <summary>
    /// Builds inference for all methods in the project.
    /// </summary>
    public void BuildAll()
    {
        if (_isBuilt)
            return;

        // Step 1: Build dependency graph and detect cycles
        _cycleDetector.BuildDependencyGraph();
        _cycleDetector.DetectCycles();

        // Step 2: Get inference order (topological sort with cycle handling)
        var inferenceOrder = _cycleDetector.GetInferenceOrder().ToList();

        // Step 3: Infer types for each method in order
        foreach (var (methodKey, inCycle) in inferenceOrder)
        {
            var parts = methodKey.Split('.');
            if (parts.Length < 2) continue;

            var typeName = parts[0];
            var methodName = parts[1];

            var report = InferMethodSignatureInternal(typeName, methodName, inCycle);
            if (report != null)
            {
                _methodReports[methodKey] = report;
            }
        }

        // Step 4: Build project report
        _projectReport = BuildProjectReport();
        _isBuilt = true;
    }

    /// <summary>
    /// Gets the inference report for a specific method.
    /// </summary>
    public GDMethodInferenceReport? GetMethodReport(string className, string methodName)
    {
        if (!_isBuilt)
            BuildAll();

        var key = $"{className}.{methodName}";
        return _methodReports.TryGetValue(key, out var report) ? report : null;
    }

    /// <summary>
    /// Gets the full project inference report.
    /// </summary>
    public GDProjectInferenceReport GetProjectReport()
    {
        if (!_isBuilt)
            BuildAll();

        return _projectReport!;
    }

    /// <summary>
    /// Infers the Union type for a specific parameter.
    /// </summary>
    public GDUnionType? InferParameterType(string className, string methodName, string parameterName)
    {
        var report = GetMethodReport(className, methodName);
        return report?.GetParameter(parameterName)?.InferredUnionType;
    }

    /// <summary>
    /// Infers the Union type for a method's return type.
    /// </summary>
    public GDUnionType? InferReturnType(string className, string methodName)
    {
        var report = GetMethodReport(className, methodName);
        return report?.ReturnTypeReport?.InferredUnionType;
    }

    /// <summary>
    /// Checks if a method is part of an inference cycle.
    /// </summary>
    public bool IsMethodInCycle(string className, string methodName)
    {
        return _cycleDetector.IsInCycle($"{className}.{methodName}");
    }

    /// <summary>
    /// Invalidates the cache, forcing a rebuild on next access.
    /// </summary>
    public void Invalidate()
    {
        _methodReports.Clear();
        _projectReport = null;
        _isBuilt = false;
    }

    private GDMethodInferenceReport? InferMethodSignatureInternal(string typeName, string methodName, bool inCycle)
    {
        // Find the method declaration
        var scriptInfo = GetScriptInfo(typeName);
        if (scriptInfo?.Class == null)
            return null;

        var method = scriptInfo.Class.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return null;

        var methodKey = $"{typeName}.{methodName}";

        // Build parameter reports
        var parameterReports = new Dictionary<string, GDParameterInferenceReport>();
        if (method.Parameters != null)
        {
            var callSites = _callSiteCollector.CollectCallSites(typeName, methodName);

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var param = method.Parameters.ElementAt(i);
                if (param.Identifier == null) continue;

                var paramName = param.Identifier.Sequence;
                var explicitType = param.Type?.BuildName();
                var hasExplicit = !string.IsNullOrEmpty(explicitType) && explicitType != "Variant";

                // Build parameter report
                var paramReport = BuildParameterReport(
                    paramName,
                    i,
                    explicitType,
                    hasExplicit,
                    callSites,
                    inCycle);

                parameterReports[paramName] = paramReport;
            }
        }

        // Build return type report
        var returnReport = BuildReturnReport(method, inCycle);

        // Get dependencies
        var dependencies = _cycleDetector.GetDependencies(methodKey).ToList();

        // Get method location
        var firstToken = method.AllTokens.FirstOrDefault();

        return new GDMethodInferenceReport
        {
            ClassName = typeName,
            MethodName = methodName,
            FilePath = scriptInfo.FullPath,
            Line = firstToken?.StartLine ?? 0,
            Column = firstToken?.StartColumn ?? 0,
            Parameters = parameterReports,
            ReturnTypeReport = returnReport,
            Dependencies = dependencies,
            HasCyclicDependency = inCycle,
            TotalCallSitesAnalyzed = parameterReports.Values.Sum(p => p.CallSiteCount)
        };
    }

    private GDParameterInferenceReport BuildParameterReport(
        string paramName,
        int paramIndex,
        string? explicitType,
        bool hasExplicit,
        IReadOnlyList<GDCallSiteInfo> callSites,
        bool inCycle)
    {
        // If has explicit type, no need to infer
        if (hasExplicit)
        {
            return new GDParameterInferenceReport
            {
                ParameterName = paramName,
                ParameterIndex = paramIndex,
                ExplicitType = explicitType,
                Confidence = GDReferenceConfidence.Strict
            };
        }

        // Build Union type from call sites
        var union = new GDUnionType();
        var callSiteReports = new List<GDCallSiteArgumentReport>();

        foreach (var callSite in callSites)
        {
            var arg = callSite.GetArgument(paramIndex);
            if (arg == null) continue;

            // Add to union
            if (!string.IsNullOrEmpty(arg.InferredType))
            {
                union.AddType(arg.InferredType, arg.IsHighConfidence);
            }

            // Create report
            callSiteReports.Add(GDCallSiteArgumentReport.FromCallSite(callSite, arg));
        }

        // Determine confidence
        var confidence = DetermineConfidence(callSiteReports, inCycle);
        var confidenceReason = GetConfidenceReason(callSiteReports, inCycle);

        return new GDParameterInferenceReport
        {
            ParameterName = paramName,
            ParameterIndex = paramIndex,
            ExplicitType = null,
            InferredUnionType = union.IsEmpty ? null : union,
            CallSiteArguments = callSiteReports,
            Confidence = confidence,
            ConfidenceReason = confidenceReason
        };
    }

    private GDReturnInferenceReport BuildReturnReport(GDMethodDeclaration method, bool inCycle)
    {
        // Check for explicit return type
        var explicitType = method.ReturnType?.BuildName();
        var hasExplicit = !string.IsNullOrEmpty(explicitType) && explicitType != "Variant";

        if (hasExplicit)
        {
            return new GDReturnInferenceReport
            {
                ExplicitType = explicitType,
                Confidence = GDReferenceConfidence.Strict
            };
        }

        // Collect return statements
        var collector = new GDReturnTypeCollector(method, _runtimeProvider);
        collector.Collect();

        // Build reports
        var returnStatementReports = collector.Returns
            .Select(GDReturnStatementReport.FromReturnInfo)
            .ToList();

        // Compute union type
        var union = collector.ComputeReturnUnionType();

        // Determine confidence
        var confidence = inCycle ? GDReferenceConfidence.Potential :
            (union.AllHighConfidence ? GDReferenceConfidence.Strict : GDReferenceConfidence.Potential);

        return new GDReturnInferenceReport
        {
            ExplicitType = null,
            InferredUnionType = union.IsEmpty ? null : union,
            ReturnStatements = returnStatementReports,
            Confidence = confidence
        };
    }

    private GDReferenceConfidence DetermineConfidence(List<GDCallSiteArgumentReport> callSites, bool inCycle)
    {
        if (inCycle)
            return GDReferenceConfidence.Potential;

        if (!callSites.Any())
            return GDReferenceConfidence.Potential;

        // If any duck-typed, lower confidence
        if (callSites.Any(c => c.IsDuckTyped))
            return GDReferenceConfidence.Potential;

        // If all high confidence, return strict
        if (callSites.All(c => c.IsHighConfidence))
            return GDReferenceConfidence.Strict;

        return GDReferenceConfidence.Potential;
    }

    private string? GetConfidenceReason(List<GDCallSiteArgumentReport> callSites, bool inCycle)
    {
        if (inCycle)
            return "Method is part of an inference cycle";

        if (!callSites.Any())
            return "No call sites found";

        var duckTypedCount = callSites.Count(c => c.IsDuckTyped);
        if (duckTypedCount > 0)
            return $"{duckTypedCount} duck-typed call site(s)";

        var lowConfidenceCount = callSites.Count(c => !c.IsHighConfidence);
        if (lowConfidenceCount > 0)
            return $"{lowConfidenceCount} low confidence call site(s)";

        return null;
    }

    private GDProjectInferenceReport BuildProjectReport()
    {
        var cycles = _cycleDetector.DetectedCycles.ToList();
        var graph = GDInferenceDependencyGraph.FromCycleDetector(_cycleDetector);

        return new GDProjectInferenceReport
        {
            ProjectName = _project.ProjectPath,
            GeneratedAt = DateTime.UtcNow,
            Methods = new Dictionary<string, GDMethodInferenceReport>(_methodReports),
            DependencyGraph = graph,
            DetectedCycles = cycles
        };
    }

    private IGDScriptInfo? GetScriptInfo(string typeName)
    {
        if (_runtimeProvider is GDCompositeRuntimeProvider composite)
        {
            return composite.ProjectTypesProvider?.GetScriptInfoForType(typeName);
        }
        return null;
    }
}

/// <summary>
/// Result of inferring a method signature.
/// </summary>
internal class GDInferredMethodSignature
{
    /// <summary>
    /// The method name.
    /// </summary>
    public string MethodName { get; init; } = "";

    /// <summary>
    /// Inferred parameter types (parameter name -> Union type).
    /// </summary>
    public Dictionary<string, GDUnionType> ParameterTypes { get; init; } = new();

    /// <summary>
    /// Inferred return type.
    /// </summary>
    public GDUnionType? ReturnType { get; init; }

    /// <summary>
    /// Whether this method has a cyclic dependency.
    /// </summary>
    public bool HasCyclicDependency { get; init; }

    /// <summary>
    /// Creates from a method inference report.
    /// </summary>
    public static GDInferredMethodSignature FromReport(GDMethodInferenceReport report)
    {
        return new GDInferredMethodSignature
        {
            MethodName = report.MethodName,
            ParameterTypes = report.Parameters.Values
                .Where(p => p.InferredUnionType != null)
                .ToDictionary(p => p.ParameterName, p => p.InferredUnionType!),
            ReturnType = report.ReturnTypeReport?.InferredUnionType,
            HasCyclicDependency = report.HasCyclicDependency
        };
    }
}
