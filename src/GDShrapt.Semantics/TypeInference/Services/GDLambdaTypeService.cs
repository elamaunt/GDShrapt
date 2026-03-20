using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for lambda and Callable type inference.
/// Provides methods for inferring lambda parameter types from call sites and inter-procedural analysis.
/// </summary>
internal class GDLambdaTypeService
{
    /// <summary>
    /// Delegate for getting the call site registry.
    /// </summary>
    public delegate GDCallableCallSiteRegistry? GetCallSiteRegistryDelegate();

    /// <summary>
    /// Delegate for getting the script file.
    /// </summary>
    public delegate GDScriptFile? GetScriptFileDelegate();

    /// <summary>
    /// Delegate for getting the class name.
    /// </summary>
    public delegate string? GetClassNameDelegate();

    private readonly GetCallSiteRegistryDelegate _getCallSiteRegistry;
    private readonly GetScriptFileDelegate _getScriptFile;
    private readonly GetClassNameDelegate _getClassName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDLambdaTypeService"/> class.
    /// </summary>
    public GDLambdaTypeService(
        GetCallSiteRegistryDelegate getCallSiteRegistry,
        GetScriptFileDelegate getScriptFile,
        GetClassNameDelegate getClassName)
    {
        _getCallSiteRegistry = getCallSiteRegistry ?? throw new ArgumentNullException(nameof(getCallSiteRegistry));
        _getScriptFile = getScriptFile ?? throw new ArgumentNullException(nameof(getScriptFile));
        _getClassName = getClassName ?? throw new ArgumentNullException(nameof(getClassName));
    }

    /// <summary>
    /// Infers lambda parameter types from call sites.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesFromCallSites(GDMethodExpression lambda)
    {
        var registry = _getCallSiteRegistry();
        var scriptFile = _getScriptFile();

        if (registry == null || lambda == null || scriptFile == null)
            return new Dictionary<int, GDUnionType>();

        return registry.InferParameterTypes(lambda, scriptFile);
    }

    /// <summary>
    /// Infers a specific lambda parameter type from call sites.
    /// </summary>
    public string? InferLambdaParameterTypeFromCallSites(GDMethodExpression lambda, int parameterIndex)
    {
        var registry = _getCallSiteRegistry();
        var scriptFile = _getScriptFile();

        if (registry == null || lambda == null || scriptFile == null)
            return null;

        return registry.InferParameterType(lambda, scriptFile, parameterIndex);
    }

    /// <summary>
    /// Infers lambda parameter types including inter-procedural analysis.
    /// This includes call sites from method parameters when the lambda is passed as argument.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesWithFlow(GDMethodExpression lambda)
    {
        var registry = _getCallSiteRegistry();
        var scriptFile = _getScriptFile();

        if (registry == null || lambda == null || scriptFile == null)
            return new Dictionary<int, GDUnionType>();

        return registry.InferParameterTypesWithFlow(lambda, scriptFile);
    }

    /// <summary>
    /// Infers a specific lambda parameter type with inter-procedural analysis.
    /// </summary>
    public string? InferLambdaParameterTypeWithFlow(GDMethodExpression lambda, int parameterIndex)
    {
        var registry = _getCallSiteRegistry();
        var scriptFile = _getScriptFile();

        if (registry == null || lambda == null || scriptFile == null)
            return null;

        return registry.InferParameterTypeWithFlow(lambda, scriptFile, parameterIndex);
    }

    /// <summary>
    /// Gets the method Callable profile for a method.
    /// </summary>
    public GDMethodCallableProfile? GetMethodCallableProfile(string methodName)
    {
        var registry = _getCallSiteRegistry();

        if (registry == null || string.IsNullOrEmpty(methodName))
            return null;

        var className = _getClassName();
        var methodKey = GDMethodCallableProfile.CreateMethodKey(className, methodName);
        return registry.GetMethodProfile(methodKey);
    }

    /// <summary>
    /// Gets argument bindings for a lambda (where it's passed to method parameters).
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetLambdaArgumentBindings(GDMethodExpression lambda)
    {
        var registry = _getCallSiteRegistry();
        var scriptFile = _getScriptFile();

        if (registry == null || lambda == null || scriptFile == null)
            return Array.Empty<GDCallableArgumentBinding>();

        return registry.GetBindingsForLambda(lambda, scriptFile);
    }
}
