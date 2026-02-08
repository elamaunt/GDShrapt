using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Profile of Callable parameter usage within a method.
/// Tracks call sites on Callable-typed parameters (e.g., callback.call(42)).
/// </summary>
internal class GDMethodCallableProfile
{
    /// <summary>
    /// Unique key for this method (ClassName.MethodName).
    /// </summary>
    public string MethodKey { get; }

    /// <summary>
    /// Name of the class containing this method.
    /// </summary>
    public string? ClassName { get; }

    /// <summary>
    /// Name of the method.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The source file containing this method.
    /// </summary>
    public GDScriptFile? SourceFile { get; }

    /// <summary>
    /// Maps Callable parameter names to their call sites within this method.
    /// Key: parameter name (e.g., "callback")
    /// Value: list of call sites (e.g., callback.call(42), callback.call("hello"))
    /// </summary>
    public Dictionary<string, List<GDCallableCallSiteInfo>> ParameterCallSites { get; } = new();

    /// <summary>
    /// Maps Callable parameter names to class variables they are assigned to.
    /// Used for tracking: self._callback = callback
    /// </summary>
    public Dictionary<string, string> ParameterToClassVarAssignments { get; } = new();

    /// <summary>
    /// Callable parameter indices by name.
    /// </summary>
    public Dictionary<string, int> CallableParameterIndices { get; } = new();

    public GDMethodCallableProfile(
        string methodKey,
        string? className,
        string methodName,
        GDScriptFile? sourceFile)
    {
        MethodKey = methodKey;
        ClassName = className;
        MethodName = methodName;
        SourceFile = sourceFile;
    }

    /// <summary>
    /// Adds a call site for a Callable parameter.
    /// </summary>
    public void AddParameterCallSite(string parameterName, GDCallableCallSiteInfo callSite)
    {
        if (string.IsNullOrEmpty(parameterName) || callSite == null)
            return;

        if (!ParameterCallSites.TryGetValue(parameterName, out var sites))
        {
            sites = new List<GDCallableCallSiteInfo>();
            ParameterCallSites[parameterName] = sites;
        }

        sites.Add(callSite);
    }

    /// <summary>
    /// Records that a Callable parameter is assigned to a class variable.
    /// </summary>
    public void AddParameterToClassVarAssignment(string parameterName, string classVarName)
    {
        if (string.IsNullOrEmpty(parameterName) || string.IsNullOrEmpty(classVarName))
            return;

        ParameterToClassVarAssignments[parameterName] = classVarName;
    }

    /// <summary>
    /// Registers a Callable parameter.
    /// </summary>
    public void RegisterCallableParameter(string parameterName, int index)
    {
        if (!string.IsNullOrEmpty(parameterName))
            CallableParameterIndices[parameterName] = index;
    }

    /// <summary>
    /// Checks if a name is a Callable parameter.
    /// </summary>
    public bool IsCallableParameter(string name)
    {
        return !string.IsNullOrEmpty(name) && CallableParameterIndices.ContainsKey(name);
    }

    /// <summary>
    /// Gets the parameter index for a Callable parameter.
    /// </summary>
    public int GetParameterIndex(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return -1;

        return CallableParameterIndices.TryGetValue(parameterName, out var index) ? index : -1;
    }

    /// <summary>
    /// Gets all call sites for a specific parameter.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return System.Array.Empty<GDCallableCallSiteInfo>();

        return ParameterCallSites.TryGetValue(parameterName, out var sites)
            ? sites
            : System.Array.Empty<GDCallableCallSiteInfo>();
    }

    /// <summary>
    /// Creates a method key from class name and method name.
    /// </summary>
    public static string CreateMethodKey(string? className, string methodName)
    {
        return $"{className ?? "self"}.{methodName}";
    }

    public override string ToString()
    {
        var callableParams = string.Join(", ", CallableParameterIndices.Keys);
        return $"MethodProfile({MethodKey}, Callable params: [{callableParams}])";
    }
}
