using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for providing inference data to UI/plugins.
/// Provides tooltip data, call site information, and dependency graph visualization.
/// </summary>
internal class GDInferenceVisualizationService
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider _runtimeProvider;
    private GDMethodSignatureInferenceEngine? _engine;
    private GDProjectInferenceReport? _cachedReport;

    /// <summary>
    /// Creates a new visualization service for a project.
    /// </summary>
    public GDInferenceVisualizationService(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _runtimeProvider = project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Creates a new visualization service with a custom runtime provider.
    /// </summary>
    public GDInferenceVisualizationService(GDScriptProject project, IGDRuntimeProvider runtimeProvider)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _runtimeProvider = runtimeProvider ?? throw new ArgumentNullException(nameof(runtimeProvider));
    }

    #region Report Access

    /// <summary>
    /// Gets the project inference report, generating it if necessary.
    /// </summary>
    public GDProjectInferenceReport GetProjectReport()
    {
        if (_cachedReport != null)
            return _cachedReport;

        EnsureEngine();
        _cachedReport = _engine!.GetProjectReport();
        return _cachedReport;
    }

    /// <summary>
    /// Gets the method inference report for a specific method.
    /// </summary>
    public GDMethodInferenceReport? GetMethodReport(string className, string methodName)
    {
        EnsureEngine();
        return _engine!.GetMethodReport(className, methodName);
    }

    /// <summary>
    /// Invalidates the cached report, forcing regeneration on next access.
    /// Call this when the project changes.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedReport = null;
        _engine = null;
    }

    #endregion

    #region Tooltip Data

    /// <summary>
    /// Gets parameter inference data for a hover tooltip.
    /// </summary>
    /// <param name="filePath">Full path to the script file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (0-based).</param>
    /// <returns>Parameter inference report if a parameter is at the location, null otherwise.</returns>
    public GDParameterInferenceReport? GetParameterTooltipData(string filePath, int line, int column)
    {
        var script = GetScriptByPath(filePath);
        if (script?.Class == null)
            return null;

        // Find the parameter at the given position
        var finder = new GDPositionFinder(script.Class);
        var identifier = finder.FindIdentifierAtPosition(line, column);
        if (identifier == null)
            return null;

        // Check if it's a parameter
        var parameter = FindParentOfType<GDParameterDeclaration>(identifier);
        if (parameter == null)
            return null;

        var paramName = parameter.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return null;

        // Find the containing method
        var method = FindParentOfType<GDMethodDeclaration>(parameter);
        if (method == null)
            return null;

        var methodName = method.Identifier?.Sequence;
        var className = script.TypeName ?? script.Reference?.ResourcePath ?? "unknown";

        if (string.IsNullOrEmpty(methodName))
            return null;

        // Get the method report
        var methodReport = GetMethodReport(className, methodName);
        return methodReport?.GetParameter(paramName);
    }

    /// <summary>
    /// Gets return type inference data for a hover tooltip.
    /// </summary>
    /// <param name="filePath">Full path to the script file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (0-based).</param>
    /// <returns>Return inference report if a return statement is at the location, null otherwise.</returns>
    public GDReturnInferenceReport? GetReturnTypeTooltipData(string filePath, int line, int column)
    {
        var script = GetScriptByPath(filePath);
        if (script?.Class == null)
            return null;

        // Find the return keyword at the given position
        var finder = new GDPositionFinder(script.Class);
        var node = finder.FindNodeAtPosition(line, column);

        // Check if we're in a method with a return type annotation
        var method = FindParentOfType<GDMethodDeclaration>(node);
        if (method == null)
            return null;

        var methodName = method.Identifier?.Sequence;
        var className = script.TypeName ?? script.Reference?.ResourcePath ?? "unknown";

        if (string.IsNullOrEmpty(methodName))
            return null;

        // Get the method report
        var methodReport = GetMethodReport(className, methodName);
        return methodReport?.ReturnTypeReport;
    }

    /// <summary>
    /// Gets full method inference data for a hover tooltip.
    /// </summary>
    public GDMethodInferenceReport? GetMethodTooltipData(string filePath, int line, int column)
    {
        var script = GetScriptByPath(filePath);
        if (script?.Class == null)
            return null;

        // Find the method at the given position
        var finder = new GDPositionFinder(script.Class);
        var identifier = finder.FindIdentifierAtPosition(line, column);
        if (identifier == null)
            return null;

        // Check if it's a method declaration
        var method = FindParentOfType<GDMethodDeclaration>(identifier);
        if (method?.Identifier != identifier)
            return null;

        var methodName = method.Identifier?.Sequence;
        var className = script.TypeName ?? script.Reference?.ResourcePath ?? "unknown";

        if (string.IsNullOrEmpty(methodName))
            return null;

        return GetMethodReport(className, methodName);
    }

    #endregion

    #region Call Site Information

    /// <summary>
    /// Gets all call sites for a parameter (for "Find All References" extension).
    /// </summary>
    public IEnumerable<GDCallSiteArgumentReport> GetParameterCallSites(
        string className,
        string methodName,
        string parameterName)
    {
        var methodReport = GetMethodReport(className, methodName);
        var paramReport = methodReport?.GetParameter(parameterName);

        return paramReport?.CallSiteArguments ?? Enumerable.Empty<GDCallSiteArgumentReport>();
    }

    /// <summary>
    /// Gets all call sites for a method.
    /// </summary>
    public IEnumerable<GDCallSiteArgumentReport> GetMethodCallSites(string className, string methodName)
    {
        var methodReport = GetMethodReport(className, methodName);
        if (methodReport == null)
            return Enumerable.Empty<GDCallSiteArgumentReport>();

        // Collect all call sites from all parameters
        return methodReport.Parameters.Values
            .SelectMany(p => p.CallSiteArguments)
            .GroupBy(c => new { c.SourceFilePath, c.Line })
            .Select(g => g.First());
    }

    #endregion

    #region Dependency Graph

    /// <summary>
    /// Gets the full dependency graph for visualization.
    /// </summary>
    public GDInferenceDependencyGraph? GetDependencyGraph()
    {
        var report = GetProjectReport();
        return report.DependencyGraph;
    }

    /// <summary>
    /// Gets all cycles that contain a specific method.
    /// </summary>
    public IEnumerable<List<string>> GetCyclesContainingMethod(string methodKey)
    {
        var report = GetProjectReport();
        return report.DetectedCycles
            .Where(cycle => cycle.Contains(methodKey));
    }

    /// <summary>
    /// Gets methods that depend on a given method (incoming edges).
    /// </summary>
    public IEnumerable<string> GetMethodDependents(string methodKey)
    {
        var graph = GetDependencyGraph();
        if (graph == null)
            return Enumerable.Empty<string>();
        return graph.Edges
            .Where(e => e.ToMethod == methodKey)
            .Select(e => e.FromMethod)
            .Distinct();
    }

    /// <summary>
    /// Gets methods that a given method depends on (outgoing edges).
    /// </summary>
    public IEnumerable<string> GetMethodDependencies(string methodKey)
    {
        var graph = GetDependencyGraph();
        if (graph == null)
            return Enumerable.Empty<string>();

        return graph.Edges
            .Where(e => e.FromMethod == methodKey)
            .Select(e => e.ToMethod)
            .Distinct();
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets statistics about inference in the project.
    /// </summary>
    public GDInferenceStatistics GetStatistics()
    {
        var report = GetProjectReport();
        return report.GetStatistics();
    }

    #endregion

    #region Export

    /// <summary>
    /// Exports the project inference report to JSON format.
    /// </summary>
    public string ExportToJson()
    {
        var report = GetProjectReport();
        return report.ExportToJson();
    }

    /// <summary>
    /// Exports a specific method report to JSON format.
    /// </summary>
    public string? ExportMethodToJson(string className, string methodName)
    {
        var report = GetMethodReport(className, methodName);
        return report?.ExportToJson();
    }

    #endregion

    #region Helper Methods

    private void EnsureEngine()
    {
        if (_engine == null)
        {
            _engine = new GDMethodSignatureInferenceEngine(_project, _runtimeProvider);
        }
    }

    private GDScriptFile? GetScriptByPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        return _project.ScriptFiles.FirstOrDefault(s =>
            s.Reference?.FullPath != null &&
            s.Reference.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
    }

    private static T? FindParentOfType<T>(GDSyntaxToken? token) where T : GDNode
    {
        var current = token?.Parent;
        while (current != null)
        {
            if (current is T result)
                return result;
            current = current.Parent;
        }
        return null;
    }

    private static T? FindParentOfType<T>(GDNode? node) where T : GDNode
    {
        var current = node?.Parent;
        while (current != null)
        {
            if (current is T result)
                return result;
            current = current.Parent;
        }
        return null;
    }

    #endregion
}
